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

        public Worker(SocketService socketService, MessageDispatcher messageDispatcher, ILogger<Worker> logger)
        {
            _socketService = socketService;
            _messageDispatcher = messageDispatcher;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var listener = new TcpListener(IPAddress.Any, 5000);
            await _socketService.ListenAsync(listener, stoppingToken, _messageDispatcher.OnMessageReceived);      
        }

        private readonly SocketService _socketService;
        private readonly MessageDispatcher _messageDispatcher;
        private readonly ILogger<Worker> _logger;

    }
}
