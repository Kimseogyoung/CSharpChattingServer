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

        public Worker(SocketService socketService, ILogger<Worker> logger)
        {
            _socketService = socketService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var listener = new TcpListener(IPAddress.Any, 5000);
            await _socketService.ListenAsync(listener, stoppingToken);      
        }

        // Channel : .NET의 고성능 async 큐
        // ConcurrentQueue : 간단한 멀티스레드용 FIFO
        private readonly ConcurrentQueue<string> _messageQueue = new();
        private readonly SocketService _socketService;
        private readonly ILogger<Worker> _logger;

    }
}
