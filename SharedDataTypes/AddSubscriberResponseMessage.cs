using System;

namespace SharedDataTypes
{
    public class AddSubscriberResponseMessage : MessageBase
    {
        public Guid SubscriptionId { get; set; }
        public int SubscriberId { get; set; }
        public bool Success { get; set; }

        public AddSubscriberResponseMessage()
        {
            MessageType = MessageType.AddSubscriberResponse;
        }
    }
}
