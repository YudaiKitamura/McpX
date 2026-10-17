<img src="assets/mcpx_ogp.png" alt="logo" />
<br>
<p>
  <img alt="Downloads" src="https://img.shields.io/nuget/dt/McpX" />
  <img alt="Version" src="https://img.shields.io/badge/version-1.0.0--rc.1-blue" />
  <img alt=".NET 7.0+" src="https://img.shields.io/badge/.NET-7.0+-blueviolet" />
  <img alt=".NET 8.0+" src="https://img.shields.io/badge/.NET-8.0+-purple" />
  <img alt=".NET 9.0+" src="https://img.shields.io/badge/.NET-9.0+-indigo" />
  <img alt=".NET 10.0+" src="https://img.shields.io/badge/.NET-10.0+-darkviolet" />
  <img alt=".NET Standard 2.0" src="https://img.shields.io/badge/.NET_Standard-2.0-darkgreen" />
  <img alt=".NET Framework 4.6.2+" src="https://img.shields.io/badge/.NET_Framework-4.6.2+-teal?logo=windows" />
  <img alt="License" src="https://img.shields.io/badge/license-MIT-brightgreen.svg" />
  <a href="https://flatt.tech/oss/gmo/trampoline" target="_blank"><img src="https://flatt.tech/assets/images/badges/gmo-oss.svg" height="24px"/></a>
</p>

<p>
  <a href="README_JA.md">日本語</a> | <a href="README.md">English</a>
</p>

**公式サイト・ドキュメント: [mcpx.yktmr.jp](https://mcpx.yktmr.jp/)** — 概要、[API リファレンス](https://mcpx.yktmr.jp/docs)、[MCプロトコル入門](https://mcpx.yktmr.jp/learn)

McpXは、三菱電機製PLCと通信するためのMCプロトコル対応ライブラリです。<br>
シンプルなAPIで扱いやすく、MCプロトコルを意識することなく利用でき、Linux、Windows、macOS など、さまざまなプラットフォームで動作します。

> 0.x から移行する場合は、[移行ガイド](MIGRATION_JA.md)を参照してください。

## インストール方法
### .NET CLI
```sh
dotnet add package McpX
```
### Package Manager(Visual Studio)
```sh
PM> NuGet\Install-Package McpX
```

## 使用例
```csharp
using McpXLib;
using McpXLib.Enums;

// PLCに接続（既定は TCP・バイナリコード・3Eフレーム・MELSEC-Q/Lシリーズ）
using var mcpx = McpX.Connect("192.168.12.88", 10000);

// M0から7000点を読み込み（1回の交信の上限を超える場合は自動で分割）
bool[] mArr = mcpx.BatchRead<bool>(Prefix.M, "0", 7000);

// D1000から7000ワードを読み込み
short[] dArr = mcpx.BatchRead<short>(Prefix.D, "1000", 7000);

// D0〜D1に1234、D2〜D3に5678を符号付き32ビット整数で書き込み
mcpx.BatchWrite<int>(Prefix.D, "0", [1234, 5678]);

// 範囲指定と単一指定のデバイスをまとめて読み込み（範囲は連続アクセス、単一はランダムアクセス）
short[] block = [];
int total = 0;
mcpx.Read(b => b
    .Add<short>(Prefix.D, "400", 100, v => block = v)
    .Add<int>(Prefix.D, "2000", v => total = v));
```
すべてのメソッドに、`CancellationToken` を受け取る非同期版があります。
```csharp
await using var mcpx = await McpX.ConnectAsync("192.168.12.88", 10000, cancellationToken);
short d0 = await mcpx.ReadAsync<short>(Prefix.D, "0", cancellationToken);
```
[C#、Visual Basic のサンプルはこちらを参照してください。](https://github.com/YudaiKitamura/McpX/tree/main/Example)

### 接続設定
PLCのパラメータ設定に合わせて、`McpXOptions` を `McpX.Connect` / `McpX.ConnectAsync` に渡します。
```csharp
using var mcpx = await McpX.ConnectAsync(new McpXOptions
{
    Host = "192.168.12.89",
    Port = 5007,
    ProcessorSeries = ProcessorSeries.iQR,  // MELSEC iQ-Rシリーズ（拡張デバイス指定）
    RequestFrame = RequestFrame.E4,
    IsAscii = false,
    IsUdp = false,
    Timeout = TimeSpan.FromSeconds(3),
    Password = "1234",                      // リモートパスワード（接続時に解除、破棄時にロック）
});
```

| プロパティ | 説明 | 既定値 |
|---|---|---|
| `Host` | PLCのIPアドレスまたはホスト名 | （必須） |
| `Port` | ポート番号（1〜65535） | （必須） |
| `Password` | リモートパスワード。接続時に解除し、`Dispose` / `DisposeAsync` でロックします | `null` |
| `IsAscii` | ASCIIコードは `true`、バイナリコードは `false` | `false` |
| `IsUdp` | UDPは `true`、TCPは `false` | `false` |
| `RequestFrame` | `RequestFrame.E3`（3Eフレーム）／ `RequestFrame.E4`（4Eフレーム） | `E3` |
| `ProcessorSeries` | `ProcessorSeries.Q`（MELSEC-Q/Lのデバイス指定）／ `ProcessorSeries.iQR`（MELSEC iQ-Rの拡張デバイス指定） | `Q` |
| `Timeout` | 接続と、各要求のタイムアウト時間。`Timeout.InfiniteTimeSpan` で無期限 | 5秒 |
| `UseMultiBlockAccess` | 統合アクセス（`Read` / `Write`）の範囲指定に、複数ブロック一括読出し・書込み（コマンド: 0406 / 1406）を使う | `false` |
| `Route` | 他局・別の号機へのアクセス経路（下記参照） | 自局 |

### 他局へのアクセス
`McpXRoute`（ネットワーク番号、PC番号、要求先ユニットI/O番号、要求先ユニット局番号）を指定すると、ネットワーク上の他局や、マルチCPUシステムの別の号機にアクセスできます。
```csharp
// ネットワーク番号1・PC番号2の局
var options = new McpXOptions { Host = "192.168.12.89", Port = 5007, Route = new McpXRoute(networkNo: 1, pcNo: 2) };

// 接続したユニットと同じマルチCPUシステムの2号機
options.Route = McpXRoute.ForCpu(2);
```

### GX Simulator3 への接続
`McpXSimulator` を使うと、システムNo.と号機No.を指定して GX Simulator3（GX Works3 のシミュレーション機能）に接続できます。
ポート番号は `5500 + システムNo. × 10 + 号機No.` です（例：システム1・号機1 = 5511、システム2・号機1 = 5521）。
複数のシミュレータに同時に接続する場合は、シミュレータごとに接続します。返されるのは通常の `McpX` です。
```csharp
using var sim1 = McpXSimulator.Connect();                                   // 127.0.0.1:5511（システム1・号機1）
using var sim2 = McpXSimulator.Connect(systemNo: 2, host: "192.168.12.90"); // 192.168.12.90:5521
using var cpu2 = await McpXSimulator.ConnectAsync(systemNo: 1, cpuNo: 2);   // 127.0.0.1:5512（マルチCPUの2号機）

// ほかの設定も変える場合は、CreateOptions で作った設定を変更して接続する
var options = McpXSimulator.CreateOptions(systemNo: 1, cpuNo: 1);
options.Timeout = TimeSpan.FromSeconds(10);
using var sim3 = McpX.Connect(options);
```
GX Simulator3 は 127.0.0.1 でのみ待ち受けます。別のPCから接続する場合は、シミュレータ側のPCで各ポートを 127.0.0.1 へ転送してください（`netsh interface portproxy` など）。
交信はTCP・バイナリコードのみです（GX Simulator3 はASCIIコードの交信に応答しません）。

## 対応コマンド

非同期メソッドは、いずれも省略可能な `CancellationToken` を受け取ります。

| 名称                     | 説明                                             | 同期メソッド                                     | 非同期メソッド                                               |
|------------------------|------------------------------------------------|---------------------------------------------|------------------------------------------------------------|
| **単一読出し**          | デバイスの単一値を取得します。                       | `Read<T>(Prefix prefix, string address)`   | `ReadAsync<T>(Prefix prefix, string address)`             |
| **単一書込み**          | デバイスに単一値を書き込みます。                       | `Write<T>(Prefix prefix, string address, T value)` | `WriteAsync<T>(Prefix prefix, string address, T value)`   |
| **一括読出し**          | 連続したデバイスから、指定数のデータを一括で読み出します。    | `BatchRead<T>(Prefix prefix, string address, int length)` | `BatchReadAsync<T>(Prefix prefix, string address, int length)` |
| **一括書込み**          | 複数のデバイスに配列で指定した値を一括書き込みします。        | `BatchWrite<T>(Prefix prefix, string address, T[] values)`  | `BatchWriteAsync<T>(Prefix prefix, string address, T[] values)`  |
| **ランダム読出し**       | 非連続デバイス（ビット／ワード／ダブルワード）を読み出します。   | `RandomRead(Action<RandomReadBuilder> build)`   | `RandomReadAsync(Action<RandomReadBuilder> build)`        |
| **ランダム書込み**       | 非連続デバイス（ビット／ワード／ダブルワード）へ書き込みます。   | `RandomWrite(Action<RandomWriteBuilder> build)` | `RandomWriteAsync(Action<RandomWriteBuilder> build)`      |
| **モニタ登録**           | モニタ対象デバイスを登録し、`MonitorSession` を返します。    | `MonitorRegister(Action<MonitorBuilder> build)`   | `MonitorRegisterAsync(Action<MonitorBuilder> build)`        |
| **モニタ読み取り**        | 返却されたセッション経由で登録済みデバイスの最新値を読み出します。 | `MonitorSession.Read()`                    | `MonitorSession.ReadAsync()`                              |
| **統合読み込み**          | 範囲指定（点数あり）と単一指定のデバイスをまとめて読み込みます。範囲は連続アクセス（`UseMultiBlockAccess` を有効にすると、範囲が複数あれば複数ブロック一括読出し）、単一はランダムアクセスで読み込みます。 | `Read(Action<ReadBuilder> build)`   | `ReadAsync(Action<ReadBuilder> build)`   |
| **統合書き込み**          | 範囲指定（配列）と単一指定のデバイスにまとめて書き込みます。配列は連続アクセス（`UseMultiBlockAccess` を有効にすると、範囲が複数あれば複数ブロック一括書込み）、単一値はランダムアクセスで書き込みます。 | `Write(Action<WriteBuilder> build)` | `WriteAsync(Action<WriteBuilder> build)` |
| **複数ブロック一括読出し**  | 連続したデバイスの範囲（ブロック）を複数指定し、1回の交信でまとめて読み出します（コマンド: 0406）。 | `BlockRead(Action<BlockReadBuilder> build)`   | `BlockReadAsync(Action<BlockReadBuilder> build)`   |
| **複数ブロック一括書込み**  | 連続したデバイスの範囲（ブロック）を複数指定し、1回の交信でまとめて書き込みます（コマンド: 1406）。 | `BlockWrite(Action<BlockWriteBuilder> build)` | `BlockWriteAsync(Action<BlockWriteBuilder> build)` |
| **文字列の読み書き**      | Shift_JIS の文字列を読み書きします。読み込みの長さはワード数で指定します。 | `ReadString(Prefix prefix, string address, int wordLength)` / `WriteString(Prefix prefix, string address, string value)` | `ReadStringAsync(...)` / `WriteStringAsync(...)` |
| **リモートRUN / STOP / PAUSE** | CPUユニットの動作状態を変更します（コマンド: 1001 / 1002 / 1003）。 | `RemoteRun(bool force, RemoteRunClearMode clearMode)` / `RemoteStop()` / `RemotePause(bool force)` | `RemoteRunAsync(...)` / `RemoteStopAsync()` / `RemotePauseAsync(...)` |
| **リモートラッチクリア / RESET** | ラッチクリア、リセットを実行します（コマンド: 1005 / 1006）。STOP状態で実行してください。`RemoteReset` はリセット後に接続し直します。 | `RemoteLatchClear()` / `RemoteReset(TimeSpan? reconnectTimeout)` | `RemoteLatchClearAsync()` / `RemoteResetAsync(...)` |
| **リモートパスワード 解除/ロック** | `McpXOptions.Password` を指定すると、接続時に解除し、破棄時にロックします（コマンド: 1630 / 1631）。 | `McpX.Connect(options)` / `Dispose()` | `McpX.ConnectAsync(options)` / `DisposeAsync()` |

`T` には、`bool`、`short`、`ushort`、`int`、`uint`、`float`、`long`、`ulong`、`double`、`byte`、`sbyte` を指定できます（`byte` / `sbyte` は1ワードに1つ）。ランダムアクセスとモニタで指定できるのは、`bool`、`short`、`ushort`、`int`、`uint`、`float` です。

## 例外とスレッドセーフ
- PLCがエラーコードを返した場合は `McProtocolException`（`ErrorCode` に終了コード）、アドレスが不正な場合は `DeviceAddressException`、応答が不正な場合は `ReceivePacketException` をスローします。いずれも `McpXException` の派生クラスです。
- タイムアウトは `TimeoutException` をスローします。TCP 交信でタイムアウト・通信エラー・キャンセルが発生すると接続を閉じ、以降の呼び出しは `IOException` になります（接続し直してください）。
- `Dispose` の後に呼び出すと `ObjectDisposedException` をスローします。
- 1つの `McpX` を、複数のスレッド・タスクから同時に呼び出せます（要求は1つずつ送信します）。複数の要求に分かれる操作（点数の多い一括読出しなど）の途中に、他の要求が割り込むことがあります。

## 対応PLC・プロトコル
| 項目 | 対応 |
|---|---|
| PLCシリーズ | MELSEC iQ-Rシリーズ（`ProcessorSeries.iQR`）、MELSEC-Q/Lシリーズ（`ProcessorSeries.Q`）。3E / 4E フレームに対応した他の機種でも動作する可能性がありますが、未検証です。 |
| プロトコル | TCP、UDP |
| フレーム | 3Eフレーム、4Eフレーム |
| 交信データコード | バイナリコード、ASCIIコード |
| シミュレータ | GX Simulator3 |

## PLCのパラメータ設定
内蔵Ethernetを使用する場合は、GX Works3 で次のように設定します。

<img src="assets/plc_parameter_1.png" width="500" />
<img src="assets/plc_parameter_2.png" width="500" />

| 設定項目 | 説明 | McpX の接続設定（`McpXOptions`） |
|:-|:-|:-|
| IPアドレス | PLCのIPアドレスを指定します。 | `Host` にPLCのIPアドレスを指定します。 |
| 交信データコード | `バイナリコード交信`、`ASCIIコード交信`を指定します。 | `ASCIIコード交信`の場合は `IsAscii` に `true` を指定します。 |
| RUN中書込を許可する | デバイスに書込みを行う場合に有効にします。 | - |
| プロトコル | `TCP`、`UDP`を指定します。 | `UDP`の場合は `IsUdp` に `true` を指定します。 |
| オープン方式 | `MCプロトコル`を指定します。 | - |
| 自局ポート番号 | 通信を行うポート番号（1025～4999、5010～65534の範囲）を指定します。 | `Port` にPLCのポート番号を指定します。 |

## バージョニング
1.0.0 以降は [セマンティック バージョニング](https://semver.org/lang/ja/) に従います。公開APIの互換性がなくなる変更は、メジャーバージョンでのみ行います。

## ネイティブライブラリ（C ABI）
McpX を C の関数として公開するネイティブライブラリ（`mcpx.dll` / `mcpx.so` / `mcpx.dylib`）です。.NET ランタイムは不要で、Python（ctypes）や Node.js（koffi）など、FFI を持つ言語から利用できます。各[リリース](https://github.com/YudaiKitamura/McpX/releases)に添付しています。
ヘッダは [McpXInterop/include/mcpx.h](McpXInterop/include/mcpx.h) です。単一・連続・ランダム・複数ブロックの読み書き、文字列、モニタ、リモート操作に対応し、Windows x64 / Linux x64・arm64 / macOS arm64 向けにビルドしています。すべての関数はステータスコード（`MCPX_OK = 0`）を返し、失敗時は `mcpx_error` に詳細（PLC の終了コードを含む）を書き込みます。

```c
#include "mcpx.h"

mcpx_connect_options options;
mcpx_connect_options_init(&options);
options.host = "192.168.12.88";
options.port = 10000;

mcpx_error err = { .struct_size = sizeof(mcpx_error) };
mcpx_client_t plc;
if (mcpx_connect(&options, &plc, &err) == MCPX_OK)
{
    int16_t values[10];
    if (mcpx_batch_read(plc, MCPX_PREFIX_D, "0", MCPX_TYPE_I16, 10, values, sizeof(values), &err) != MCPX_OK)
    {
        printf("%s\n", err.message);
    }
    mcpx_close(plc, &err);
}
```

## 今後の予定
- [x] ~~3Eフレーム（ASCIIコード）対応~~
- [x] ~~4Eフレーム（バイナリコード）対応~~
- [x] ~~4Eフレーム（ASCIIコード）対応~~
- [x] ~~UDP対応~~
- [x] ~~GX Simulator 対応~~
- [x] ~~他局へのアクセス~~
- [ ] Python / Node.js ラッパー - 準備中

## 変更履歴
- [CHANGELOG_JA.md](./CHANGELOG_JA.md)

## 関連
- [mcpx-mcp-server](https://github.com/YudaiKitamura/mcpx-mcp-server) 生成AIから三菱電機製 PLC のデバイスへリアルタイムアクセスを可能にする MCP(Model Context Protocol)サーバー
