using System;

namespace SharedDataTypes
{
    public class RemoveSubscriberResponseMessage : MessageBase
    {
        public Guid SubscriptionId { get; set; }
        public int SubscriberId { get; set; }
        public bool Success { get; set; }

        public RemoveSubscriberResponseMessage()
        {
            MessageType = MessageType.RemoveSubscriberResponse;
        }
    }
}
