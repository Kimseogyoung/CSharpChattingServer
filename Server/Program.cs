using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var webSocketConnections = new List<WebSocket>();
app.UseWebSockets();

app.MapGet("/", () => "Hello World!");

app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var socket = await context.WebSockets.AcceptWebSocketAsync();
        webSocketConnections.Add(socket);

        var buffer = new byte[1024 * 4];

        while (socket.State == WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Text)
            {
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"[수신]: {message}");

                var sendBuffer = Encoding.UTF8.GetBytes($"[Echo] {message}");

                foreach (var ws in webSocketConnections.Where(w => w.State == WebSocketState.Open))
                {
                    await ws.SendAsync(new ArraySegment<byte>(sendBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None);
                webSocketConnections.Remove(socket);
                Console.WriteLine("클라이언트 연결 종료");
            }
        }
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

app.Run();
