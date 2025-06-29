using Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class MessageDispatcher
    {
        public MessageDispatcher(ChatService chatService)
        {
            _chatService = chatService;
        }

        public async Task OnMessageReceived(string sessionId, byte[] data)
        {
            // 1. 파싱
            var chatReq = ProtocolParser.Deserialize<ChatReqPacket>(data);

            // 2. 비즈니스 로직 호출
            await _chatService.ProcessMessageAsync(sessionId, chatReq.UserId, chatReq.Action, chatReq.Channel, chatReq.Message);
        }

        private readonly ChatService _chatService;
    }
}
