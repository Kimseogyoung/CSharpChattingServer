using Protocol;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
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
    public interface ISocketSender
    {
        void AddSendQueue(string sessionId, byte[] data);
    }

    public class SocketService : ISocketSender
    {
        public SocketService(SessionService sessionService, ILogger<SocketService> logger)
        {
            _sessionService = sessionService;
            _logger = logger;
        }

        public async Task ListenAsync(TcpListener listener, CancellationToken stoppingToken, Func<string, byte[], Task> excuteAction)
        {
            _excuteFunc = excuteAction;

            ProcessSendQueueAsync(stoppingToken);

            listener.Start();

            while (!stoppingToken.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync();
                _ = HandleClientAsync(client, stoppingToken);
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            var session = _sessionService.CreateSession(client);

            try
            {
                using var stream = client.GetStream();
                var messageBuffer = new MemoryStream();

                while (!token.IsCancellationRequested)
                {
                    var lengthBuffer = new byte[4];
                    // 메시지 길이 4바이트 읽기
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

                    // 메시지 본문 (동적으로 할당)
                    var messageBytes = new byte[readMessageCnt];
                    read = await ReadExactAsync(stream, messageBytes, readMessageCnt, token);
                    if (read == 0)
                    {
                        // 연결 종료
                        break;
                    }

                    // 파싱/비즈니스는 Dispatcher에 위임
                    await _excuteFunc.Invoke(session.Id, messageBytes);
                }
            }
            finally
            {
                _sessionService.CloseSession(session.Id);
            }
        }

        public void AddSendQueue(string guid, byte[] bytes)
        {
            _sendQueue.Enqueue((guid, bytes));
        }

        private async Task SendAsync(string sessionId, byte[] bytes)
        {
            var session = _sessionService.GetSession(sessionId);
            var client = session.Client;

            // 응답 예시
            var responseLength = BitConverter.GetBytes(bytes.Length).Reverse().ToArray();

            var stream = client.GetStream();
            await stream.WriteAsync(responseLength, 0, 4);
            await stream.WriteAsync(bytes, 0, bytes.Length);
        }

        public async void ProcessSendQueueAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                while (_sendQueue.TryDequeue(out var item))
                {
                    await SendAsync(item.Id, item.Bytes);
                }
                await Task.Delay(5, stoppingToken);
            }
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
        private readonly SessionService _sessionService;
        private Func<string, byte[], Task>? _excuteFunc; 
        private readonly ConcurrentQueue<(string Id, byte[] Bytes)> _sendQueue = new();
    }
}
