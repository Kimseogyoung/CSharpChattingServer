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
    public enum ESessionState
    {
        PENDING,    // 아직 인증/로그인 전
        AUTHENTICATED // 인증/로그인 완료
    }

    public class Session
    {
        public string Id { get; }
        public TcpClient Client { get; }
        public DateTime ConnectTime { get; set; }
        public DateTime LastActivityTime { get; set; }
        public ulong UserId { get; set; }
        public ESessionState State { get; set; }

        public Session(TcpClient client)
        {
            this.Id = System.Guid.NewGuid().ToString();
            this.Client = client;
            this.ConnectTime = DateTime.UtcNow;
            this.LastActivityTime = DateTime.UtcNow;
            this.State = ESessionState.PENDING;
        }

        public void Authenticate(ulong userId)
        {
            this.UserId = userId;
            this.State |= ESessionState.AUTHENTICATED;
        }
    }

    public class SessionService
    {
 
        public SessionService(ILogger<SessionService> logger)
        {
            _logger = logger;
        }

        public Session CreateSession(TcpClient client)
        {
            var session = new Session(client);
            if (!_sessionDict.TryAdd(session.Id, session))
            {
                throw new Exception($"FAILED_ADD_SESSION Guid({session.Id})");
            }
            return session;
        }

        public Session GetSession(string guid)
        {
            if (!_sessionDict.TryGetValue(guid, out var sessionCtx))
            {
                throw new Exception($"NOT_FOUND_SESSION Guid({guid})");
            }

            return sessionCtx;
        }

        public Session GetSessionByUserId(ulong userId)
        {
            var sessionCtx =_sessionDict.FirstOrDefault(x=>x.Value.UserId == userId).Value;
            if (sessionCtx == null)
            {
                throw new Exception($"NOT_FOUND_SESSION UserId({userId})");
            }

            return sessionCtx;
        }

        public void CloseSession(string guid)
        {
            if (!_sessionDict.TryRemove(guid, out var session))
            {
                return;
            }

            try
            {
                session.Client.Close();
            }
            catch
            {
                _logger.LogError($"FAILED_CLOSE_SESSION_CLIENT Guild({guid})");
            }
        }

        public void AuthenticateSession(string guid, ulong userId)
        {
            var ctx = GetSession(guid);
            ctx.Authenticate(userId);
        }

        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, Session> _sessionDict = new();
    }
}
