namespace Protocol
{
    public abstract class ReqPacket
    {
        public abstract string GetMethodName();
    }

    public abstract class ResPacket
    {
    }


    public enum EChatAction
    {
        NONE = 0,
        SUBSCRIBE = 1,
        UNSUBSCRIBE = 2,
        SEND = 3,

        REGISTER_USER = 10,
    }

    public class ChatReqPacket : ReqPacket
    {
        public ulong UserId { get; set; }
        public EChatAction Action { get; set; }
        public string Channel { get; set; }
        public string Message { get; set; }
        public ChatReqPacket(ulong userId, EChatAction action, string channel, string message)
        {
            UserId = userId;
            Action = action;
            Channel = channel;
            Message = message;
        }

        public const string METHOD_NAME = "chat";
        public override string GetMethodName() => METHOD_NAME;
    }

    public class ChatResPacket : ResPacket
    {
        public string Message { get; set; }
    }

    public class MoveReqPacket : ReqPacket// 다른 패킷 예시로 작성
    {
        public ulong UserId { get; set; }
        public int MoveX { get; set; }
        public int MoveY { get; set; }
        public MoveReqPacket(ulong userId, int moveX, int moveY)
        {
            UserId = userId;
            MoveX = moveX;
            MoveY = moveY;
        }

        public const string METHOD_NAME = "move";
        public override string GetMethodName() => METHOD_NAME;
    }

    public class MoveResPacket : ResPacket
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
}
