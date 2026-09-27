using McpXLib;
using McpXLib.Enums;

namespace TestMcpX;

/// <summary>
/// GX Simulator3（R120CPU・TCP・バイナリ）を使った結合テスト。
/// </summary>
/// <remarks>
/// シミュレータの D0〜D4999・M0〜M12287 を上書きします（R120CPU の M は既定 12288点）。<br/>
/// GX Simulator3 は 127.0.0.1 でしか待ち受けないため、別PCから接続する場合は
/// シミュレータ側PCで <c>scripts/gxsim-portforward.ps1</c> を実行してポートを公開してください。<br/>
/// 通常の <c>dotnet test</c> では実行されず（Inconclusive）、環境変数 <c>MCPX_SIM_PLC=1</c> を指定した場合のみ実行します。
/// <code>
/// MCPX_SIM_PLC=1 dotnet test TestMcpX --filter TestCategory=SimulatorPlc
/// </code>
/// 接続先は <c>MCPX_SIM_IP</c>（既定 192.168.12.90）/ <c>MCPX_SIM_PORT</c>（既定 5511）で変更できます。
/// </remarks>
public abstract class SimulatorPlcTestBase : PlcIntegrationTestBase
{
    protected abstract RequestFrame RequestFrame { get; }

    protected abstract ProcessorSeries ProcessorSeries { get; }

    protected override McpX Connect() =>
        ConnectIfEnabled("MCPX_SIM_PLC", "MCPX_SIM_IP", "192.168.12.90", "MCPX_SIM_PORT", 5511, RequestFrame, ProcessorSeries);

    protected override int BitDeviceAsWordsPoints => 12288;
}

/// <summary>
/// GX Simulator3 結合テスト（3Eフレーム・Q/Lシリーズ互換のデバイス指定）。
/// </summary>
[TestClass]
[TestCategory("SimulatorPlc")]
public sealed class TestSimulatorPlc : SimulatorPlcTestBase
{
    protected override RequestFrame RequestFrame => RequestFrame.E3;

    protected override ProcessorSeries ProcessorSeries => ProcessorSeries.Q;
}

/// <summary>
/// GX Simulator3 結合テスト（4Eフレーム・iQ-Rシリーズの拡張デバイス指定）。
/// </summary>
[TestClass]
[TestCategory("SimulatorPlc")]
public sealed class TestSimulatorPlcE4iQR : SimulatorPlcTestBase
{
    protected override RequestFrame RequestFrame => RequestFrame.E4;

    protected override ProcessorSeries ProcessorSeries => ProcessorSeries.iQR;
}
