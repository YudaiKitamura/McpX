using McpXLib;
using McpXLib.Enums;

namespace TestMcpX;

/// <summary>
/// 複数の GX Simulator3 に同時接続する結合テスト。
/// </summary>
/// <remarks>
/// 1台目（<c>MCPX_SIM_SYSTEM_NO</c> / <c>MCPX_SIM_CPU_NO</c>、既定 1 / 1 = ポート5511）と
/// 2台目（<c>MCPX_SIM2_SYSTEM_NO</c> / <c>MCPX_SIM2_CPU_NO</c>、既定 2 / 1 = ポート5521）の D5000・M12000 を使用し、テスト後は元の値に戻します。<br/>
/// 実行条件・接続先IPは <see cref="SimulatorConnection"/> を参照してください。
/// </remarks>
[TestClass]
[TestCategory("SimulatorPlc")]
public sealed class TestSimulatorMultiSystem
{
    private const string WordAddress = "5000";
    private const string BitAddress = "12000";

    [TestMethod]
    public async Task TestIndependentSystems()
    {
        using var sim1 = SimulatorConnection.Connect();
        using var sim2 = SimulatorConnection.Connect(systemNoVar: "MCPX_SIM2_SYSTEM_NO", defaultSystemNo: 2, cpuNoVar: "MCPX_SIM2_CPU_NO", defaultCpuNo: 1);

        Assert.AreNotEqual(
            McpXSimulator.GetPort(sim1.SystemNo, sim1.CpuNo),
            McpXSimulator.GetPort(sim2.SystemNo, sim2.CpuNo),
            "1台目と2台目に同じシミュレータが指定されています。");

        var original = (Word1: sim1.Read<short>(Prefix.D, WordAddress), Bit1: sim1.Read<bool>(Prefix.M, BitAddress),
                        Word2: sim2.Read<short>(Prefix.D, WordAddress), Bit2: sim2.Read<bool>(Prefix.M, BitAddress));
        try
        {
            // 2台に異なる値を同時に書き込み、それぞれの値が他方に影響しないこと
            await Task.WhenAll(
                sim1.WriteAsync(Prefix.D, WordAddress, (short)1111),
                sim2.WriteAsync(Prefix.D, WordAddress, (short)2222));
            sim1.Write(Prefix.M, BitAddress, true);
            sim2.Write(Prefix.M, BitAddress, false);

            Assert.AreEqual((short)1111, sim1.Read<short>(Prefix.D, WordAddress));
            Assert.AreEqual((short)2222, sim2.Read<short>(Prefix.D, WordAddress));
            Assert.IsTrue(sim1.Read<bool>(Prefix.M, BitAddress));
            Assert.IsFalse(sim2.Read<bool>(Prefix.M, BitAddress));
        }
        finally
        {
            sim1.Write(Prefix.D, WordAddress, original.Word1);
            sim1.Write(Prefix.M, BitAddress, original.Bit1);
            sim2.Write(Prefix.D, WordAddress, original.Word2);
            sim2.Write(Prefix.M, BitAddress, original.Bit2);
        }
    }
}
