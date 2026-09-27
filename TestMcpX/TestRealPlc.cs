namespace TestMcpX;

/// <summary>
/// 実機PLC（Q/Lシリーズ・TCP・3Eフレーム・バイナリ）を使った結合テスト。
/// </summary>
/// <remarks>
/// 実機の D0〜D4999・M0〜M15999 を上書きします。<br/>
/// 通常の <c>dotnet test</c> では実行されず（Inconclusive）、環境変数 <c>MCPX_REAL_PLC=1</c> を指定した場合のみ実行します。
/// <code>
/// MCPX_REAL_PLC=1 dotnet test TestMcpX --filter TestCategory=RealPlc
/// </code>
/// 接続先は <c>MCPX_PLC_IP</c>（既定 192.168.12.88）/ <c>MCPX_PLC_PORT</c>（既定 10000）で変更できます。
/// </remarks>
[TestClass]
[TestCategory("RealPlc")]
public sealed class TestRealPlc : PlcIntegrationTestBase
{
    protected override McpXLib.McpX Connect() =>
        ConnectIfEnabled("MCPX_REAL_PLC", "MCPX_PLC_IP", "192.168.12.88", "MCPX_PLC_PORT", 10000);
}
