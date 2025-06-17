using Server;

var builder = Host.CreateApplicationBuilder(args);

// AddHostedService�� ��Ͻ� �ڵ� �����(BackgroundService)
builder.Services.AddHostedService<Worker>();

// ���� DI ���
builder.Services.AddSingleton<ChatService>();

var host = builder.Build();
host.Run();
