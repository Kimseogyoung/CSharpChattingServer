using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Protocol;

namespace Server
{
    public class ChatService
    {
        public ChatService(SessionService sessionService, RedisPubSubService redisPubSubService, ISocketSender sender)
        {
            _sessionService = sessionService;
            _redisPubSubService = redisPubSubService;
            _redisPubSubService.Connect(new List<string>() { "127.0.0.1:6379", "127.0.0.1:6379" });
            _sender = sender;
        }

        public async Task ProcessMessageAsync(string sessionId, ulong userId, EChatAction action, string channel, string message)
        {
            Log(ELogLevel.DEBUG, userId, $"ProcessMessageStart Action({action}) Msg({message})");

            switch (action)
            {
                case EChatAction.REGISTER_USER:
                    {
                        _sessionService.AuthenticateSession(sessionId, userId);
                        var chatRes = new ChatResPacket { Message = $"REGISTER ({userId})" };
                        var bytes = ProtocolParser.Serialize(chatRes); ;
                        _sender.AddSendQueue(sessionId, bytes);
                        break;
                    }
                case EChatAction.SUBSCRIBE:
                    {
                        Subcribe(userId, channel);
                    }
                    break;
                case EChatAction.UNSUBSCRIBE:
                    {
                        Unsubcribe(userId, channel);
                    }
                    break;
                case EChatAction.SEND:
                    {
                        await PublishMsgAsync(userId, channel, message);
                    }
                    break;
                default:
                    Log(ELogLevel.ERROR, userId, $"NO_HANDLING_ACTION({action})");
                    break;
            }
        }

        private void Subcribe(ulong userId, string channel)
        {
            _subscribeChannelUserIdDict.TryAdd(channel, new HashSet<ulong>());
            var userIdSet = _subscribeChannelUserIdDict[channel];

            if (userIdSet.Contains(userId))
            {
                Log(ELogLevel.WARNING, userId, $"ALREADY_SUBSCRIBE Channel({channel})");
                return;
            }

            userIdSet.Add(userId);
            _redisPubSubService.Subscribe(channel, OnRecvMessage);
            Log(ELogLevel.INFO, userId, $"SubscribeToRedis Channel({channel})");
        }

        private void Unsubcribe(ulong userId, string channel)
        {
            if (!_subscribeChannelUserIdDict.TryGetValue(channel, out var userIdSet))
            {
                Log(ELogLevel.WARNING, userId, $"NOT_UNSUBSCRIBE Channel({channel})");
                return;
            }

            if (!userIdSet.Contains(userId))
            {
                Log(ELogLevel.WARNING, userId, $"NOT_UNSUBSCRIBE Channel({channel})");
                return;
            }

            userIdSet.Remove(userId);
            Log(ELogLevel.INFO, userId, $"UnsubscribeToRedis Channel({channel})");

            if (userIdSet.Count == 0)
            {
                _redisPubSubService.Unsubscribe(channel);
            }

        }

        private async Task PublishMsgAsync(ulong userId, string channel, string msg)
        {
            var sendMsg = $"FROM({channel}) UserId({userId}) : {msg}";
            await _redisPubSubService.PublishAsync(channel, sendMsg);
            Log(ELogLevel.INFO, userId, $"PublishToRedis Channel({channel}) Msg({sendMsg})");
        }

        private void OnRecvMessage(string? channel, string? message)
        {
            if (string.IsNullOrEmpty(channel) || string.IsNullOrEmpty(message))
            {
                return;
            }

            if (!_subscribeChannelUserIdDict.TryGetValue(channel, out var userIdSet))
            {
                return;
            }

            if (userIdSet.Count == 0)
            {
                return;
            }

            Log(ELogLevel.INFO, 0, $"PublishToClient Channel({channel}) Msg({message})");
            var chatRes = new ChatResPacket { Message = message };
            var bytes = ProtocolParser.Serialize(chatRes);

            foreach (var userId in userIdSet)
            {
                var session = _sessionService.GetSessionByUserId(userId);
                _sender.AddSendQueue(session.Id, bytes);
            }
        }

        // 로그 확인용. TODO: NLogger로 교체
        private void Log(ELogLevel level, ulong userId, string msg)
        {
            Console.WriteLine($"[{level.ToString()}]{userId}: {msg}");
        }

        public enum ELogLevel
        {
            DEBUG = 1,
            INFO = 2,
            WARNING = 3,
            ERROR = 4,
        }

        private readonly SessionService _sessionService;
        private readonly RedisPubSubService _redisPubSubService;
        private readonly ISocketSender _sender;
        private ConcurrentDictionary<string, HashSet<ulong>> _subscribeChannelUserIdDict = new ConcurrentDictionary<string, HashSet<ulong>>();
    }
}
