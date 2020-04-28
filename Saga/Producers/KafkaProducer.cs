using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TestPlanningSaga.Messages;

namespace TestPlanningSaga.Producers
{
    public class KafkaProducer : IKafkaProducer
    {
        ProducerConfig _producerConfig;
        private readonly ILogger<KafkaProducer> _logger;

        public KafkaProducer(ILogger<KafkaProducer> logger)
        {
            _producerConfig = new ProducerConfig
            {
                BootstrapServers = "localhost:9092",
            };

            _logger = logger;
        }

        public void Produce(Message message, string topicName)
        {
            Task.Run(() =>
            {
                var messageObject = new
                {
                    messageType = message.GetType().Name,
                    occuredAt = DateTime.Now,
                    payload = message
                };

                string messageJson = JsonConvert.SerializeObject(messageObject);

                Thread.Sleep(2000);

                using (var producer = new ProducerBuilder<Null, string>(_producerConfig).Build())
                {
                    Type type = message.GetType();
                    var t = producer.ProduceAsync(topicName,
                        new Message<Null, string> { Value = messageJson });

                    t.Wait();
                }

            });
        }
    }

}
