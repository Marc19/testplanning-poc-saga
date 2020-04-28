﻿using System;

namespace TestPlanningSaga.Messages
{
    public abstract class Message
    {
        public readonly long LoggedInUserId;

        public Guid SagaId { get; set; }

        public Message(long loggedInUserId, Guid sagaId)
        {
            LoggedInUserId = loggedInUserId;
            SagaId = sagaId;
        }
    }
}
