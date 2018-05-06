namespace SharedDataTypes
{
    public class AddSubscriberRequestMessage : MessageBase
    {
        public int SubscriberId { get; set; }
        public string Name { get; set; }
        public string Topic { get; set; }

        public AddSubscriberRequestMessage()
        {
            MessageType = MessageType.AddSubscriberRequest;
        }
    }
}
