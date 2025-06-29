using Server;

var builder = Host.CreateApplicationBuilder(args);

// AddHostedService�� ��Ͻ� �ڵ� �����(BackgroundService)
builder.Services.AddHostedService<Worker>();

// ���� DI ���
builder.Services.AddSingleton<SessionService>();
builder.Services.AddSingleton<SocketService>();
builder.Services.AddSingleton<ISocketSender>(sp => sp.GetRequiredService<SocketService>());
builder.Services.AddSingleton<ChatService>();
builder.Services.AddSingleton<RedisPubSubService>();
builder.Services.AddSingleton<MessageDispatcher>();

var host = builder.Build();
host.Run();