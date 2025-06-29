using Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Server
{
    public class SocketService
    {
        public SocketService(ChatService chatService, ILogger<SocketService> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        public async Task ListenAsync(TcpListener listener, CancellationToken stoppingToken)
        {
            _chatService.Init(SendResponse);

            listener.Start();
            while (!stoppingToken.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync();
                _ = HandleClientAsync(client, stoppingToken);
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            using var stream = client.GetStream();

            var messageBuffer = new MemoryStream();

            while (!token.IsCancellationRequested)
            {
                var lengthBuffer = new byte[4];
                // 1. 메시지 길이 4바이트 읽기
                int read = await ReadExactAsync(stream, lengthBuffer, 4, token);
                if (read == 0)
                {
                    // 연결 종료
                    break;
                }

                int readMessageCnt = BitConverter.ToInt32(lengthBuffer.Reverse().ToArray(), 0);  // Big endian 처리
                if (readMessageCnt <= 0)
                {
                    _logger.LogError($"잘못된 메시지 길이 ({readMessageCnt})");
                    break;
                }

                // 2. 메시지 본문 (동적으로 할당)
                var messageBytes = new byte[readMessageCnt];
                read = await ReadExactAsync(stream, messageBytes, readMessageCnt, token);
                if (read == 0)
                {
                    // 연결 종료
                    break;
                }

                var message = Encoding.UTF8.GetString(messageBytes);
                var chatReq = ProtocolParser.Deserialize<ChatReqPacket>(messageBytes);
                _userIdTotcpClientDict.TryAdd(chatReq.UserId, client);

                var resBytes = await _chatService.ProcessMessageAsync(chatReq.UserId, chatReq.Action, chatReq.Channel, chatReq.Message);
                if (resBytes == null)
                {
                    continue;
                }

                SendResponse(chatReq.UserId, resBytes);
            }
        }

        private async void SendResponse(ulong userId, byte[] bytes)
        {
            // 응답 예시
            var responseLength = BitConverter.GetBytes(bytes.Length).Reverse().ToArray();

            if (!_userIdTotcpClientDict.TryGetValue(userId, out var tcpClient))
            {
                return;
            }

            var stream = tcpClient.GetStream();
            await stream.WriteAsync(responseLength, 0, 4);
            await stream.WriteAsync(bytes, 0, bytes.Length);
        }

        private async Task<int> ReadExactAsync(Stream stream, byte[] buffer, int length, CancellationToken token)
        {
            int totalRead = 0;
            while (totalRead < length)
            {
                int read = await stream.ReadAsync(buffer, totalRead, length - totalRead, token);
                if (read == 0)
                {
                    return 0; // 연결 종료
                }
                totalRead += read;
            }
            return totalRead;
        }

        private readonly ILogger _logger;
        private readonly ChatService _chatService;
        private Dictionary<ulong, TcpClient> _userIdTotcpClientDict = new();
    }
}
