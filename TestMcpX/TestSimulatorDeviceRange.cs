using McpXLib;
using McpXLib.Enums;
using McpXLib.Exceptions;

namespace TestMcpX;

/// <summary>
/// R120CPU（GX Simulator3・iQ-R の実機）の全デバイス範囲を網羅する結合テストの共通実装。
/// </summary>
/// <remarks>
/// 対象デバイスの全点を上書きし、テスト後は0で埋め戻します。<br/>
/// デバイス点数は <see cref="DevicePoints"/>（既定は GX Simulator3 の R120CPU：M 12K / B 8K / SB 2K / F 2K / V 2K / T 1K / C 512 / D 18K / W 64K / SW 2K / L 8K）で決まります。<br/>
/// 点数0の S・ST、およびLT・LC（ライブラリの <see cref="Prefix"/> に未定義）は対象外です。<br/>
/// </remarks>
public abstract class DeviceRangeTestBase
{
    private static readonly Random random = new();

    protected abstract RequestFrame RequestFrame { get; }

    protected abstract ProcessorSeries ProcessorSeries { get; }

    // BatchRead/BatchWrite の点数は ushort のため、W（65536点）などは分けて要求する
    private const int ChunkSize = 0x8000;

    /// <summary>
    /// 接続先PLCへ接続する。実行条件を満たさない場合は <see cref="Assert.Inconclusive(string)"/> を呼ぶ。
    /// </summary>
    protected abstract McpX Connect();

    /// <summary>
    /// デバイスごとの点数（TS・TC は TN、CS・CC は CN と同じ点数）。
    /// </summary>
    protected virtual IReadOnlyDictionary<Prefix, int> DevicePoints { get; } = new Dictionary<Prefix, int>
    {
        [Prefix.D] = 18432, [Prefix.W] = 0x10000, [Prefix.SW] = 0x800, [Prefix.TN] = 1024, [Prefix.CN] = 512,
        [Prefix.M] = 12288, [Prefix.B] = 0x2000, [Prefix.SB] = 0x800, [Prefix.F] = 2048, [Prefix.V] = 2048, [Prefix.L] = 8192,
    };

    private int Points(Prefix prefix) => DevicePoints[prefix switch
    {
        Prefix.TS or Prefix.TC => Prefix.TN,
        Prefix.CS or Prefix.CC => Prefix.CN,
        _ => prefix,
    }];

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
    [DataRow(Prefix.D)]
    [DataRow(Prefix.W)]
    [DataRow(Prefix.SW)]
    [DataRow(Prefix.TN)]
    [DataRow(Prefix.CN)]
    public void TestWordDeviceFullRange(Prefix prefix)
    {
        using var mcpx = Connect();
        var points = Points(prefix);

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
    [DataRow(Prefix.M)]
    [DataRow(Prefix.B)]
    [DataRow(Prefix.SB)]
    [DataRow(Prefix.F)]
    [DataRow(Prefix.V)]
    [DataRow(Prefix.L)]
    [DataRow(Prefix.TS)]
    [DataRow(Prefix.TC)]
    [DataRow(Prefix.CS)]
    [DataRow(Prefix.CC)]
    public void TestBitDeviceFullRange(Prefix prefix)
    {
        using var mcpx = Connect();
        var points = Points(prefix);

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
    [DataRow(Prefix.D, false)]
    [DataRow(Prefix.W, false)]
    [DataRow(Prefix.SW, false)]
    [DataRow(Prefix.TN, false)]
    [DataRow(Prefix.CN, false)]
    [DataRow(Prefix.M, true)]
    [DataRow(Prefix.B, true)]
    [DataRow(Prefix.SB, true)]
    [DataRow(Prefix.F, true)]
    [DataRow(Prefix.V, true)]
    [DataRow(Prefix.L, true)]
    [DataRow(Prefix.TS, true)]
    [DataRow(Prefix.CS, true)]
    public void TestOutOfRange(Prefix prefix, bool isBit)
    {
        using var mcpx = Connect();
        var points = Points(prefix);

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
/// GX Simulator3（R120CPU）の全デバイス範囲テスト。
/// </summary>
/// <remarks>
/// 実行条件・接続先は <see cref="SimulatorConnection"/> を参照してください。
/// <code>
/// MCPX_SIM_PLC=1 dotnet test TestMcpX --filter TestCategory=SimulatorPlc
/// </code>
/// </remarks>
public abstract class SimulatorDeviceRangeTestBase : DeviceRangeTestBase
{
    protected override McpX Connect() => SimulatorConnection.Connect(RequestFrame, ProcessorSeries);
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
