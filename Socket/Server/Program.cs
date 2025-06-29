using Server;

var builder = Host.CreateApplicationBuilder(args);

// AddHostedService�� ��Ͻ� �ڵ� �����(BackgroundService)
builder.Services.AddHostedService<Worker>();

// ���� DI ���
builder.Services.AddSingleton<SocketService>();
builder.Services.AddSingleton<ChatService>();
builder.Services.AddSingleton<RedisPubSubService>();

var host = builder.Build();
host.Run();