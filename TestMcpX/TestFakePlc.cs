using McpXLib;

namespace TestMcpX;

/// <summary>
/// ダミー PLC（<see cref="FakePlcCore"/>、書き込んだ値を保持する）を使った結合テスト。
/// </summary>
/// <remarks>
/// 実機・GX Simulator3 の結合テスト（<see cref="MultiBlockPlcTestBase"/>）と同じテストを、PLC なしで（CI でも）実行します。
/// ダミー PLC は 3E フレーム・バイナリ・Q/L 形式のデバイス指定に応答します。
/// </remarks>
[TestClass]
[TestCategory("FakePlc")]
public sealed class TestFakePlc : MultiBlockPlcTestBase
{
    protected override McpX Connect() => new(new FakePlcTransport(stateful: true));
}
