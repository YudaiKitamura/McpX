using McpXLib.Enums;

namespace TestMcpX;

/// <summary>
/// 複数ブロック一括読出し・書込みに対応したPLC（iQ-R の実機・GX Simulator3）を使った結合テストの共通実装。
/// </summary>
/// <remarks>
/// <see cref="PlcIntegrationTestBase"/> のテストに加え、D7000〜D8999・W200〜W20F・M11000〜M11031 を上書きします。
/// </remarks>
public abstract class MultiBlockPlcTestBase : PlcIntegrationTestBase
{
    // ---------------------------------------------------------------
    // 複数ブロック一括読出し・書込み（D7000〜D8999 / W200〜W20F / M11000〜M11031）
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
    public void TestIntegratedMultiBlock()
    {
        using var mcpx = Connect();

        // 複数ブロックを使う場合（true）と使わない場合（false）で、同じ値を読み書きできること
        foreach (var useMultiBlock in new[] { true, false })
        {
            mcpx.UseMultiBlockAccess = useMultiBlock;
            short value = (short)(useMultiBlock ? 100 : 200);

            mcpx.Write(b => b
                .Add(Prefix.D, "7000", new[] { value, (short)(value + 1) })
                .Add(Prefix.D, "8000", new[] { (short)(value + 2) })
                .Add(Prefix.M, "11000", Enumerable.Repeat(useMultiBlock, 10).ToArray()));

            short[] a = []; short[] c = []; bool[] bits = [];
            mcpx.Read(b => b
                .Add<short>(Prefix.D, "7000", 2, v => a = v)
                .Add<short>(Prefix.D, "8000", 1, v => c = v)
                .Add<bool>(Prefix.M, "11000", 10, v => bits = v));

            CollectionAssert.AreEqual(new[] { value, (short)(value + 1) }, a, $"UseMultiBlockAccess={useMultiBlock}");
            CollectionAssert.AreEqual(new[] { (short)(value + 2) }, c, $"UseMultiBlockAccess={useMultiBlock}");
            Assert.IsTrue(bits.All(x => x == useMultiBlock), $"UseMultiBlockAccess={useMultiBlock}");
        }
    }
}
