using System;
using TestPlanningSaga.Messages;
using TestPlanningSaga.DTOs;
using TestPlanningSaga.Messages.Commands;
using Chronicle;
using TestPlanningSaga.Producers;
using Microsoft.Extensions.Configuration;
using TestPlanningSaga.Messages.Events;

namespace TestPlanningSaga.Handlers
{
    public class ExperimentWithMethodsHandler : IExperimentWithMethodsHandler
    {
        private readonly ISagaCoordinator _sagaCoordinator;
        private readonly ISagaLog SagaLogData;
        private readonly IKafkaProducer _kafkaProducer;
        private readonly IConfiguration Configuration;
        private readonly string EXPERIMENTS_TOPIC;
        private readonly string METHODS_TOPIC;

        public ExperimentWithMethodsHandler(
            ISagaCoordinator sagaCoordinator, ISagaLog sagaLogData,
            IKafkaProducer kafkaProducer, IConfiguration configuration)
        {
            _sagaCoordinator = sagaCoordinator;
            _kafkaProducer = kafkaProducer;
            Configuration = configuration;
            EXPERIMENTS_TOPIC = Configuration["ExperimentsTopic"];
            METHODS_TOPIC = Configuration["MethodsTopic"];
            SagaLogData = sagaLogData;

        }

        public void Handle(Message message)
        {
            var sagaContext = SagaContext.Empty;

            switch (message)
            {
                case StartCreatingExperimentWithMethods m: _sagaCoordinator.ProcessAsync(m, sagaContext); break;
                case ExperimentCreated m: _sagaCoordinator.ProcessAsync(m, sagaContext); break;
                case ExperimentCreationFailed m: _sagaCoordinator.ProcessAsync(m, sagaContext); break;
                case MethodsCreated m: _sagaCoordinator.ProcessAsync(m, sagaContext); break;
                case MethodsCreationFailed m: _sagaCoordinator.ProcessAsync(m, sagaContext); break;
                case MethodsAddedToExperiment m: _sagaCoordinator.ProcessAsync(m, sagaContext); break;
                case MethodsAdditionToExperimentFailed m: _sagaCoordinator.ProcessAsync(m, sagaContext); break;
                case ExperimentAddedToMethods m: _sagaCoordinator.ProcessAsync(m, sagaContext); break;
                case ExperimentAdditionToMethodsFailed m: _sagaCoordinator.ProcessAsync(m, sagaContext); break;
                case ExperimentWithMethodsCreationFailed m: _sagaCoordinator.ProcessAsync(m, sagaContext); break;
            }
        }
    }
}
