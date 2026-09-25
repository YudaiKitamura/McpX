using McpXLib;
using McpXLib.Enums;
using McpXLib.Exceptions;

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
public sealed class TestRealPlc
{
    private static readonly Random random = new();

    private static McpX Connect()
    {
        if (Environment.GetEnvironmentVariable("MCPX_REAL_PLC") != "1")
        {
            Assert.Inconclusive("実機テストは MCPX_REAL_PLC=1 のときのみ実行します。");
        }

        var ip = Environment.GetEnvironmentVariable("MCPX_PLC_IP") ?? "192.168.12.88";
        var port = int.TryParse(Environment.GetEnvironmentVariable("MCPX_PLC_PORT"), out var p) ? p : 10000;
        return new McpX(ip, port);
    }

    private static T[] RandomValues<T>(int length, Func<T> next) => Enumerable.Range(0, length).Select(_ => next()).ToArray();

    private static short NextShort() => (short)random.Next(short.MinValue, short.MaxValue + 1);

    // ---------------------------------------------------------------
    // 単一デバイス（D0〜D99 / M0〜M9）
    // ---------------------------------------------------------------

    [TestMethod]
    public void TestSingleWriteRead()
    {
        using var mcpx = Connect();

        short s = NextShort();
        int i = random.Next(int.MinValue, int.MaxValue);
        float f = (float)(random.NextDouble() * 1000 - 500);
        long l = random.NextInt64(long.MinValue, long.MaxValue);
        double d = random.NextDouble() * 1e6 - 5e5;
        byte b = (byte)random.Next(0, 256);
        sbyte sb = (sbyte)random.Next(sbyte.MinValue, sbyte.MaxValue + 1);
        bool bit = random.Next(2) == 1;

        mcpx.Write(Prefix.D, "0", s);
        mcpx.Write(Prefix.D, "10", i);
        mcpx.Write(Prefix.D, "20", f);
        mcpx.Write(Prefix.D, "30", l);
        mcpx.Write(Prefix.D, "40", d);
        mcpx.Write(Prefix.D, "50", b);
        mcpx.Write(Prefix.D, "60", sb);
        mcpx.Write(Prefix.M, "0", bit);

        Assert.AreEqual(s, mcpx.Read<short>(Prefix.D, "0"));
        Assert.AreEqual(i, mcpx.Read<int>(Prefix.D, "10"));
        Assert.AreEqual(f, mcpx.Read<float>(Prefix.D, "20"));
        Assert.AreEqual(l, mcpx.Read<long>(Prefix.D, "30"));
        Assert.AreEqual(d, mcpx.Read<double>(Prefix.D, "40"));
        Assert.AreEqual(b, mcpx.Read<byte>(Prefix.D, "50"));
        Assert.AreEqual(sb, mcpx.Read<sbyte>(Prefix.D, "60"));
        Assert.AreEqual(bit, mcpx.Read<bool>(Prefix.M, "0"));

        // byte/sbyte は1ワードの下位バイト。上位バイトは0（sbyteは符号拡張）で書き込まれること。
        Assert.AreEqual((short)b, mcpx.Read<short>(Prefix.D, "50"));
        Assert.AreEqual((short)sb, mcpx.Read<short>(Prefix.D, "60"));
    }

    [TestMethod]
    public async Task TestSingleWriteReadAsync()
    {
        using var mcpx = Connect();

        int i = random.Next(int.MinValue, int.MaxValue);
        bool bit = random.Next(2) == 1;

        await mcpx.WriteAsync(Prefix.D, "70", i);
        await mcpx.WriteAsync(Prefix.M, "1", bit);

        Assert.AreEqual(i, await mcpx.ReadAsync<int>(Prefix.D, "70"));
        Assert.AreEqual(bit, await mcpx.ReadAsync<bool>(Prefix.M, "1"));
    }

    // ---------------------------------------------------------------
    // 連続デバイス（分割あり）
    // ---------------------------------------------------------------

    [TestMethod]
    public void TestBatchWordSplit()
    {
        using var mcpx = Connect();

        // 2000ワード → 960 + 960 + 80 に分割される
        var values = RandomValues(2000, NextShort);
        mcpx.BatchWrite(Prefix.D, "1000", values);

        CollectionAssert.AreEqual(values, mcpx.BatchRead<short>(Prefix.D, "1000", 2000));

        // 分割境界（D1960）をまたぐ単一読み込みでも一致すること
        Assert.AreEqual(values[959], mcpx.Read<short>(Prefix.D, "1959"));
        Assert.AreEqual(values[960], mcpx.Read<short>(Prefix.D, "1960"));
    }

    [TestMethod]
    public async Task TestBatchDoubleWordSplitAsync()
    {
        using var mcpx = Connect();

        // int 1000要素 = 2000ワード。要素が分割境界で分断されないこと。
        var values = RandomValues(1000, () => random.Next(int.MinValue, int.MaxValue));
        await mcpx.BatchWriteAsync(Prefix.D, "3000", values);

        CollectionAssert.AreEqual(values, await mcpx.BatchReadAsync<int>(Prefix.D, "3000", 1000));
    }

    [TestMethod]
    public void TestBatchBitSplit()
    {
        using var mcpx = Connect();

        // 7200点 → 7168 + 32 に分割される
        var values = RandomValues(7200, () => random.Next(2) == 1);
        mcpx.BatchWrite(Prefix.M, "0", values);

        CollectionAssert.AreEqual(values, mcpx.BatchRead<bool>(Prefix.M, "0", 7200));
    }

    [TestMethod]
    public void TestBatchBitDeviceAsWordsSplit()
    {
        using var mcpx = Connect();

        // M をワード単位で1000ワード（=16000点）書き込み、ビット単位で読み戻して一致を確認する。
        // 960ワードで分割されるため、2回目の要求は M15360 から始まる必要がある（修正 A1 の実機確認）。
        var words = RandomValues(1000, () => (ushort)random.Next(0, 65536));
        try
        {
            mcpx.BatchWrite(Prefix.M, "0", words);
        }
        catch (McProtocolException ex)
        {
            Assert.Inconclusive($"M0〜M15999 にアクセスできませんでした。PLCパラメータの M の点数を確認してください。({ex.Message})");
        }

        CollectionAssert.AreEqual(words, mcpx.BatchRead<ushort>(Prefix.M, "0", 1000));

        var bits = mcpx.BatchRead<bool>(Prefix.M, "0", 16000);
        for (int w = 0; w < words.Length; w++)
        {
            for (int b = 0; b < 16; b++)
            {
                Assert.AreEqual((words[w] >> b & 1) == 1, bits[w * 16 + b], $"M{w * 16 + b}");
            }
        }
    }

    // ---------------------------------------------------------------
    // ランダム／統合ビルダー／モニタ（D100〜D199 / M100〜M199）
    // ---------------------------------------------------------------

    [TestMethod]
    public void TestRandomBuilder()
    {
        using var mcpx = Connect();

        short s = NextShort();
        int i = random.Next(int.MinValue, int.MaxValue);
        float f = (float)(random.NextDouble() * 1000);
        bool bit = random.Next(2) == 1;

        mcpx.RandomWrite(b => b
            .Add(Prefix.D, "100", s)
            .Add(Prefix.D, "110", i)
            .Add(Prefix.D, "120", f)
            .Add(Prefix.M, "100", bit));

        short rs = 0; int ri = 0; float rf = 0; bool rbit = !bit;
        mcpx.RandomRead(b => b
            .Add<short>(Prefix.D, "100", v => rs = v)
            .Add<int>(Prefix.D, "110", v => ri = v)
            .Add<float>(Prefix.D, "120", v => rf = v)
            .Add<bool>(Prefix.M, "100", v => rbit = v));

        Assert.AreEqual(s, rs);
        Assert.AreEqual(i, ri);
        Assert.AreEqual(f, rf);
        Assert.AreEqual(bit, rbit);
    }

    [TestMethod]
    public async Task TestCombinedBuilderAsync()
    {
        using var mcpx = Connect();

        var block = RandomValues(50, NextShort);
        var bits = RandomValues(20, () => random.Next(2) == 1);
        int i = random.Next(int.MinValue, int.MaxValue);
        bool bit = random.Next(2) == 1;

        await mcpx.WriteAsync(b => b
            .Add(Prefix.D, "130", block)      // 連続
            .Add(Prefix.M, "110", bits)       // 連続（ビット）
            .Add(Prefix.D, "190", i)          // ランダム
            .Add(Prefix.M, "150", bit));      // ランダム（ビット）

        short[] rblock = []; bool[] rbits = []; int ri = 0; bool rbit = !bit;
        await mcpx.ReadAsync(b => b
            .Add<short>(Prefix.D, "130", 50, v => rblock = v)
            .Add<bool>(Prefix.M, "110", 20, v => rbits = v)
            .Add<int>(Prefix.D, "190", v => ri = v)
            .Add<bool>(Prefix.M, "150", v => rbit = v));

        CollectionAssert.AreEqual(block, rblock);
        CollectionAssert.AreEqual(bits, rbits);
        Assert.AreEqual(i, ri);
        Assert.AreEqual(bit, rbit);
    }

    [TestMethod]
    public void TestMonitor()
    {
        using var mcpx = Connect();

        short ms = 0; int mi = 0; bool mbit = false;
        var session = mcpx.MonitorRegist(b => b
            .Add<short>(Prefix.D, "180", v => ms = v)
            .Add<int>(Prefix.D, "182", v => mi = v)
            .Add<bool>(Prefix.M, "160", v => mbit = v));

        // 登録後に値を変えても、同じセッションで最新値が読めること
        for (int n = 0; n < 3; n++)
        {
            short s = NextShort();
            int i = random.Next(int.MinValue, int.MaxValue);
            bool bit = n % 2 == 0;

            mcpx.Write(Prefix.D, "180", s);
            mcpx.Write(Prefix.D, "182", i);
            mcpx.Write(Prefix.M, "160", bit);

            session.Read();

            Assert.AreEqual(s, ms);
            Assert.AreEqual(i, mi);
            Assert.AreEqual(bit, mbit);
        }
    }

    // ---------------------------------------------------------------
    // 文字列（D200〜D299）
    // ---------------------------------------------------------------

    [TestMethod]
    public async Task TestStringWriteRead()
    {
        using var mcpx = Connect();

        const string ascii = "McpX Test";   // 9バイト → 5ワード + 終端
        const string sjis = "三菱PLCテスト";  // Shift_JIS 13バイト

        mcpx.WriteString(Prefix.D, "200", ascii);
        await mcpx.WriteStringAsync(Prefix.D, "250", sjis);

        Assert.AreEqual(ascii, mcpx.ReadString(Prefix.D, "200", 10));
        Assert.AreEqual(sjis, await mcpx.ReadStringAsync(Prefix.D, "250", 10));
    }
}
