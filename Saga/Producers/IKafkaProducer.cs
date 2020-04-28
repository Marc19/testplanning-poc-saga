using System;
using TestPlanningSaga.Messages;

namespace TestPlanningSaga.Producers
{
    public interface IKafkaProducer
    {
        void Produce(Message message, string topicName);
    }
}
