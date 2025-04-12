<h1>McpX</h1>
<p>
  <img alt="Version" src="https://img.shields.io/badge/version-0.2.0-blue" />
  <img alt=".NET 7.0+" src="https://img.shields.io/badge/.NET-7.0+-blueviolet" />
  <img alt=".NET 8.0+" src="https://img.shields.io/badge/.NET-8.0+-purple" />
  <img alt=".NET 9.0+" src="https://img.shields.io/badge/.NET-9.0+-indigo" />
  <img alt=".NET Core 2.0+" src="https://img.shields.io/badge/.NET_Core-2.0+-darkgreen" />
  <img alt=".NET Framework 4.6.1+" src="https://img.shields.io/badge/.NET_Framework-4.6.1+-teal?logo=windows" />
  <img alt="License" src="https://img.shields.io/badge/license-MIT-brightgreen.svg" />
</p>

<p>
  <a href="README_JA.md">日本語</a> | <a href="README.md">English</a>
</p>

McpXは、三菱電機製PLCと通信するためのMCプロトコル対応ライブラリです。<br>
シンプルなAPIで扱いやすく、MCプロトコルを意識することなく利用でき、Linux、Windows、macOS など、さまざまなプラットフォームで動作します。

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

// IP、Portを指定してPLCに接続
using (var mcpx = new McpX("192.168.12.88", 10000))
{
    // M0から7000点取得
    bool[] mArr = mcpx.BatchRead<bool>(Prefix.M, "0", 7000);
    
    // D1000から7000ワード取得
    short[] dArr = mcpx.BatchRead<short>(Prefix.D, "1000", 7000);

    // D0に1234、D1に5678を符号あり32ビットで書込み 
    mcpx.BatchWrite<int>(Prefix.D, "0", [1234, 5678]);
}
```
[C#、Visual Basicのサンプルはこちら](https://github.com/YudaiKitamura/McpX/tree/main/Example)

## 対応コマンド

| 名称                     | 説明                                             | 同期メソッド                                     | 非同期メソッド                                               |
|------------------------|------------------------------------------------|---------------------------------------------|------------------------------------------------------------|
| **単一読出し**          | デバイスの単一値を取得します。                       | `Read<T>(Prefix prefix, string address)`   | `ReadAsync<T>(Prefix prefix, string address)`             |
| **単一書込み**          | デバイスに単一値を書き込みます。                       | `Write<T>(Prefix prefix, string address, T value)` | `WriteAsync<T>(Prefix prefix, string address, T value)`   |
| **一括読出し**          | 連続したデバイスから、指定数のデータを一括で読み出します。    | `BatchRead<T>(Prefix prefix, string address, ushort length)` | `BatchReadAsync<T>(Prefix prefix, string address, ushort length)` |
| **一括書込み**          | 複数のデバイスに配列で指定した値を一括書き込みします。        | `BatchWrite<T>(Prefix prefix, string address, T[] values)`  | `BatchWriteAsync<T>(Prefix prefix, string address, T[] values)`  |
| **ランダム読出し**       | 非連続アドレスからワード・ダブルワード単位で読み出します。       | `RandomRead<T1, T2>((Prefix, string)[] wordAddresses, (Prefix, string)[] doubleWordAddresses)` | `RandomReadAsync<T1, T2>((Prefix, string)[] wordAddresses, (Prefix, string)[] doubleWordAddresses)` |
| **ランダム書込み**       | 非連続アドレスにワード・ダブルワード単位で書き込みます。        | `RandomWrite<T1, T2>(...)`                 | `RandomWriteAsync<T1, T2>(...)`                            |
| **モニタ登録**           | モニタ対象デバイスを登録します。                         | `MonitorRegist((Prefix, string)[] wordAddresses, (Prefix, string)[] doubleWordAddresses)` | `MonitorRegistAsync((Prefix, string)[] wordAddresses, (Prefix, string)[] doubleWordAddresses)` |
| **モニタ読み取り**        | 登録済みモニタデバイスの最新値を読み出します。                | `Monitor<T1, T2>(...)`                     | `MonitorAsync<T1, T2>(...)`                                |
| **リモートパスワード ロック/アンロック** | リモートパスワード指定時、インスタンス生成時にロック、破棄時に自動アンロックします。 | `McpX(string ip, int port, string? password = null)` | －                                                          |


## 対応プロトコル
- TCP
- UDP
- 3Eフレーム（バイナリコード）
- 3Eフレーム（ASCIIコード）
- 4Eフレーム（バイナリコード）
- 4Eフレーム（ASCIIコード）

## 今後の予定
- [x] ~~3Eフレーム（ASCIIコード）対応~~
- [x] ~~4Eフレーム（バイナリコード）対応~~
- [x] ~~4Eフレーム（ASCIIコード）対応~~
- [x] ~~UDP対応~~
