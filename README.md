# C# ChattingServer
1. WebSocket
2. Socket


---
## WebSocket
멀티 인스턴스 환경에서 WebSocket 기반 채팅 또는 알림 서비스를 구성할 때, Redis Pub/Sub를 사용해 서버 간 메시지를 중계하는 구조

```
1. Server → Redis (Subscribe)
2. Client → Server(WebSocket) → Redis (Publish)
3. Redis → All Servers → Connected Clients
```


| 구성 요소         | 역할 |
|------------------|------|
| **Client**       | WebSocket을 통해 서버에 연결 |
| **WebSocket Server** | Redis에 메시지를 `PUBLISH`, Redis 메시지를 `SUBSCRIBE` |
| **Redis**        | 메시지 브로커 (Pub/Sub 시스템) |
| **Redis 채널**   | `chat/room1`, `user/123` 등 주제 기반 메시지 구분 |


## Socket
