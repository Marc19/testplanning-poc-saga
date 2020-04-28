using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Chronicle;
using Microsoft.Extensions.Configuration;
using TestPlanningSaga.DTOs;
using TestPlanningSaga.Messages;
using TestPlanningSaga.Messages.Commands;
using TestPlanningSaga.Messages.Events;
using TestPlanningSaga.Producers;

namespace TestPlanningSaga.Sagas
{
    public class ExperimentWithMethodsSaga : Saga<ExperimentWithMethodSagaData>,
        ISagaStartAction<StartCreatingExperimentWithMethods>,
        ISagaAction<ExperimentCreated>,
        ISagaAction<ExperimentCreationFailed>,
        ISagaAction<MethodsCreated>,
        ISagaAction<MethodsCreationFailed>,
        ISagaAction<MethodsAddedToExperiment>,
        ISagaAction<MethodsAdditionToExperimentFailed>,
        ISagaAction<ExperimentAddedToMethods>,
        ISagaAction<ExperimentAdditionToMethodsFailed>
    {
        private readonly IKafkaProducer _kafkaProducer;
        private readonly IConfiguration Configuration;
        private readonly string EXPERIMENTS_TOPIC;
        private readonly string METHODS_TOPIC;
        

        public ExperimentWithMethodsSaga(IKafkaProducer kafkaProducer, IConfiguration configuration)
        {
            _kafkaProducer = kafkaProducer;
            Configuration = configuration;
            EXPERIMENTS_TOPIC = Configuration["ExperimentsTopic"];
            METHODS_TOPIC = Configuration["MethodsTopic"];
        }

        public override SagaId ResolveId(object message, ISagaContext context)
        {
            return message switch
            {
                Message m => m.SagaId.ToString(),
                _ => base.ResolveId(message, context),
            };
        }


        public Task HandleAsync(StartCreatingExperimentWithMethods message, ISagaContext context)
        {
            //Initializing data without Ids as they are not yet created
            Data.Experiment = new ExperimentData { Creator = message.CreateExperiment.Creator, Name = message.CreateExperiment.Name };
            Data.Methods = new List<MethodData>();

            foreach (var createMethod in message.CreateMethods.Methods)
            {
                Data.Methods.Add(new MethodData
                {
                    Creator = createMethod.Creator,
                    Name = createMethod.Name,
                    ApplicationRate = createMethod.ApplicationRate
                });
            }

            CreateExperiment createExperiment = new CreateExperiment(
                Data.Experiment.Creator,
                Data.Experiment.Name,
                message.LoggedInUserId,
                message.SagaId
            );

            _kafkaProducer.Produce(createExperiment, EXPERIMENTS_TOPIC);
            return Task.CompletedTask;
        }

        public Task CompensateAsync(StartCreatingExperimentWithMethods message, ISagaContext context)
        {
            ExperimentWithMethodsCreationFailed failed = new ExperimentWithMethodsCreationFailed(
                Data.failureReason,
                message.LoggedInUserId,
                message.SagaId
            );

            _kafkaProducer.Produce(failed, EXPERIMENTS_TOPIC);

            return Task.CompletedTask;
        }


        public Task HandleAsync(ExperimentCreated message, ISagaContext context)
        {
            Data.Experiment.Id = message.Id;
            Data.Experiment.CreationDate = message.CreationDate;

            List<CreateMethod> createMethodValues = new List<CreateMethod>();

            foreach (var methodData in Data.Methods)
            {
                createMethodValues.Add(
                    new CreateMethod(
                        methodData.Creator,
                        methodData.Name,
                        methodData.ApplicationRate,
                        message.LoggedInUserId,
                        message.SagaId
                    )
                );
            }

            CreateMethods createMethods = new CreateMethods(createMethodValues, message.LoggedInUserId, message.SagaId);

            _kafkaProducer.Produce(createMethods, METHODS_TOPIC);
            return Task.CompletedTask;
        }

        public Task CompensateAsync(ExperimentCreated message, ISagaContext context)
        {
            DeleteExperiment deleteExperiment = new DeleteExperiment(message.Id, message.LoggedInUserId, message.SagaId);

            _kafkaProducer.Produce(deleteExperiment, EXPERIMENTS_TOPIC);

            return Task.CompletedTask;
        }


        public Task HandleAsync(MethodsCreated message, ISagaContext context)
        {
            if (message.CreatedMethods.Count != Data.Methods.Count)
            {
                try
                {
                    RejectAsync();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                return Task.CompletedTask;
            }

            for (int i = 0; i < Data.Methods.Count; i++)
            {
                Data.Methods[i].Id = message.CreatedMethods[i].Id;
                Data.Methods[i].CreationDate = message.CreatedMethods[i].CreationDate;
            }

            AddMethodsToExperiment addMethodsToExperiment = new AddMethodsToExperiment(
                Data.ExperimentId,
                Data.MethodsIds,
                message.LoggedInUserId,
                message.SagaId
            );

            _kafkaProducer.Produce(addMethodsToExperiment, METHODS_TOPIC);
            _kafkaProducer.Produce(addMethodsToExperiment, EXPERIMENTS_TOPIC);

            return Task.CompletedTask;
        }

        public Task CompensateAsync(MethodsCreated message, ISagaContext context)
        {
            DeleteMethods deleteMethods =
                new DeleteMethods(message.CreatedMethods.Select(m => m.Id).ToList(), message.LoggedInUserId, message.SagaId);

            _kafkaProducer.Produce(deleteMethods, METHODS_TOPIC);

            return Task.CompletedTask;
        }


        public Task HandleAsync(MethodsAddedToExperiment message, ISagaContext context)
        {
            Data.MethodsAddedToExperiment = true;

            if (Data.MethodsAddedToExperiment && Data.ExperimentAddedToMethods)
            {
                ExperimentData experiment = Data.Experiment;
                List<MethodData> methods = Data.Methods;

                ExperimentCreatedWithMethods experimentCreatedWithMethods = new ExperimentCreatedWithMethods(
                    new ExperimentEventData(experiment.Id, experiment.Creator, experiment.Name, experiment.CreationDate),
                    methods.Select(m => new MethodEventData(m.Id, m.Creator, m.Name, m.ApplicationRate, m.CreationDate)).ToList(),
                    message.LoggedInUserId,
                    message.SagaId
                );

                _kafkaProducer.Produce(experimentCreatedWithMethods, EXPERIMENTS_TOPIC);

                Complete();
            }

            return Task.CompletedTask;
        }

        public Task CompensateAsync(MethodsAddedToExperiment message, ISagaContext context)
        {
            Data.MethodsAddedToExperiment = false;

            RemoveMethodsFromExperiment removeMethodsFromExperiment = new RemoveMethodsFromExperiment(
                Data.ExperimentId,
                Data.MethodsIds,
                message.LoggedInUserId,
                message.SagaId
            );

            _kafkaProducer.Produce(removeMethodsFromExperiment, EXPERIMENTS_TOPIC);

            return Task.CompletedTask;
        }


        public Task HandleAsync(ExperimentAddedToMethods message, ISagaContext context)
        {
            Data.ExperimentAddedToMethods = true;

            if (Data.MethodsAddedToExperiment && Data.ExperimentAddedToMethods)
            {
                ExperimentData experiment = Data.Experiment;
                List<MethodData> methods = Data.Methods;

                ExperimentCreatedWithMethods experimentCreatedWithMethods = new ExperimentCreatedWithMethods(
                    new ExperimentEventData(experiment.Id, experiment.Creator, experiment.Name, experiment.CreationDate),
                    methods.Select(m => new MethodEventData(m.Id, m.Creator, m.Name, m.ApplicationRate, m.CreationDate)).ToList(),
                    message.LoggedInUserId,
                    message.SagaId
                );

                _kafkaProducer.Produce(experimentCreatedWithMethods, EXPERIMENTS_TOPIC);

                Complete();
            }

            return Task.CompletedTask;
        }

        public Task CompensateAsync(ExperimentAddedToMethods message, ISagaContext context)
        {
            Data.ExperimentAddedToMethods = false;

            RemoveMethodsFromExperiment removeMethodsFromExperiment = new RemoveMethodsFromExperiment(
                Data.ExperimentId,
                Data.MethodsIds,
                message.LoggedInUserId,
                message.SagaId
            );

            _kafkaProducer.Produce(removeMethodsFromExperiment, METHODS_TOPIC);

            return Task.CompletedTask;
        }


        public Task HandleAsync(ExperimentCreationFailed message, ISagaContext context)
        {
            Data.failureReason = message.FailureReason;
            try
            {
                RejectAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return Task.CompletedTask;
        }

        public Task CompensateAsync(ExperimentCreationFailed message, ISagaContext context)
        {
            return Task.CompletedTask;
        }


        public Task HandleAsync(MethodsCreationFailed message, ISagaContext context)
        {
            Data.failureReason = message.FailureReason;
            try
            {
                RejectAsync();
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
            return Task.CompletedTask;
        }

        public Task CompensateAsync(MethodsCreationFailed message, ISagaContext context)
        {
            return Task.CompletedTask;
        }


        public Task HandleAsync(MethodsAdditionToExperimentFailed message, ISagaContext context)
        {
            Data.failureReason = message.FailureReason;
            try
            {
                RejectAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return Task.CompletedTask;
        }

        public Task CompensateAsync(MethodsAdditionToExperimentFailed message, ISagaContext context)
        {
            return Task.CompletedTask;
        }


        public Task HandleAsync(ExperimentAdditionToMethodsFailed message, ISagaContext context)
        {
            Data.failureReason = message.FailureReason;
            try
            {
                RejectAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return Task.CompletedTask;
        }

        public Task CompensateAsync(ExperimentAdditionToMethodsFailed message, ISagaContext context)
        {
            return Task.CompletedTask;
        }
    }

    /*
     * =Copy the saga data to another file in the same folder, and change its suffix to state maybe
     * =Copy these commands and events from Methods project to other projects
     * =Handle their actions in Methods project and Experimens project accordingly
     * Add configuration in Consumer for topic names in method and experiments projects
     * (=)Use Serilog to print what happens in Produce method
     * =Now, complete the remaining saga steps
     */

    /*
     * =First: Correct All topic names in different projects to be read from config file
     * =Second: Correct consumers group names
     * =Third: Correct port numbers in launchsettings.json:
     *      (0-1): Experiments, (2-3): Notifications, (4-5): Methods, (6,7): Saga
     * 
     * =Produce(StartCreatingExperimentWithMethods)
     * =Enters Saga
     * =Produce(CreateExperiment)
     * =When: ExperimentCreated
     * =Produce(CreateMethods)
     * =When: MethodsCreated
     * =Produce(AddMethodsToExperiment)
     * =When: MethodsAddedToExperiment &&
     * =When: ExperimentsAddedToMethod
     * =Produce(ExperimentCreatedWithMethods)
     * =Produce(SAGA-FAILURE-EVENT)
     *
     * =First: Notification should somehow know whether an event is part of saga or not
     *
     */
}
