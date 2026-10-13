# Contributing

[日本語は下にあります](#コントリビューション)

Bug reports and pull requests are welcome. For a large change, please open an issue first to discuss it.

## Build and Test

The .NET SDK version is pinned in [global.json](global.json) (.NET 10 SDK).

```sh
dotnet build McpX.sln
dotnet test TestMcpX            # unit tests and integration tests against a fake PLC
dotnet test TestMcpXInterop     # tests of the native library (C ABI)
```

Integration tests against a real PLC or GX Simulator3 run only when enabled by environment variables (they overwrite devices on the PLC):

| Target | Command |
|---|---|
| MELSEC iQ-R | `MCPX_REAL_IQR=1 MCPX_IQR_IP=<ip> dotnet test TestMcpX --filter TestCategory=RealPlcIQR` |
| MELSEC-Q/L | `MCPX_REAL_PLC=1 MCPX_PLC_IP=<ip> dotnet test TestMcpX --filter TestCategory=RealPlc` |
| GX Simulator3 | `MCPX_SIM_PLC=1 MCPX_SIM_IP=<ip> dotnet test TestMcpX --filter TestCategory=SimulatorPlc` |

## Pull Requests

- Add tests for the change.
- Add the change to the `[Unreleased]` section of both [CHANGELOG.md](CHANGELOG.md) and [CHANGELOG_JA.md](CHANGELOG_JA.md).
- Do not break the public API outside a major version (see [Versioning](README.md#versioning)).

---

# コントリビューション

不具合の報告やプルリクエストを歓迎します。大きな変更の場合は、先に Issue で相談してください。

## ビルドとテスト

.NET SDK のバージョンは [global.json](global.json) で固定しています（.NET 10 SDK）。

```sh
dotnet build McpX.sln
dotnet test TestMcpX            # 単体テストと、ダミー PLC を使った結合テスト
dotnet test TestMcpXInterop     # ネイティブライブラリ（C ABI）のテスト
```

実機・GX Simulator3 を使った結合テストは、環境変数を指定した場合のみ実行します（PLC のデバイスを上書きします）。

| 対象 | コマンド |
|---|---|
| MELSEC iQ-R | `MCPX_REAL_IQR=1 MCPX_IQR_IP=<ip> dotnet test TestMcpX --filter TestCategory=RealPlcIQR` |
| MELSEC-Q/L | `MCPX_REAL_PLC=1 MCPX_PLC_IP=<ip> dotnet test TestMcpX --filter TestCategory=RealPlc` |
| GX Simulator3 | `MCPX_SIM_PLC=1 MCPX_SIM_IP=<ip> dotnet test TestMcpX --filter TestCategory=SimulatorPlc` |

## プルリクエスト

- 変更に対するテストを追加してください。
- [CHANGELOG.md](CHANGELOG.md) と [CHANGELOG_JA.md](CHANGELOG_JA.md) の両方の `[Unreleased]` に、変更内容を追記してください。
- メジャーバージョン以外で、公開 API の互換性を壊さないでください（[バージョニング](README_JA.md#バージョニング)を参照）。
