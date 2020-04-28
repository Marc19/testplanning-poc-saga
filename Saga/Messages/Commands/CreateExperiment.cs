using System;
namespace TestPlanningSaga.Messages.Commands
{
    public class CreateExperiment : Command
    {
        public readonly string Creator;

        public readonly string Name;

        public CreateExperiment(string creator, string name, long loggedInUserId, Guid sagaId = default)
            : base(loggedInUserId, sagaId)
        {
            Creator = creator;
            Name = name;
        }
    }
}
