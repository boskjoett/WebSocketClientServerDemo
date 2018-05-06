using System;

namespace SharedDataTypes
{
    public class RemoveSubscriberRequestMessage : MessageBase
    {
        public int SubscriberId { get; set; }
        public Guid SubscriptionId { get; set; }

        public RemoveSubscriberRequestMessage()
        {
            MessageType = MessageType.RemoveSubscriberRequest;
        }
    }
}
