using Server;
using System.Net.WebSockets;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ChatService>();
builder.Services.AddSingleton<RedisPubSubService>();

var app = builder.Build();

// Redis 연결
var chatSvc = app.Services.GetRequiredService<ChatService>();
chatSvc.Init();

var webSocketConnections = new List<WebSocket>();
app.UseWebSockets(); // 웹소켓 활성화

app.MapGet("/", () => "This is ChattingServer");

app.Map("/ws", async context =>
{
    var charSvc = context.RequestServices.GetRequiredService<ChatService>();
    await charSvc.ProcessAsync(context); 
});

app.Run();
