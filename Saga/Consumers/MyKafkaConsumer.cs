using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TestPlanningSaga.Handlers;
using TestPlanningSaga.Messages;
using TestPlanningSaga.Messages.Commands;
using TestPlanningSaga.Messages.Events;
using TestPlanningSaga.Sagas;

namespace TestPlanningSaga.Consumers
{
    public class MyKafkaConsumer : BackgroundService
    {
        ConsumerConfig _consumerConfig;
        private readonly IExperimentWithMethodsHandler _experimentWithMethodsHandler;
        private readonly IConfiguration Configuration;
        private readonly string EXPERIMENTS_TOPIC;
        private readonly string METHODS_TOPIC;

        public MyKafkaConsumer(ConsumerConfig consumerConfig, IExperimentWithMethodsHandler experimentCommandHandler,
            IConfiguration configuration)
        {
            _consumerConfig = new ConsumerConfig
            {
                GroupId = "saga-consumer",
                BootstrapServers = "localhost:9092",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            _experimentWithMethodsHandler = experimentCommandHandler;
            Configuration = configuration;
            EXPERIMENTS_TOPIC = Configuration["ExperimentsTopic"];
            METHODS_TOPIC = Configuration["MethodsTopic"];
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Task.Run(() => StartConsumer(stoppingToken));
            return Task.CompletedTask;
        }

        private void StartConsumer(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var c = new ConsumerBuilder<string, string>(_consumerConfig).Build())
                {
                    c.Subscribe(new List<string> { EXPERIMENTS_TOPIC, METHODS_TOPIC });

                    CancellationTokenSource cts = new CancellationTokenSource();
                    Console.CancelKeyPress += (_, e) => {
                        e.Cancel = true; // prevent the process from terminating.
                        cts.Cancel();
                    };

                    try
                    {
                        while (true)
                        {
                            Console.Write("Starting loop");
                            try
                            {
                                var cr = c.Consume(cts.Token);
                                var msg = cr.Message;
                                //var key = cr.Message.Key;
                                var val = cr.Message.Value;
                                Console.WriteLine($"Consumed message '{cr.Message.Value}' at: '{cr.TopicPartitionOffset}'.");

                                Message message = GetMessageType(val);
                                if (message != null)
                                    _experimentWithMethodsHandler.Handle(message);
                                //otherwise, it's not a command
                            }
                            catch (ConsumeException e)
                            {
                                Console.WriteLine($"Error occured: {e.Error.Reason}");
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Ensure the consumer leaves the group cleanly and final offsets are committed.
                        c.Close();
                    }
                }
            }
        }

        private Message GetMessageType(string message)
        {
            JObject rss = JObject.Parse(message);
            string messageType = (string)rss["messageType"];
            string payload = ((JObject)rss["payload"]).ToString();

            return messageType switch
            {
                "CreateExperiment" => JsonConvert.DeserializeObject<CreateExperiment>(payload),
                "DeleteExperiment" => JsonConvert.DeserializeObject<DeleteExperiment>(payload),
                "UpdateExperiment" => JsonConvert.DeserializeObject<UpdateExperiment>(payload),
                "StartCreatingExperimentWithMethods" => JsonConvert.DeserializeObject<StartCreatingExperimentWithMethods>(payload),
                "ExperimentCreated" => JsonConvert.DeserializeObject<ExperimentCreated>(payload),
                "ExperimentCreationFailed" => JsonConvert.DeserializeObject<ExperimentCreationFailed>(payload),
                "MethodsCreated" => JsonConvert.DeserializeObject<MethodsCreated>(payload),
                "MethodsCreationFailed" => JsonConvert.DeserializeObject<MethodsCreationFailed>(payload),
                "MethodsAddedToExperiment" => JsonConvert.DeserializeObject<MethodsAddedToExperiment>(payload),
                "MethodsAdditionToExperimentFailed" => JsonConvert.DeserializeObject<MethodsAdditionToExperimentFailed>(payload),
                "ExperimentAddedToMethods" => JsonConvert.DeserializeObject<ExperimentAddedToMethods>(payload),
                "ExperimentAdditionToMethodsFailed" => JsonConvert.DeserializeObject<ExperimentAdditionToMethodsFailed>(payload),
                "ExperimentWithMethodsCreationFailed" => JsonConvert.DeserializeObject<ExperimentWithMethodsCreationFailed>(payload),
                _ => null,
            };
        }
    }

}
