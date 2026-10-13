# 対応プロトコル・コマンド
## プロトコル
| 項目 | 対応 |
|---|---|
| PLCシリーズ | MELSEC iQ-Rシリーズ（`ProcessorSeries.iQR`）、MELSEC-Q/Lシリーズ（`ProcessorSeries.Q`） |
| プロトコル | TCP、UDP |
| フレーム | 3Eフレーム、4Eフレーム |
| 交信データコード | バイナリコード、ASCIIコード |
| シミュレータ | GX Simulator3（<xref:McpXLib.McpXSimulator>） |

接続設定は <xref:McpXLib.McpXOptions> で指定し、<xref:McpXLib.McpX.Connect*> / <xref:McpXLib.McpX.ConnectAsync*> で接続します。他局・マルチCPUの別の号機へは <xref:McpXLib.McpXRoute> でアクセスします。

## コマンド
非同期メソッドは、いずれも省略可能な `CancellationToken` を受け取ります。

| 名称 | 説明 | 同期メソッド | 非同期メソッド |
|---|---|---|---|
| **単一読出し** | デバイスの単一値を取得します。 | <xref:McpXLib.McpX.Read*> | <xref:McpXLib.McpX.ReadAsync*> |
| **単一書込み** | デバイスに単一値を書き込みます。 | <xref:McpXLib.McpX.Write*> | <xref:McpXLib.McpX.WriteAsync*> |
| **一括読出し** | 連続したデバイスから、指定数のデータを一括で読み出します。上限を超える場合は自動で分割します。 | <xref:McpXLib.McpX.BatchRead*> | <xref:McpXLib.McpX.BatchReadAsync*> |
| **一括書込み** | 連続したデバイスに、配列で指定した値を一括で書き込みます。上限を超える場合は自動で分割します。 | <xref:McpXLib.McpX.BatchWrite*> | <xref:McpXLib.McpX.BatchWriteAsync*> |
| **ランダム読出し** | 非連続デバイス（ビット／ワード／ダブルワード）を読み出します（コマンド: 0403）。 | <xref:McpXLib.McpX.RandomRead*> | <xref:McpXLib.McpX.RandomReadAsync*> |
| **ランダム書込み** | 非連続デバイス（ビット／ワード／ダブルワード）へ書き込みます（コマンド: 1402）。 | <xref:McpXLib.McpX.RandomWrite*> | <xref:McpXLib.McpX.RandomWriteAsync*> |
| **モニタ登録** | モニタ対象デバイスを登録し、<xref:McpXLib.MonitorSession> を返します（コマンド: 0801）。 | <xref:McpXLib.McpX.MonitorRegister*> | <xref:McpXLib.McpX.MonitorRegisterAsync*> |
| **モニタ読み取り** | 登録済みデバイスの最新値を読み出します（コマンド: 0802）。 | <xref:McpXLib.MonitorSession.Read*> | <xref:McpXLib.MonitorSession.ReadAsync*> |
| **統合読み込み** | 範囲指定と単一指定のデバイスをまとめて読み込みます。 | `Read(Action<ReadBuilder>)` | `ReadAsync(Action<ReadBuilder>)` |
| **統合書き込み** | 範囲指定と単一指定のデバイスにまとめて書き込みます。 | `Write(Action<WriteBuilder>)` | `WriteAsync(Action<WriteBuilder>)` |
| **複数ブロック一括読出し** | 複数の範囲（ブロック）を1回の交信でまとめて読み出します（コマンド: 0406）。 | <xref:McpXLib.McpX.BlockRead*> | <xref:McpXLib.McpX.BlockReadAsync*> |
| **複数ブロック一括書込み** | 複数の範囲（ブロック）を1回の交信でまとめて書き込みます（コマンド: 1406）。 | <xref:McpXLib.McpX.BlockWrite*> | <xref:McpXLib.McpX.BlockWriteAsync*> |
| **文字列の読み書き** | Shift_JIS の文字列を読み書きします。 | <xref:McpXLib.McpX.ReadString*> / <xref:McpXLib.McpX.WriteString*> | <xref:McpXLib.McpX.ReadStringAsync*> / <xref:McpXLib.McpX.WriteStringAsync*> |
| **リモートRUN / STOP / PAUSE** | CPUユニットの動作状態を変更します（コマンド: 1001 / 1002 / 1003）。 | <xref:McpXLib.McpX.RemoteRun*> / <xref:McpXLib.McpX.RemoteStop*> / <xref:McpXLib.McpX.RemotePause*> | <xref:McpXLib.McpX.RemoteRunAsync*> / <xref:McpXLib.McpX.RemoteStopAsync*> / <xref:McpXLib.McpX.RemotePauseAsync*> |
| **リモートラッチクリア / RESET** | ラッチクリア、リセットを実行します（コマンド: 1005 / 1006）。 | <xref:McpXLib.McpX.RemoteLatchClear*> / <xref:McpXLib.McpX.RemoteReset*> | <xref:McpXLib.McpX.RemoteLatchClearAsync*> / <xref:McpXLib.McpX.RemoteResetAsync*> |
| **リモートパスワード 解除/ロック** | `McpXOptions.Password` を指定すると、接続時に解除し、破棄時にロックします（コマンド: 1630 / 1631）。 | <xref:McpXLib.McpX.Connect*> / <xref:McpXLib.McpX.Dispose*> | <xref:McpXLib.McpX.ConnectAsync*> / <xref:McpXLib.McpX.DisposeAsync*> |
