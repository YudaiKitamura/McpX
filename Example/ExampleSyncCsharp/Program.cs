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
