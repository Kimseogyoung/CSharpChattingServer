// See https://aka.ms/new-console-template for more information
using System.Net.Sockets;
using System.Text;
using Protocol;

Console.WriteLine("Hello, World!");
using var client = new TcpClient();

await client.ConnectAsync("127.0.0.1", 5000);
Console.WriteLine("서버에 연결됨");

using var stream = client.GetStream();

// 사용자 ID 입력 및 등록
Console.Write("User ID를 입력하세요 > ");
var userIdInput = Console.ReadLine();
if (!ulong.TryParse(userIdInput, out var userId))
{
    return;
}
var registerPacket = new ChatReqPacket(userId, EChatAction.REGISTER_USER, "", "");
var regPayload = ProtocolParser.Serialize(registerPacket);
var regPrefix = BitConverter.GetBytes(regPayload.Length).Reverse().ToArray();

await stream.WriteAsync(regPrefix);
await stream.WriteAsync(regPayload);

// 서버 응답 수신 루프 시작
_ = Task.Run(async () => await ReceiveLoop(stream));

while (true)
{
    Console.Write("입력 (1. SUBSCRIBE, 2.UNSUBSCRICE, 3.SEND) > ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input))
    {
        break;
    }

    if (!Enum.TryParse<EChatAction>(input, out var action))
    {
        Console.WriteLine("ChatAction 잘못 입력함.");
        continue;
    }

    Console.Write("Channel 입력 > ");
    var channel = Console.ReadLine();
    var reqChat = new ChatReqPacket(userId, action, channel, "");
    if (action == EChatAction.SEND)
    {
        Console.Write("Message 입력 > ");
        var msg = Console.ReadLine();
        reqChat.Message = msg;
    }

    // 1. 메시지 → 바이트
    var payloadBytes = ProtocolParser.Serialize(reqChat);
    var lengthPrefix = BitConverter.GetBytes(payloadBytes.Length).Reverse().ToArray(); // Big endian

    // 2. 전송: [Length][Message]
    await stream.WriteAsync(lengthPrefix);
    await stream.WriteAsync(payloadBytes);
}

Console.WriteLine("연결 종료");

static async Task ReadExactAsync(NetworkStream stream, byte[] buffer, int size)
{
    int total = 0;
    while (total < size)
    {
        int read = await stream.ReadAsync(buffer, total, size - total);
        if (read == 0) throw new Exception("서버 연결 끊김");
        total += read;
    }
}

// 응답 수신 루프
static async Task ReceiveLoop(NetworkStream stream)
{
    try
    {
        while (true)
        {
            var lengthBuffer = new byte[4];
            await ReadExactAsync(stream, lengthBuffer, 4);
            int responseLength = BitConverter.ToInt32(lengthBuffer.Reverse().ToArray(), 0);

            var responseBuffer = new byte[responseLength];
            await ReadExactAsync(stream, responseBuffer, responseLength);
            var resChat = ProtocolParser.Deserialize<ChatResPacket>(responseBuffer);

            Console.WriteLine($"\n[서버 응답] {resChat.Message}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[수신 종료] {ex.Message}");
    }
}