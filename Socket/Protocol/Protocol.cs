namespace Protocol
{
    public class ChatReqPacket
    {
        public ulong UserId { get; set; }
        public string Channel { get; set; }
        public string Message { get; set; }
        public ChatReqPacket(ulong userId, string channel, string message)
        {
            UserId = userId;
            Channel = channel;
            Message = message;
        }
    }

    public class ChatResPacket
    {
        public ulong UserId { get; set; }
        public string Channel { get; set; }
        public string Message { get; set; }
    }
}
