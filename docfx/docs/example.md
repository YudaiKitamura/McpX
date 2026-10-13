# 使用例
リポジトリの [Example](https://github.com/YudaiKitamura/McpX/tree/main/Example) にあるサンプルです。IPアドレス・ポート番号は、PLCのパラメータ設定に合わせて変更してください。

## C# 同期処理の例
```csharp
using McpXLib;
using McpXLib.Enums;

// PLC に接続する（IPアドレス・ポート番号は PLC のパラメータ設定に合わせてください）
// Connect to the PLC (match the IP address and port to the PLC parameters)
// - TCP / バイナリコード / 3Eフレーム / MELSEC-Q/L シリーズ（既定の設定）
//   TCP / binary code / 3E frame / MELSEC-Q/L series (default settings)
using var mcpx = McpX.Connect(new McpXOptions
{
    Host = "192.168.12.88",
    Port = 10000,
    // ProcessorSeries = ProcessorSeries.iQR,      // MELSEC iQ-R シリーズの場合 / for the MELSEC iQ-R series
    // IsAscii = true,                             // ASCII コード交信の場合 / for ASCII code
    // IsUdp = true,                               // UDP 交信の場合 / for UDP
    // Timeout = TimeSpan.FromSeconds(3),          // タイムアウト（既定 5秒） / timeout (default 5 s)
});


// 単一デバイスの読み書き。型に応じてデバイス点数は自動で決まる（int / float は2ワード、double は4ワード）
// Single device access. The number of words follows the type (2 for int / float, 4 for double)
mcpx.Write(Prefix.M, "0", true);
mcpx.Write<short>(Prefix.D, "0", 32767);
mcpx.Write(Prefix.D, "2", 2147483647);
mcpx.Write(Prefix.D, "4", 3.14f);

Console.WriteLine($"M0: {mcpx.Read<bool>(Prefix.M, "0")}");
Console.WriteLine($"D0: {mcpx.Read<short>(Prefix.D, "0")}");
Console.WriteLine($"D2: {mcpx.Read<int>(Prefix.D, "2")}");
Console.WriteLine($"D4: {mcpx.Read<float>(Prefix.D, "4")}");


// 連続デバイスの一括読み書き。1回の要求の上限（960ワードなど）を超える場合は自動で分割する
// Batch access. Requests over the limit (e.g. 960 words) are split automatically
short[] values = Enumerable.Range(0, 2000).Select(i => (short)i).ToArray();
mcpx.BatchWrite(Prefix.D, "100", values);

short[] read = mcpx.BatchRead<short>(Prefix.D, "100", values.Length);
Console.WriteLine($"D100-D2099: {read[0]} ... {read[^1]}");


// 連続アクセスとランダムアクセスを1つのビルダーでまとめて読み込む
// Read contiguous and random devices together with one builder
mcpx.Read(b => b
    .Add<short>(Prefix.D, "100", 10, v => Console.WriteLine($"D100-D109: {string.Join(", ", v)}"))
    .Add<int>(Prefix.D, "2", v => Console.WriteLine($"D2: {v}"))
    .Add<bool>(Prefix.M, "0", v => Console.WriteLine($"M0: {v}")));


// ランダムアクセス（離れたデバイスをまとめて読み書き）
// Random access (devices at separate addresses)
mcpx.RandomWrite(b => b
    .Add<short>(Prefix.D, "8000", 100)
    .Add<int>(Prefix.D, "8010", 200000)
    .Add(Prefix.M, "100", true));

mcpx.RandomRead(b => b
    .Add<short>(Prefix.D, "8000", v => Console.WriteLine($"D8000: {v}"))
    .Add<int>(Prefix.D, "8010", v => Console.WriteLine($"D8010: {v}"))
    .Add<bool>(Prefix.M, "100", v => Console.WriteLine($"M100: {v}")));


// モニタ：一度登録したデバイスを、繰り返し読み出す
// Monitor: register devices once, then read them repeatedly
var session = mcpx.MonitorRegister(b => b
    .Add<short>(Prefix.D, "8000", v => Console.WriteLine($"D8000: {v}"))
    .Add<int>(Prefix.D, "8010", v => Console.WriteLine($"D8010: {v}")));

for (int i = 0; i < 3; i++)
{
    session.Read();
}


// 文字列（Shift_JIS）の読み書き。読み込みはワード数（1ワード = 2バイト）で指定する
// Strings (Shift_JIS). Specify the read length in words (1 word = 2 bytes)
mcpx.WriteString(Prefix.D, "9000", "McpX");
Console.WriteLine($"D9000: {mcpx.ReadString(Prefix.D, "9000", 4)}");
```

## C# 非同期処理の例
```csharp
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
```

## Visual Basic 同期処理の例
```vb
Imports System
Imports McpXLib
Imports McpXLib.Enums

Module Program
    Sub Main()
        ' PLC に接続する（IPアドレス・ポート番号は PLC のパラメータ設定に合わせてください）
        ' Connect to the PLC (match the IP address and port to the PLC parameters)
        Dim options As New McpXOptions With {
            .Host = "192.168.12.88",
            .Port = 10000
        }
        ' options.ProcessorSeries = ProcessorSeries.iQR   ' MELSEC iQ-R シリーズの場合 / for the MELSEC iQ-R series
        ' options.IsAscii = True                          ' ASCII コード交信の場合 / for ASCII code

        Using mcpx As McpX = McpX.Connect(options)

            ' 単一デバイスの読み書き。型に応じてデバイス点数は自動で決まる
            ' Single device access. The number of words follows the type
            mcpx.Write(Of Boolean)(Prefix.M, "0", True)
            mcpx.Write(Of Short)(Prefix.D, "0", CShort(32767))
            mcpx.Write(Of Integer)(Prefix.D, "2", 2147483647)

            Console.WriteLine($"M0: {mcpx.Read(Of Boolean)(Prefix.M, "0")}")
            Console.WriteLine($"D0: {mcpx.Read(Of Short)(Prefix.D, "0")}")
            Console.WriteLine($"D2: {mcpx.Read(Of Integer)(Prefix.D, "2")}")

            ' 連続デバイスの一括読み書き（上限を超える場合は自動で分割する）
            ' Batch access (requests over the limit are split automatically)
            Dim values(1999) As Short
            For i As Integer = 0 To values.Length - 1
                values(i) = CShort(i)
            Next
            mcpx.BatchWrite(Of Short)(Prefix.D, "100", values)

            Dim read As Short() = mcpx.BatchRead(Of Short)(Prefix.D, "100", values.Length)
            Console.WriteLine($"D100-D2099: {read(0)} ... {read(read.Length - 1)}")

            ' ランダムアクセス
            ' Random access
            mcpx.RandomWrite(Sub(b) b.Add(Of Short)(Prefix.D, "8000", CShort(100)).Add(Of Integer)(Prefix.D, "8010", 200000))

            mcpx.RandomRead(Sub(b) b _
                .Add(Of Short)(Prefix.D, "8000", Sub(v) Console.WriteLine($"D8000: {v}")) _
                .Add(Of Integer)(Prefix.D, "8010", Sub(v) Console.WriteLine($"D8010: {v}")))

            ' モニタ：一度登録したデバイスを、繰り返し読み出す
            ' Monitor: register devices once, then read them repeatedly
            Dim session = mcpx.MonitorRegister(Sub(b) b _
                .Add(Of Short)(Prefix.D, "8000", Sub(v) Console.WriteLine($"D8000: {v}")) _
                .Add(Of Integer)(Prefix.D, "8010", Sub(v) Console.WriteLine($"D8010: {v}")))

            For i As Integer = 1 To 3
                session.Read()
            Next

            ' 文字列（Shift_JIS）の読み書き。読み込みはワード数で指定する
            ' Strings (Shift_JIS). Specify the read length in words
            mcpx.WriteString(Prefix.D, "9000", "McpX")
            Console.WriteLine($"D9000: {mcpx.ReadString(Prefix.D, "9000", 4)}")

        End Using
    End Sub
End Module
```

## Visual Basic 非同期処理の例
```vb
Imports System
Imports System.Threading.Tasks
Imports McpXLib
Imports McpXLib.Enums

Module Program
    Sub Main()
        RunAsync().GetAwaiter().GetResult()
    End Sub

    Async Function RunAsync() As Task
        ' PLC に非同期で接続する（スレッドを止めずに接続する）
        ' Connect to the PLC asynchronously (without blocking a thread)
        Dim options As New McpXOptions With {
            .Host = "192.168.12.88",
            .Port = 10000
        }
        ' Using を抜けると接続を閉じる（VB には Await Using がないため、Dispose で閉じる）
        ' The connection is closed at End Using (VB has no Await Using, so Dispose closes it)
        Using mcpx As McpX = Await McpX.ConnectAsync(options)
            ' 単一デバイスの読み書き
            ' Single device access
            Await mcpx.WriteAsync(Of Boolean)(Prefix.M, "0", True)
            Await mcpx.WriteAsync(Of Short)(Prefix.D, "0", CShort(32767))

            Console.WriteLine($"M0: {Await mcpx.ReadAsync(Of Boolean)(Prefix.M, "0")}")
            Console.WriteLine($"D0: {Await mcpx.ReadAsync(Of Short)(Prefix.D, "0")}")

            ' 連続デバイスの一括読み書き（上限を超える場合は自動で分割する）
            ' Batch access (requests over the limit are split automatically)
            Dim values(1999) As Short
            For i As Integer = 0 To values.Length - 1
                values(i) = CShort(i)
            Next
            Await mcpx.BatchWriteAsync(Of Short)(Prefix.D, "100", values)

            Dim read As Short() = Await mcpx.BatchReadAsync(Of Short)(Prefix.D, "100", values.Length)
            Console.WriteLine($"D100-D2099: {read(0)} ... {read(read.Length - 1)}")

            ' ランダムアクセス
            ' Random access
            Await mcpx.RandomReadAsync(Sub(b) b _
                .Add(Of Short)(Prefix.D, "8000", Sub(v) Console.WriteLine($"D8000: {v}")) _
                .Add(Of Integer)(Prefix.D, "8010", Sub(v) Console.WriteLine($"D8010: {v}")))

            ' モニタ
            ' Monitor
            Dim session = Await mcpx.MonitorRegisterAsync(Sub(b) b _
                .Add(Of Short)(Prefix.D, "8000", Sub(v) Console.WriteLine($"D8000: {v}")))

            For i As Integer = 1 To 3
                Await session.ReadAsync()
                Await Task.Delay(1000)
            Next
        End Using
    End Function
End Module
```
