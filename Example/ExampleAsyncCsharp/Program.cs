using McpXLib;
using McpXLib.Enums;

// Ctrl+C で、接続・通信をキャンセルする
// Cancel the connection and requests with Ctrl+C
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

// PLC に非同期で接続する（スレッドを止めずに接続する）
// Connect to the PLC asynchronously (without blocking a thread)
await using var mcpx = await McpX.ConnectAsync(new McpXOptions
{
    Host = "192.168.12.88",
    Port = 10000,
}, cts.Token);


// 単一デバイスの読み書き
// Single device access
await mcpx.WriteAsync(Prefix.M, "0", true, cts.Token);
await mcpx.WriteAsync<short>(Prefix.D, "0", 32767, cts.Token);
await mcpx.WriteAsync(Prefix.D, "2", 2147483647, cts.Token);

Console.WriteLine($"M0: {await mcpx.ReadAsync<bool>(Prefix.M, "0", cts.Token)}");
Console.WriteLine($"D0: {await mcpx.ReadAsync<short>(Prefix.D, "0", cts.Token)}");
Console.WriteLine($"D2: {await mcpx.ReadAsync<int>(Prefix.D, "2", cts.Token)}");


// 連続デバイスの一括読み書き（上限を超える場合は自動で分割する）
// Batch access (requests over the limit are split automatically)
short[] values = Enumerable.Range(0, 2000).Select(i => (short)i).ToArray();
await mcpx.BatchWriteAsync(Prefix.D, "100", values, cts.Token);

short[] read = await mcpx.BatchReadAsync<short>(Prefix.D, "100", values.Length, cts.Token);
Console.WriteLine($"D100-D2099: {read[0]} ... {read[^1]}");


// 連続アクセスとランダムアクセスをまとめて読み込む
// Read contiguous and random devices together
await mcpx.ReadAsync(b => b
    .Add<short>(Prefix.D, "100", 10, v => Console.WriteLine($"D100-D109: {string.Join(", ", v)}"))
    .Add<int>(Prefix.D, "2", v => Console.WriteLine($"D2: {v}")), cts.Token);


// ランダムアクセス
// Random access
await mcpx.RandomWriteAsync(b => b
    .Add<short>(Prefix.D, "8000", 100)
    .Add<int>(Prefix.D, "8010", 200000), cts.Token);

await mcpx.RandomReadAsync(b => b
    .Add<short>(Prefix.D, "8000", v => Console.WriteLine($"D8000: {v}"))
    .Add<int>(Prefix.D, "8010", v => Console.WriteLine($"D8010: {v}")), cts.Token);


// モニタ：1秒ごとに読み出す（Ctrl+C で終了）
// Monitor: read every second (Ctrl+C to stop)
var session = await mcpx.MonitorRegisterAsync(b => b
    .Add<short>(Prefix.D, "8000", v => Console.WriteLine($"D8000: {v}"))
    .Add<int>(Prefix.D, "8010", v => Console.WriteLine($"D8010: {v}")), cts.Token);

try
{
    while (true)
    {
        await session.ReadAsync(cts.Token);
        await Task.Delay(1000, cts.Token);
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("Stopped.");
}
