using System;

namespace TestPlanningSaga.Messages.Events
{
    public abstract class Event : Message
    {
        public Event(long loggedInUserId, Guid sagaId) : base(loggedInUserId, sagaId) { }
    }
}
