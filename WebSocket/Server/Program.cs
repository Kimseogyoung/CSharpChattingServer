using Server;
using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ChatService>();

var app = builder.Build();

var webSocketConnections = new List<WebSocket>();
app.UseWebSockets();

app.MapGet("/", () => "Hello World!");

app.Map("/ws", async context =>
{
    var charSvc = context.RequestServices.GetRequiredService<ChatService>();
    await charSvc.ProcessAsync(context); 
});

app.Run();
