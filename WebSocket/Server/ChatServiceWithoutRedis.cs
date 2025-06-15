using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace Server
{
    // 레디스 없이 바로 클라 전달하는 예제
    public class ChatServiceWithoutRedis
    {
        public ChatServiceWithoutRedis()
        {

        }

        public async Task ProcessAsync(HttpContext httpCtx)
        {
            if (!httpCtx.WebSockets.IsWebSocketRequest)
            {
                httpCtx.Response.StatusCode = 400;
                return;
            }

            // userId 읽기
            var userIdStr = httpCtx.Request.Query["userId"].FirstOrDefault();
            if (string.IsNullOrEmpty(userIdStr))
            {
                httpCtx.Response.StatusCode = 400;
                return;
            }

            var userId = ulong.Parse(userIdStr);
            Console.WriteLine($"[유저 ID]: {userId}");

            // 최초 연결
            if (_userIdWebSocketDict.ContainsKey(userId))
            {
                Console.WriteLine($"[유저 ID]: {userId} 이미 연결됨.");
                httpCtx.Response.StatusCode = 400;
                return;
            }

            var socket = await httpCtx.WebSockets.AcceptWebSocketAsync();
            if (!_userIdWebSocketDict.TryAdd(userId, socket))
            {
                Console.WriteLine($"[유저 ID]: {userId} 이미 연결됨 ?????.");
                httpCtx.Response.StatusCode = 400;
                return;
            }

            var buffer = new byte[1024 * 4];
            var messageBuffer = new MemoryStream();

            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                switch (result.MessageType)
                {
                    case WebSocketMessageType.Text:
                        {
                            // 이번에 받은 부분만 출력
                            var partialMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            Console.WriteLine($"[수신 조각]: {partialMessage}");

                            messageBuffer.Write(buffer, 0, result.Count);

                            if (result.EndOfMessage)
                            {
                                var messageBytes = messageBuffer.ToArray();
                                var message = Encoding.UTF8.GetString(messageBytes);
                                await OnMessageAsync(message);
                            }
                        }
                        break;
                    case WebSocketMessageType.Binary:
                        break;
                    case WebSocketMessageType.Close:
                        {
                            if (!_userIdWebSocketDict.TryRemove(userId, out var connectedWebSocket))
                            {
                                Console.WriteLine($"클라이언트 연결 종료 실패 : {userId}");
                                httpCtx.Response.StatusCode = 400;
                                return;
                            }

                            await connectedWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None);
                            Console.WriteLine($"클라이언트 연결 종료 : {userId}");
                        }
                        break;
                }
            }
        }

        private async Task OnMessageAsync(string message)
        {
            var sendMessage = $"[Echo] {message}";
            Console.WriteLine(sendMessage);

            var sendBuffer = Encoding.UTF8.GetBytes(sendMessage);
            foreach (var ws in _userIdWebSocketDict.Values.Where(w => w.State == WebSocketState.Open))
            {
                await ws.SendAsync(new ArraySegment<byte>(sendBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        private ConcurrentDictionary<ulong, WebSocket> _userIdWebSocketDict = new ConcurrentDictionary<ulong, WebSocket>();
    }
}
