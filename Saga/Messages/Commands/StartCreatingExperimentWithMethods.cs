using System;
namespace TestPlanningSaga.Messages.Commands
{
    public class StartCreatingExperimentWithMethods : Command
    {
        public readonly CreateExperiment CreateExperiment;

        public readonly CreateMethods CreateMethods;

        public StartCreatingExperimentWithMethods(
            CreateExperiment createExperiment, CreateMethods createMethods, long loggedInUserId, Guid sagaId = default)
            : base(loggedInUserId, sagaId)
        {
            CreateExperiment = createExperiment;
            CreateMethods = createMethods;
        }
    }
}
