using McpXLib;
using McpXLib.Enums;

namespace TestMcpX;

/// <summary>
/// GX Simulator3 結合テストの接続先。
/// </summary>
/// <remarks>
/// 通常の <c>dotnet test</c> では実行されず（Inconclusive）、環境変数 <c>MCPX_SIM_PLC=1</c> を指定した場合のみ実行します。
/// <code>
/// MCPX_SIM_PLC=1 dotnet test TestMcpX --filter TestCategory=SimulatorPlc
/// </code>
/// 接続先は <c>MCPX_SIM_IP</c>（既定 192.168.12.90）、<c>MCPX_SIM_SYSTEM_NO</c> / <c>MCPX_SIM_CPU_NO</c>（既定 1 / 1 = ポート5511）で変更できます。<br/>
/// GX Simulator3 は 127.0.0.1 でしか待ち受けないため、別PCから接続する場合は、シミュレータ側PCで
/// <c>netsh interface portproxy</c> により各ポート（5500 + システムNo. × 10 + 号機No.）を 127.0.0.1 へ転送してください。
/// </remarks>
internal static class SimulatorConnection
{
    internal const string EnableVar = "MCPX_SIM_PLC";

    internal static string Ip => Environment.GetEnvironmentVariable("MCPX_SIM_IP") ?? "192.168.12.90";

    internal static McpXSimulator Connect(
        RequestFrame requestFrame = RequestFrame.E3,
        ProcessorSeries processorSeries = ProcessorSeries.iQR,
        string systemNoVar = "MCPX_SIM_SYSTEM_NO", int defaultSystemNo = 1,
        string cpuNoVar = "MCPX_SIM_CPU_NO", int defaultCpuNo = 1)
    {
        PlcIntegrationTestBase.EnsureEnabled(EnableVar);

        return new McpXSimulator(
            systemNo: PlcIntegrationTestBase.GetEnvironmentInt(systemNoVar, defaultSystemNo),
            cpuNo: PlcIntegrationTestBase.GetEnvironmentInt(cpuNoVar, defaultCpuNo),
            ip: Ip,
            requestFrame: requestFrame,
            processorSeries: processorSeries);
    }
}

/// <summary>
/// GX Simulator3（R120CPU・TCP・バイナリ）を使った結合テスト。
/// </summary>
/// <remarks>
/// シミュレータの D0〜D4999・M0〜M12287 を上書きします（R120CPU の M は既定 12288点）。
/// <c>MCPX_SIM_SYSTEM_NO</c> などで接続先を変えた場合は、そのシミュレータのデバイスを上書きします。<br/>
/// 実行条件・接続先は <see cref="SimulatorConnection"/> を参照してください。
/// </remarks>
public abstract class SimulatorPlcTestBase : PlcIntegrationTestBase
{
    protected abstract RequestFrame RequestFrame { get; }

    protected abstract ProcessorSeries ProcessorSeries { get; }

    protected override McpX Connect() => SimulatorConnection.Connect(RequestFrame, ProcessorSeries);

    protected override int BitDeviceAsWordsPoints => 12288;

    // ---------------------------------------------------------------
    // 複数ブロック一括読出し・書込み（D7000〜D8999 / W200〜W20F / M11000〜M11031）
    // 実機（CPU内蔵Ethernetなど）は非対応の場合があるため、シミュレータでのみ実行する。
    // ---------------------------------------------------------------

    [TestMethod]
    public async Task TestBlockWriteRead()
    {
        using var mcpx = Connect();
        var random = new Random();

        var d = Enumerable.Range(0, 1500).Select(_ => (short)random.Next(short.MinValue, short.MaxValue + 1)).ToArray();  // 分割される
        var w = Enumerable.Range(0, 8).Select(_ => random.Next()).ToArray();
        var m = Enumerable.Range(0, 32).Select(_ => random.Next(2) == 1).ToArray();

        mcpx.BlockWrite(b => b
            .Add(Prefix.D, "7000", d)
            .Add(Prefix.W, "200", w)
            .Add(Prefix.M, "11000", m));

        short[] rd = []; int[] rw = []; bool[] rm = []; bool[] rm5 = [];
        await mcpx.BlockReadAsync(b => b
            .Add<short>(Prefix.D, "7000", 1500, v => rd = v)
            .Add<int>(Prefix.W, "200", 8, v => rw = v)
            .Add<bool>(Prefix.M, "11000", 32, v => rm = v)
            .Add<bool>(Prefix.M, "11003", 5, v => rm5 = v));

        CollectionAssert.AreEqual(d, rd);
        CollectionAssert.AreEqual(w, rw);
        CollectionAssert.AreEqual(m, rm);
        CollectionAssert.AreEqual(m.Skip(3).Take(5).ToArray(), rm5);

        // 従来の一括読出しでも同じ値であること
        CollectionAssert.AreEqual(d, mcpx.BatchRead<short>(Prefix.D, "7000", 1500));
        CollectionAssert.AreEqual(m, mcpx.BatchRead<bool>(Prefix.M, "11000", 32));
    }

    [TestMethod]
    public void TestIntegratedMultiBlockModes()
    {
        using var mcpx = Connect();

        // Always（複数ブロック）と Never（範囲ごと）で、同じ値を読み書きできること
        foreach (var mode in new[] { MultiBlockAccessMode.Always, MultiBlockAccessMode.Never })
        {
            mcpx.MultiBlockAccess = mode;
            short value = (short)mode;

            mcpx.Write(b => b
                .Add(Prefix.D, "7000", new[] { value, (short)(value + 1) })
                .Add(Prefix.D, "8000", new[] { (short)(value + 2) })
                .Add(Prefix.M, "11000", Enumerable.Repeat(mode == MultiBlockAccessMode.Always, 10).ToArray()));

            short[] a = []; short[] c = []; bool[] bits = [];
            mcpx.Read(b => b
                .Add<short>(Prefix.D, "7000", 2, v => a = v)
                .Add<short>(Prefix.D, "8000", 1, v => c = v)
                .Add<bool>(Prefix.M, "11000", 10, v => bits = v));

            CollectionAssert.AreEqual(new[] { value, (short)(value + 1) }, a, mode.ToString());
            CollectionAssert.AreEqual(new[] { (short)(value + 2) }, c, mode.ToString());
            Assert.IsTrue(bits.All(x => x == (mode == MultiBlockAccessMode.Always)), mode.ToString());
        }
    }
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
