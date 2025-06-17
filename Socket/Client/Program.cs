// See https://aka.ms/new-console-template for more information
using System.Net.Sockets;
using System.Text;

Console.WriteLine("Hello, World!");
using var client = new TcpClient();

await client.ConnectAsync("127.0.0.1", 5000);
Console.WriteLine("서버에 연결됨");

using var stream = client.GetStream();

while (true)
{
    Console.Write("입력 > ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input)) break;

    // 1. 메시지 → 바이트
    var payload = Encoding.UTF8.GetBytes(input);
    var lengthPrefix = BitConverter.GetBytes(payload.Length).Reverse().ToArray(); // Big endian

    // 2. 전송: [Length][Message]
    await stream.WriteAsync(lengthPrefix);
    await stream.WriteAsync(payload);

    // 3. 응답 수신
    var lengthBuffer = new byte[4];
    await ReadExactAsync(stream, lengthBuffer, 4);
    int responseLength = BitConverter.ToInt32(lengthBuffer.Reverse().ToArray(), 0);

    var responseBuffer = new byte[responseLength];
    await ReadExactAsync(stream, responseBuffer, responseLength);
    var response = Encoding.UTF8.GetString(responseBuffer);

    Console.WriteLine($"응답: {response}");
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