# 0.x から 1.0 への移行

1.0 では公開APIを固定するため、互換性のために残していたAPIの削除と、名前の変更を行いました。このガイドでは、変更点と書き換え方を説明します。すべての変更は [CHANGELOG_JA.md](CHANGELOG_JA.md) を参照してください。

[English](MIGRATION.md)

## 接続

コンストラクタを、ファクトリ `McpX.Connect` / `McpX.ConnectAsync` と `McpXOptions` に置き換えました。

```csharp
// 0.x
using var mcpx = new McpX("192.168.12.88", 10000, password: "1234", isAscii: true, isUdp: false,
    requestFrame: RequestFrame.E4, timeoutMilliseconds: 3000, processorSeries: ProcessorSeries.iQR);

// 1.0
using var mcpx = McpX.Connect(new McpXOptions
{
    Host = "192.168.12.88",
    Port = 10000,
    Password = "1234",
    IsAscii = true,
    IsUdp = false,
    RequestFrame = RequestFrame.E4,
    Timeout = TimeSpan.FromMilliseconds(3000),   // 無期限は Timeout.InfiniteTimeSpan（従来の 0）
    ProcessorSeries = ProcessorSeries.iQR,
});

// 既定の設定で接続する場合
using var mcpx = McpX.Connect("192.168.12.88", 10000);

// 非同期で接続する場合（スレッドを止めません）
await using var mcpx = await McpX.ConnectAsync(options, cancellationToken);
```

`McpX` の `IsAscii`、`RequestFrame`、`ProcessorSeries` は読み取り専用です。`McpX` は sealed です。

## GX Simulator3

`McpXSimulator` は静的クラスになりました。

```csharp
// 0.x
using var sim = new McpXSimulator(systemNo: 2, cpuNo: 1, ip: "192.168.12.90", timeoutMilliseconds: 10000);

// 1.0
using var sim = McpXSimulator.Connect(systemNo: 2, cpuNo: 1, host: "192.168.12.90");

// ほかの設定も変える場合
var options = McpXSimulator.CreateOptions(systemNo: 2, cpuNo: 1, host: "192.168.12.90");
options.Timeout = TimeSpan.FromSeconds(10);
using var sim = McpX.Connect(options);
```

`SystemNo` / `CpuNo` プロパティは削除しました。接続時に指定した値を保持してください。

## 型ごとのメソッド

型ごとのラッパー（`ReadInt16`、`BatchReadSingleAsync`、`WriteDouble` など）を削除しました。ジェネリックメソッドを使用してください。

| 0.x | 1.0 |
|---|---|
| `ReadInt16(prefix, address)` | `Read<short>(prefix, address)` |
| `BatchReadSingle(prefix, address, length)` | `BatchRead<float>(prefix, address, length)` |
| `WriteDouble(prefix, address, value)` | `Write<double>(prefix, address, value)` |
| `BatchWriteUInt32Async(prefix, address, values)` | `BatchWriteAsync<uint>(prefix, address, values)` |

## ランダムアクセスとモニタ

タプル形式のメソッドを削除しました。ビルダーを使用してください。

```csharp
// 0.x
var (words, doubleWords) = mcpx.RandomRead<short, int>(
    [(Prefix.D, "0"), (Prefix.D, "10")],
    [(Prefix.D, "20")]);

// 1.0
short d0 = 0, d10 = 0;
int d20 = 0;
mcpx.RandomRead(b => b
    .Add<short>(Prefix.D, "0", v => d0 = v)
    .Add<short>(Prefix.D, "10", v => d10 = v)
    .Add<int>(Prefix.D, "20", v => d20 = v));
```

```csharp
// 0.x
mcpx.RandomWrite<short, int>([(Prefix.D, "0", (short)1)], [(Prefix.D, "20", 2)]);
mcpx.RandomWriteBit([(Prefix.M, "0", true)]);

// 1.0（ビット・ワード・ダブルワードを混在できます）
mcpx.RandomWrite(b => b
    .Add<short>(Prefix.D, "0", 1)
    .Add<int>(Prefix.D, "20", 2)
    .Add(Prefix.M, "0", true));
```

```csharp
// 0.x
mcpx.MonitorRegist([(Prefix.D, "0")], [(Prefix.D, "20")]);
var (words, doubleWords) = mcpx.Monitor<short, int>([(Prefix.D, "0")], [(Prefix.D, "20")]);

// 1.0
var session = mcpx.MonitorRegister(b => b
    .Add<short>(Prefix.D, "0", v => d0 = v)
    .Add<int>(Prefix.D, "20", v => d20 = v));
session.Read();   // 必要なだけ繰り返す
```

ビルダー版の `MonitorRegist` / `MonitorRegistAsync` は、`MonitorRegister` / `MonitorRegisterAsync` に名前を変更しました。

## その他の変更

| 変更 | 対応 |
|---|---|
| `BatchWrite` / `BatchWriteAsync` が値を返さない | 戻り値の代わりに、渡した配列を使用してください |
| 点数が `int`（`BatchRead`、`ReadString`、`ReadBuilder.Add`、`BlockReadBuilder.Add`） | リテラルの場合は変更不要です。`(ushort)` のキャストは外せます |
| `ReadString(prefix, address, length)` の引数名を `wordLength` に変更 | 名前付き引数を変更してください |
| `RemoteReset(int reconnectTimeoutMilliseconds)` が `TimeSpan? reconnectTimeout` を受け取る | `RemoteReset(TimeSpan.FromSeconds(30))`。再接続しない場合は `TimeSpan.Zero` |
| すべての非同期メソッドが省略可能な `CancellationToken` を受け取る | キャンセルする場合に渡してください。TCP で通信中にキャンセルすると、接続を閉じます |
| `RecivePacketException` を `ReceivePacketException` に名前変更 | `catch` を変更してください。ライブラリの例外はすべて `McpXException` の派生クラスです |
| 名前空間 `McpXLib.Abstructs` を `McpXLib.Abstracts` に名前変更 | 中の型は公開されなくなったため、`using` を削除してください |
| `Mcp`、`BasePlc`、`Request` / `RequestAsync`、`Route`、`McpXLib.Interfaces` のインターフェースを非公開に変更 | `McpX` のメソッドを使用してください。他局へのアクセスは `McpXOptions.Route` を使用してください |
| ビルダー（`ReadBuilder` など）を `new` で生成できない | `McpX` のメソッドに渡されるビルダーを使用してください |
| `PlcDuplicationException` を削除 | `catch` を削除してください |
| `Dispose` の後の呼び出しが `ObjectDisposedException` をスローする（TCP は `IOException` でした） | 依存していた場合は `ObjectDisposedException` を捕捉してください |
| ワードデバイスへの `bool` が、送信前に `ArgumentException` をスローする（PLC の C05C による `McProtocolException` でした） | `short` などのワードの型を使うか、ビットデバイスを指定してください |
| Q/L で FFFFFF を超えるアドレスが `DeviceAddressException` をスローする | そのアドレスには `ProcessorSeries.iQR` を使用してください |
| netstandard2.0 版は .NET Framework 4.6.2 以降が必要 | 4.6.1 から更新してください |
