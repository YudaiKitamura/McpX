using McpXLib;
using McpXLib.Enums;
using McpXLib.Exceptions;

namespace TestMcpX;

/// <summary>
/// GX Simulator3（R120CPU）の全デバイス範囲を網羅する結合テスト。
/// </summary>
/// <remarks>
/// 対象デバイスの全点を上書きし、テスト後は0で埋め戻します。<br/>
/// デバイス点数は R120CPU の既定値（M 12K / B 8K / SB 2K / F 2K / V 2K / T 1K / C 512 / D 18K / W 64K / SW 2K / L 8K）を前提とします。<br/>
/// 点数0の S・ST、およびLT・LC（ライブラリの <see cref="Prefix"/> に未定義）は対象外です。<br/>
/// 実行条件・接続先は <see cref="TestSimulatorPlc"/> と同じです。
/// <code>
/// MCPX_SIM_PLC=1 dotnet test TestMcpX --filter TestCategory=SimulatorPlc
/// </code>
/// </remarks>
public abstract class SimulatorDeviceRangeTestBase
{
    private static readonly Random random = new();

    protected abstract RequestFrame RequestFrame { get; }

    protected abstract ProcessorSeries ProcessorSeries { get; }

    // BatchRead/BatchWrite の点数は ushort のため、W（65536点）などは分けて要求する
    private const int ChunkSize = 0x8000;

    private McpX Connect() =>
        PlcIntegrationTestBase.ConnectIfEnabled("MCPX_SIM_PLC", "MCPX_SIM_IP", "192.168.12.90", "MCPX_SIM_PORT", 5511, RequestFrame, ProcessorSeries);

    private static bool IsHex(Prefix prefix) =>
        prefix is Prefix.B or Prefix.W or Prefix.SB or Prefix.SW;

    private static string Address(Prefix prefix, int address) =>
        IsHex(prefix) ? address.ToString("X") : address.ToString();

    private static void WriteAll<T>(McpX mcpx, Prefix prefix, T[] values) where T : unmanaged
    {
        for (int start = 0; start < values.Length; start += ChunkSize)
        {
            mcpx.BatchWrite(prefix, Address(prefix, start), values.Skip(start).Take(ChunkSize).ToArray());
        }
    }

    private static T[] ReadAll<T>(McpX mcpx, Prefix prefix, int points) where T : unmanaged
    {
        var result = new List<T>(points);
        for (int start = 0; start < points; start += ChunkSize)
        {
            result.AddRange(mcpx.BatchRead<T>(prefix, Address(prefix, start), (ushort)Math.Min(ChunkSize, points - start)));
        }
        return result.ToArray();
    }

    private static void AssertOutOfRange(Action action, string because)
    {
        var ex = Assert.ThrowsException<McProtocolException>(action, because);
        StringAssert.Contains(ex.Message, "(4031)", because);
    }

    // ---------------------------------------------------------------
    // ワードデバイス
    // ---------------------------------------------------------------

    [TestMethod]
    [DataRow(Prefix.D, 18432, DisplayName = "D0〜D18431")]
    [DataRow(Prefix.W, 0x10000, DisplayName = "W0〜WFFFF")]
    [DataRow(Prefix.SW, 0x800, DisplayName = "SW0〜SW7FF")]
    [DataRow(Prefix.TN, 1024, DisplayName = "TN0〜TN1023")]
    [DataRow(Prefix.CN, 512, DisplayName = "CN0〜CN511")]
    public void TestWordDeviceFullRange(Prefix prefix, int points)
    {
        using var mcpx = Connect();

        var values = Enumerable.Range(0, points).Select(_ => (short)random.Next(short.MinValue, short.MaxValue + 1)).ToArray();
        try
        {
            WriteAll(mcpx, prefix, values);
            CollectionAssert.AreEqual(values, ReadAll<short>(mcpx, prefix, points));

            // 先頭・末尾の単一アクセス
            Assert.AreEqual(values[0], mcpx.Read<short>(prefix, Address(prefix, 0)));
            Assert.AreEqual(values[^1], mcpx.Read<short>(prefix, Address(prefix, points - 1)));
        }
        finally
        {
            WriteAll(mcpx, prefix, new short[points]);
        }
    }

    // ---------------------------------------------------------------
    // ビットデバイス
    // ---------------------------------------------------------------

    [TestMethod]
    [DataRow(Prefix.M, 12288, DisplayName = "M0〜M12287")]
    [DataRow(Prefix.B, 0x2000, DisplayName = "B0〜B1FFF")]
    [DataRow(Prefix.SB, 0x800, DisplayName = "SB0〜SB7FF")]
    [DataRow(Prefix.F, 2048, DisplayName = "F0〜F2047")]
    [DataRow(Prefix.V, 2048, DisplayName = "V0〜V2047")]
    [DataRow(Prefix.L, 8192, DisplayName = "L0〜L8191")]
    [DataRow(Prefix.TS, 1024, DisplayName = "TS0〜TS1023")]
    [DataRow(Prefix.TC, 1024, DisplayName = "TC0〜TC1023")]
    [DataRow(Prefix.CS, 512, DisplayName = "CS0〜CS511")]
    [DataRow(Prefix.CC, 512, DisplayName = "CC0〜CC511")]
    public void TestBitDeviceFullRange(Prefix prefix, int points)
    {
        using var mcpx = Connect();

        var values = Enumerable.Range(0, points).Select(_ => random.Next(2) == 1).ToArray();
        try
        {
            WriteAll(mcpx, prefix, values);
            CollectionAssert.AreEqual(values, ReadAll<bool>(mcpx, prefix, points));

            // 先頭・末尾の単一アクセス
            Assert.AreEqual(values[0], mcpx.Read<bool>(prefix, Address(prefix, 0)));
            Assert.AreEqual(values[^1], mcpx.Read<bool>(prefix, Address(prefix, points - 1)));

            // ワード単位でも全範囲を読み、ビット単位の結果と一致すること
            if (points % 16 == 0)
            {
                var words = ReadAll<ushort>(mcpx, prefix, points / 16);
                for (int n = 0; n < points; n++)
                {
                    Assert.AreEqual(values[n], (words[n / 16] >> (n % 16) & 1) == 1, $"{prefix}{Address(prefix, n)}");
                }
            }
        }
        finally
        {
            WriteAll(mcpx, prefix, new bool[points]);
        }
    }

    // ---------------------------------------------------------------
    // 範囲外
    // ---------------------------------------------------------------

    [TestMethod]
    [DataRow(Prefix.D, 18432, false, DisplayName = "D18432")]
    [DataRow(Prefix.W, 0x10000, false, DisplayName = "W10000")]
    [DataRow(Prefix.SW, 0x800, false, DisplayName = "SW800")]
    [DataRow(Prefix.TN, 1024, false, DisplayName = "TN1024")]
    [DataRow(Prefix.CN, 512, false, DisplayName = "CN512")]
    [DataRow(Prefix.M, 12288, true, DisplayName = "M12288")]
    [DataRow(Prefix.B, 0x2000, true, DisplayName = "B2000")]
    [DataRow(Prefix.SB, 0x800, true, DisplayName = "SB800")]
    [DataRow(Prefix.F, 2048, true, DisplayName = "F2048")]
    [DataRow(Prefix.V, 2048, true, DisplayName = "V2048")]
    [DataRow(Prefix.L, 8192, true, DisplayName = "L8192")]
    [DataRow(Prefix.TS, 1024, true, DisplayName = "TS1024")]
    [DataRow(Prefix.CS, 512, true, DisplayName = "CS512")]
    public void TestOutOfRange(Prefix prefix, int points, bool isBit)
    {
        using var mcpx = Connect();

        var end = Address(prefix, points);
        var last = Address(prefix, points - 1);

        if (isBit)
        {
            AssertOutOfRange(() => mcpx.Read<bool>(prefix, end), $"{prefix}{end} の読み込み");
            AssertOutOfRange(() => mcpx.Write(prefix, end, true), $"{prefix}{end} の書き込み");
            AssertOutOfRange(() => mcpx.BatchRead<bool>(prefix, last, 2), $"{prefix}{last} から2点（末尾をまたぐ）");
        }
        else
        {
            AssertOutOfRange(() => mcpx.Read<short>(prefix, end), $"{prefix}{end} の読み込み");
            AssertOutOfRange(() => mcpx.Write(prefix, end, (short)1), $"{prefix}{end} の書き込み");
            AssertOutOfRange(() => mcpx.BatchRead<short>(prefix, last, 2), $"{prefix}{last} から2点（末尾をまたぐ）");
        }
    }
}

/// <summary>
/// 全デバイス範囲テスト（3Eフレーム・Q/Lシリーズ互換のデバイス指定）。
/// </summary>
[TestClass]
[TestCategory("SimulatorPlc")]
public sealed class TestSimulatorDeviceRange : SimulatorDeviceRangeTestBase
{
    protected override RequestFrame RequestFrame => RequestFrame.E3;

    protected override ProcessorSeries ProcessorSeries => ProcessorSeries.Q;
}

/// <summary>
/// 全デバイス範囲テスト（4Eフレーム・iQ-Rシリーズの拡張デバイス指定）。
/// </summary>
[TestClass]
[TestCategory("SimulatorPlc")]
public sealed class TestSimulatorDeviceRangeE4iQR : SimulatorDeviceRangeTestBase
{
    protected override RequestFrame RequestFrame => RequestFrame.E4;

    protected override ProcessorSeries ProcessorSeries => ProcessorSeries.iQR;
}
