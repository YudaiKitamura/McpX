using System.Globalization;
using System.Net;
using System.Net.Sockets;
using TestMcpX;

// TCP の偽 PLC サーバー（各言語のラッパーのテスト用）。
//
//   dotnet run --project tools/FakePlcServer -- [--end-code 0401=C051]... [--delay-ms 2000]
//
// ループバックの空きポートで待ち受け、"PORT=<番号>" を標準出力に書き出す。
// 書き込んだ値は保持され、読み込みで返る（未書き込みのワードは「デバイス番号の下位16ビット」）。
// 標準入力が閉じられると終了する（テストのプロセスが終われば一緒に終了する）。

var core = new FakePlcCore { Stateful = true };
var delay = TimeSpan.Zero;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--end-code" when i + 1 < args.Length:
            var pair = args[++i].Split('=');
            core.EndCodes[ushort.Parse(pair[0], NumberStyles.HexNumber)] = ushort.Parse(pair[1], NumberStyles.HexNumber);
            break;
        case "--delay-ms" when i + 1 < args.Length:
            delay = TimeSpan.FromMilliseconds(int.Parse(args[++i]));
            break;
        default:
            Console.Error.WriteLine($"unknown argument: {args[i]}");
            return 2;
    }
}

var listener = new TcpListener(IPAddress.Loopback, 0);
listener.Start();
Console.WriteLine($"PORT={((IPEndPoint)listener.LocalEndpoint).Port}");
Console.Out.Flush();

_ = Task.Run(async () =>
{
    while (true)
    {
        var client = await listener.AcceptTcpClientAsync();
        _ = Task.Run(() => ServeAsync(client));
    }
});

// 標準入力が閉じられるまで待つ
await Console.In.ReadToEndAsync();
listener.Stop();
return 0;

async Task ServeAsync(TcpClient client)
{
    using (client)
    using (var stream = client.GetStream())
    {
        try
        {
            while (true)
            {
                // 50 00 | NW PC IO(2) ST | 長さ(2) | 以降「長さ」バイト
                var header = await ReadExactlyAsync(stream, 9);
                var body = await ReadExactlyAsync(stream, BitConverter.ToUInt16(header, 7));

                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay);
                }

                byte[] response;
                lock (core)
                {
                    response = core.Handle([.. header, .. body]);
                }

                await stream.WriteAsync(response);
            }
        }
        catch (Exception)
        {
            // 切断されたら終了
        }
    }
}

static async Task<byte[]> ReadExactlyAsync(NetworkStream stream, int length)
{
    var buffer = new byte[length];
    await stream.ReadExactlyAsync(buffer);
    return buffer;
}
