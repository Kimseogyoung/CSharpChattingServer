using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;

namespace Server
{
    public class Worker : BackgroundService
    {

        public Worker(ChatService chatService, ILogger<Worker> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var listener = new TcpListener(IPAddress.Any, 5000);
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
                    Console.WriteLine("잘못된 메시지 길이");
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
                Console.WriteLine($"수신: {message}");

                // 응답 예시
                var response = Encoding.UTF8.GetBytes($"[Echo] {message}");
                var responseLength = BitConverter.GetBytes(response.Length).Reverse().ToArray();

                await stream.WriteAsync(responseLength, 0, 4, token);
                await stream.WriteAsync(response, 0, response.Length, token);
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

        // Channel : .NET의 고성능 async 큐
        // ConcurrentQueue : 간단한 멀티스레드용 FIFO
        private readonly ConcurrentQueue<string> _messageQueue = new();
        private readonly ChatService _chatService;
        private readonly ILogger<Worker> _logger;

    }
}
