using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class ChatService
    {
        public ChatService(RedisPubSubService redisPubSubService)
        {
            _redisPubSubService = redisPubSubService;
        }

        public void Init(Action<string> onRecvMsg)
        {
            _onRecvMsg = onRecvMsg;
            _redisPubSubService.Connect(new List<string>() { "127.0.0.1:6379", "127.0.0.1:6379" });
        }

        public async Task ProcessMessageAsync(ulong userId, string originMessage)
        {
            Log(ELogLevel.DEBUG, userId, $"ProcessMessageStart ({originMessage})");

            // 예제이므로 문자열 파싱으로 통신
            var splitArr = originMessage.Split(":");
            var command = splitArr[0];
            if (originMessage.Count() == command.Count())
            {
                return;
            }

            var msg = originMessage.Substring(command.Count() + 1);

            switch (command)
            {
                case "subscribe":
                    {
                        var channel = splitArr[1];
                        Subcribe(userId, channel);
                    }
                    break;
                case "unsubscribe":
                    {
                        var channel = splitArr[1];
                        Unsubcribe(userId, channel);
                    }
                    break;
                case "publish":
                    {
                        var channel = splitArr[1];
                        var sendMsg = msg.Substring(channel.Count() + 1);
                        await PublishMsgAsync(userId, channel, sendMsg);
                    }
                    break;
                default:
                    Log(ELogLevel.ERROR, userId, $"NO_HANDLING_COMMAND({command})");
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

            //var sendBuffer = Encoding.UTF8.GetBytes(message);
            Log(ELogLevel.INFO, 0, $"PublishToClient Channel({channel}) Msg({message})");
            foreach (var userId in userIdSet)
            {
                _onRecvMsg?.Invoke(message);
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

        private readonly RedisPubSubService _redisPubSubService;
        private Action<string>? _onRecvMsg = null;
        private ConcurrentDictionary<string, HashSet<ulong>> _subscribeChannelUserIdDict = new ConcurrentDictionary<string, HashSet<ulong>>();
    }
}
