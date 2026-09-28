## [Unreleased]
### Fixed
- PLC が応答しない場合に、TCP の非同期要求がタイムアウトせず永久に待ち続ける不具合を修正（`NetworkStream.ReadAsync` は `ReceiveTimeout` を参照しないため）。
- 同じインスタンスで要求を並行実行（TCP の非同期・UDP）すると、送受信が交錯して応答が入れ替わることがある不具合を修正。要求を1つずつ処理するように変更。
- タイムアウトや通信エラーの後、次の要求が遅れて届いた古い応答を返す不具合を修正。TCP は接続を閉じ、UDP は同期要求のタイムアウト時にもソケットを作り直すように変更。
- TCP の接続が拒否された場合に `AggregateException` がスローされ、`TcpClient` が破棄されない不具合を修正。`TcpClient` を破棄し、元の `SocketException` をスローするように変更。
- `timeoutMilliseconds = 0` を指定すると TCP の接続が必ず失敗する不具合を修正。要求と同じく `0` は無期限として扱う。
- `timeoutMilliseconds` に 1〜249 を指定すると、すべてのコマンドが `ArgumentOutOfRangeException` になる不具合を修正。監視タイマをタイムアウトから決める（タイムアウトより 250ms 短い 250ms 単位の値。500ms 未満は 0）ように変更し、PLC のエラー応答がクライアント側のタイムアウトより先に届くようにした。
- `timeoutMilliseconds = 0` を指定すると、UDP の非同期要求が必ずタイムアウトする不具合を修正。TCP・UDP の同期要求と同じく `0` は無期限として扱う。
- コンストラクタでリモートパスワードの解除に失敗した場合に、接続が閉じられず残る不具合を修正。

### Changed
- TCP のタイムアウト（`timeoutMilliseconds`）を、同期・非同期とも「送信開始から応答の受信完了まで」の要求全体に対する期限に変更（従来は1回の読み込みごと）。
- TCP でタイムアウト・通信エラーが発生すると接続を閉じ、以降の要求は `IOException` になります。再接続するにはインスタンスを作り直してください。
- **破壊的変更：** TCP の接続が拒否された場合の例外を、`AggregateException` から `SocketException` に変更。
- **破壊的変更：** 通信のタイムアウトは、すべて `TimeoutException`（InnerException は `SocketError.TimedOut` の `SocketException`）をスローするように変更。従来は TCP の要求で `IOException`、UDP の要求で `SocketException` だった。

### Removed
- **破壊的変更：** `IPlc`・`IPlcTransport`・`BasePlc` の、引数1つの `Request(byte[])` / `RequestAsync(byte[])` を削除（0.5.1 から非推奨）。`IReceiveLengthParser` を受け取るオーバーロードを使用してください。

## [0.11.0] - 2026-09-28
### Added
- 複数ブロック一括読出し・書込み（コマンド: 0406 / 1406）の `BlockRead(Action<BlockReadBuilder>)` / `BlockWrite(Action<BlockWriteBuilder>)`（および非同期版）を追加。上限（120ブロック、`ProcessorSeries.iQR` は60ブロック、合計960点）を超える場合は自動で分割。
- `McpX.UseMultiBlockAccess`（および `McpX` / `McpXSimulator` のコンストラクタ引数 `useMultiBlockAccess`）を追加。有効にすると、`Read(Action<ReadBuilder>)` / `Write(Action<WriteBuilder>)` で複数の範囲を複数ブロックアクセスで1回の交信にまとめる。CPU内蔵Ethernetポートなど非対応（エラー C059）の接続先があるため、既定は無効。ビットデバイスへの `bool` で要素数が16の倍数でない範囲は、他のビットを上書きしないよう従来どおり範囲ごとに書き込み。
- リモート操作 `RemoteRun` / `RemoteStop` / `RemotePause` / `RemoteLatchClear` / `RemoteReset`（および非同期版、コマンド: 1001 / 1002 / 1003 / 1005 / 1006）と `RemoteRunClearMode` を追加。`RemoteReset` は、リセットによる接続の切断・タイムアウトを例外にせず、リセット後に接続し直す（リモートパスワードのアンロックもやり直す）ため、同じインスタンスで続けて通信可能。
- PLCから受信したエラーコードを取得できる `McProtocolException.ErrorCode` を追加。

## [0.10.0] - 2026-09-27
### Added
- システムNo.・号機No.を指定して GX Simulator3 に接続する `McpXSimulator`（ポート `5500 + システムNo. × 10 + 号機No.`、TCP・バイナリ、既定は `ProcessorSeries.iQR`）と、`McpXSimulator.GetPort` を追加。シミュレータごとにインスタンスを生成することで、複数のシミュレータに同時に接続可能。

## [0.9.2] - 2026-09-27
### Fixed
- `ProcessorSeries.iQR` を指定した場合に、ビットデバイスのランダム書き込み（`RandomWriteBit`、および `RandomWrite` / `Write(Action<WriteBuilder>)` のビットデバイス）がエラー C061 になる不具合を修正。MELSEC iQ-R 用サブコマンド（0003）のセット/リセットを2バイト（ASCIIは4桁）で送信するように変更。

## [0.9.1] - 2026-09-25
### Fixed
- ビットデバイスをワード型で連続アクセス（例：`BatchRead<ushort>(Prefix.M, ...)`）した際、960ワードを超えて分割すると2回目以降の先頭デバイス番号がずれる不具合を修正（1ワード=16点分進めるべきところをワード数分しか進めていなかった）。
- `BatchRead` / `BatchWrite` で合計が65535ワードを超えると、データが黙って欠ける不具合を修正。
- `Read<T>` が複数ワード型で必要以上のワードを読み込む不具合を修正（`int` で4ワード、`long` で16ワード）。
- `sbyte` 系API（`ReadSByte`、`WriteSByte` など）が常に `NotSupportedException` になる不具合を修正。
- `byte` の書き込みで不正なパケットを送信する不具合を修正。`byte` / `sbyte` は1要素=1ワード（下位バイト）に統一。**破壊的変更：** `BatchRead<byte>(n)` / `BatchReadByte(n)` の戻り値は `2n` 個の生バイトではなく `n` 個になります。

## [0.9.0] - 2026-09-25
### Added
- 連続／ランダム統合のビルダーAPIを追加：`Read(Action<ReadBuilder>)` / `Write(Action<WriteBuilder>)`（および非同期版）。点数指定（読み込み）・配列指定（書き込み）のデバイスは連続アクセス、単一指定のデバイスはランダムアクセスで処理。

## [0.8.1] - 2026-09-25
### Fixed
- TCP接続がタイムアウトした際、例外が観測されないまま失敗したタスクが残り、後から `TaskScheduler.UnobservedTaskException` として通知される不具合を修正（#41）。
- UDPの非同期要求で受信タイムアウトした後、保留されたままの受信が次の要求の応答を横取りし、タイムアウトが連鎖する不具合を修正。タイムアウト時にUDPソケットを作り直すように変更。

## [0.8.0] - 2026-07-15
### Added
- ビルダー方式のランダムアクセスAPIを追加：`RandomRead` / `RandomWrite(Action<builder>)`（および非同期版）。型に応じてビット／ワード／ダブルワードを自動振り分けし、ビットデバイスにも対応。
- ビルダー方式のモニタAPIを追加：`MonitorRegist(Action<MonitorBuilder>)` が `MonitorSession` を返し、登録1回で繰り返し読み出し可能。

### Changed
- ランダム読み書き・モニタ登録の点数上限を、MELSEC iQ-Rシリーズで系列別に厳密化。

### Deprecated
- 新しいビルダーAPIへの移行に伴い、ジェネリックの `RandomRead` / `RandomWrite<T1, T2>` および型固有のランダム系オーバーロード（Compat）を非推奨化。

### Fixed
- ランダム読み書き・モニタ登録のASCII点数エンコードが16点以上で崩れる不具合を修正。
- ビット（`bool`）デバイスでのランダム読み書きの不具合（要素数が不正／パケットが不正になる）を修正。

## [0.7.0] - 2026-06-30
### Added
- `ProcessorSeries`オプションにより、MELSEC iQ-Rシリーズ（デバイス拡張指定）に対応

## [0.6.0] - 2026-02-01
### Added
- タイムアウトの時間をインスタンスの生成時に指定できるように修正

## [0.5.5] - 2025-11-17
### Fixed
- NativeAOT対応のMcpxInterropクラスにて、BatchWriteBoolのBatchWrite呼び出しがshortを使用していた不具合を修正（boolに変更）

## [0.5.4] - 2025-09-03
### Fixed
- BatchReadメソッドで32ビット以上の型を指定した場合、読み出し点数が正しく計算されない不具合を修正

## [0.5.3] - 2025-08-14
### Added
- VB互換の型固有のオーバーロードを追加 

## [0.5.2] - 2025-07-16
### Fixed
- 16進アドレス変換の不具合を修正 

## [0.5.1] - 2024-04-17
### Changed
- TCP通信処理のパフォーマンスを最適化

## [0.5.0] - 2024-04-14
### Added
- ドキュメントコメントを追加

## [0.4.2] - 2024-04-13
### Changed
- 不要な`public`修飾子を`internal`に変更

## [0.4.1] - 2024-04-12
### Fixed
- ランダムリード／モニターコマンドにおいて、ワードデバイスの指定数が2点以外の場合にエラーが発生する不具合を修正

## [0.4.0] - 2024-04-12
### Added
- 4Eフレーム（バイナリ、ASCII）に対応

### Changed
- パケット生成処理を改善
- パケット解析処理を改善

## [0.3.0] - 2024-04-07
### Added
- 文字列読み込み機能を追加
- 文字列書き込み機能を追加

## [0.2.0] - 2024-04-03
### Added
- UDPに対応

## [0.1.0] - 2024-03-29
### Added
- 3Eフレーム（ASCII）に対応
