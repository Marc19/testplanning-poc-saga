using System;
using TestPlanningSaga.Messages;

namespace TestPlanningSaga.Handlers
{
    public interface IExperimentWithMethodsHandler
    {
        void Handle(Message message);
    }
}
