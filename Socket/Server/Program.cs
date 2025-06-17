using Server;

var builder = Host.CreateApplicationBuilder(args);

// AddHostedService로 등록시 자동 실행됨(BackgroundService)
builder.Services.AddHostedService<Worker>();

// 서비스 DI 등록
builder.Services.AddSingleton<ChatService>();

var host = builder.Build();
host.Run();
