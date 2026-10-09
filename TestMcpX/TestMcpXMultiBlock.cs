using McpXLib;
using McpXLib.Enums;
using McpXLib.Exceptions;
using McpXLib.Interfaces;

namespace TestMcpX;

/// <summary>
/// 複数ブロック一括読出し・書込み（BlockRead / BlockWrite）と、統合アクセスへの組み込みのテスト。
/// </summary>
[TestClass]
public sealed class TestMcpXMultiBlock
{
    private static short[] Values(int length, int seed) => Enumerable.Range(0, length).Select(i => (short)(seed + i)).ToArray();

    // ---------------------------------------------------------------
    // BlockRead / BlockWrite
    // ---------------------------------------------------------------

    [TestMethod]
    public async Task TestBlockWriteRead()
    {
        var transport = new FakeMultiBlockTransport();
        using var mcpx = new McpX(transport);

        var bits = Enumerable.Range(0, 32).Select(i => i % 3 == 0).ToArray();
        mcpx.BlockWrite(b => b
            .Add(Prefix.D, "0", new short[] { 1, -2, 3 })
            .Add(Prefix.W, "100", new[] { 123456, -7 })
            .Add(Prefix.M, "16", bits));

        Assert.AreEqual(1, transport.Count(0x1406));

        short[] d = []; int[] w = []; bool[] m = []; bool[] m5 = []; ushort[] mw = [];
        await mcpx.BlockReadAsync(b => b
            .Add<short>(Prefix.D, "0", 3, v => d = v)
            .Add<int>(Prefix.W, "100", 2, v => w = v)
            .Add<bool>(Prefix.M, "16", 32, v => m = v)
            .Add<bool>(Prefix.M, "19", 5, v => m5 = v)              // 16点単位にそろっていない範囲
            .Add<ushort>(Prefix.M, "16", 2, v => mw = v));          // ビットデバイスをワード単位で

        Assert.AreEqual(1, transport.Count(0x0406));
        CollectionAssert.AreEqual(new short[] { 1, -2, 3 }, d);
        CollectionAssert.AreEqual(new[] { 123456, -7 }, w);
        CollectionAssert.AreEqual(bits, m);
        CollectionAssert.AreEqual(bits.Skip(3).Take(5).ToArray(), m5);
        Assert.AreEqual((ushort)Enumerable.Range(0, 16).Sum(i => bits[i] ? 1 << i : 0), mw[0]);
    }

    [TestMethod]
    public void TestBlockReadSplit()
    {
        var transport = new FakeMultiBlockTransport();
        using var mcpx = new McpX(transport);
        var d = Values(1000, 1);
        var r = Values(500, 5000);
        mcpx.BatchWrite(Prefix.D, "0", d);
        mcpx.BatchWrite(Prefix.R, "0", r);
        transport.Requests.Clear();

        // 1000点は 960 + 40 に分割し、合計 960点以下ごとにリクエストを分ける
        short[] rd = []; short[] rr = [];
        mcpx.BlockRead(b => b.Add<short>(Prefix.D, "0", 1000, v => rd = v).Add<short>(Prefix.R, "0", 500, v => rr = v));

        CollectionAssert.AreEqual(d, rd);
        CollectionAssert.AreEqual(r, rr);
        Assert.AreEqual(2, transport.Count(0x0406));
        Assert.IsTrue(transport.Requests.Where(q => q.Command == 0x0406).All(q => q.Points <= 960));
    }

    [TestMethod]
    public void TestBlockReadBitDeviceSplit()
    {
        var transport = new FakeMultiBlockTransport();
        using var mcpx = new McpX(transport);

        // ビットデバイスを1000ワード → 2ブロック目は M15360（960ワード × 16点）から
        mcpx.BlockRead(b => b.Add<ushort>(Prefix.M, "0", 1000, _ => { }).Add<short>(Prefix.D, "0", 1, _ => { }));

        var starts = transport.Requests.Where(q => q.Command == 0x0406).SelectMany(q => q.Blocks).Where(x => x.Code == 0x90).Select(x => x.Number).ToArray();
        CollectionAssert.AreEqual(new uint[] { 0, 15360 }, starts);
    }

    [TestMethod]
    public void TestBlockCountLimit()
    {
        var transport = new FakeMultiBlockTransport();
        using var mcpx = new McpX(transport);

        // 121ブロック → 120 + 1
        mcpx.BlockRead(b =>
        {
            for (int i = 0; i < 121; i++)
            {
                b.Add<short>(Prefix.D, (i * 10).ToString(), 1, _ => { });
            }
        });

        CollectionAssert.AreEqual(new[] { 120, 1 }, transport.Requests.Select(q => q.Blocks.Count).ToArray());
    }

    [TestMethod]
    public void TestBlockWriteLimit()
    {
        var transport = new FakeMultiBlockTransport();
        using var mcpx = new McpX(transport);

        // (ブロック数×4)+点数 ≦ 960。1ブロックは最大 956点に分割される。
        var d = Values(1000, 1);
        mcpx.BlockWrite(b => b.Add(Prefix.D, "0", d));

        CollectionAssert.AreEqual(new[] { 956, 44 }, transport.Requests.Select(q => q.Points).ToArray());
        CollectionAssert.AreEqual(d, mcpx.BatchRead<short>(Prefix.D, "0", 1000));
    }

    [TestMethod]
    public void TestBlockBuilderArguments()
    {
        using var mcpx = new McpX(new FakeMultiBlockTransport());

        // ワードデバイスへの bool、16の倍数でない bool の書き込みは不可
        Assert.ThrowsException<ArgumentException>(() => mcpx.BlockRead(b => b.Add<bool>(Prefix.D, "0", 16, _ => { })));
        Assert.ThrowsException<ArgumentException>(() => mcpx.BlockWrite(b => b.Add(Prefix.M, "0", new bool[10])));
        Assert.ThrowsException<ArgumentException>(() => mcpx.BlockWrite(b => b.Add(Prefix.D, "0", new bool[16])));
    }

    [TestMethod]
    public void TestBlockReadUnsupported()
    {
        // 専用メソッドは切り替えずに例外
        using var mcpx = new McpX(new FakeMultiBlockTransport { SupportsMultiBlock = false });

        var ex = Assert.ThrowsException<McProtocolException>(() => mcpx.BlockRead(b => b.Add<short>(Prefix.D, "0", 1, _ => { })));
        Assert.AreEqual((ushort)0xC059, ex.ErrorCode);
    }

    // ---------------------------------------------------------------
    // 統合アクセス（Read / Write）
    // ---------------------------------------------------------------

    [TestMethod]
    public async Task TestIntegratedUsesMultiBlock()
    {
        var transport = new FakeMultiBlockTransport();
        using var mcpx = new McpX(transport) { UseMultiBlockAccess = true };

        await mcpx.WriteAsync(b => b
            .Add(Prefix.D, "0", new short[] { 1, 2, 3 })
            .Add(Prefix.M, "100", Enumerable.Repeat(true, 32).ToArray())
            .Add(Prefix.M, "0", Enumerable.Repeat(true, 10).ToArray()));   // 16の倍数でない bool は 1401（ビット単位）

        CollectionAssert.AreEqual(new ushort[] { 0x1406, 0x1401 }, transport.Requests.Select(q => q.Command).ToArray());
        Assert.AreEqual((ushort)0x0001, transport.Requests[1].SubCommand);
        Assert.IsFalse(mcpx.Read<bool>(Prefix.M, "10"), "M10 以降は上書きしない");
        transport.Requests.Clear();

        short[] d = []; bool[] m = [];
        mcpx.Read(b => b.Add<short>(Prefix.D, "0", 3, v => d = v).Add<bool>(Prefix.M, "0", 10, v => m = v));

        CollectionAssert.AreEqual(new ushort[] { 0x0406 }, transport.Requests.Select(q => q.Command).ToArray());
        CollectionAssert.AreEqual(new short[] { 1, 2, 3 }, d);
        Assert.IsTrue(m.All(x => x));
    }

    [TestMethod]
    public void TestIntegratedSingleRange()
    {
        // 範囲が1つだけなら、有効にしていても従来どおり 0401
        var transport = new FakeMultiBlockTransport();
        using var mcpx = new McpX(transport) { UseMultiBlockAccess = true };

        mcpx.Read(b => b.Add<short>(Prefix.D, "0", 3, _ => { }));

        CollectionAssert.AreEqual(new ushort[] { 0x0401 }, transport.Requests.Select(q => q.Command).ToArray());
    }

    [TestMethod]
    public async Task TestIntegratedDefault()
    {
        // 既定（無効）では、接続先が対応していても・非対応でも 0406 / 1406 を送らない（PLC側でエラーにしない）
        foreach (var supports in new[] { true, false })
        {
            var transport = new FakeMultiBlockTransport { SupportsMultiBlock = supports };
            using var mcpx = new McpX(transport);
            Assert.IsFalse(mcpx.UseMultiBlockAccess);

            mcpx.Write(b => b.Add(Prefix.D, "0", new short[] { 7, 8 }).Add(Prefix.D, "100", new short[] { 9 }));
            short[] a = []; short[] c = [];
            await mcpx.ReadAsync(b => b.Add<short>(Prefix.D, "0", 2, v => a = v).Add<short>(Prefix.D, "100", 1, v => c = v));

            CollectionAssert.AreEqual(new ushort[] { 0x1401, 0x1401, 0x0401, 0x0401 }, transport.Requests.Select(q => q.Command).ToArray());
            CollectionAssert.AreEqual(new short[] { 7, 8 }, a);
            CollectionAssert.AreEqual(new short[] { 9 }, c);
        }
    }

    [TestMethod]
    public async Task TestIntegratedUnsupported()
    {
        // 有効にしたが接続先が非対応の場合は、切り替えずに例外（何も書き込まない）
        var transport = new FakeMultiBlockTransport { SupportsMultiBlock = false };
        using var mcpx = new McpX(transport) { UseMultiBlockAccess = true };

        var ex = await Assert.ThrowsExceptionAsync<McProtocolException>(() =>
            mcpx.ReadAsync(b => b.Add<short>(Prefix.D, "0", 2, _ => { }).Add<short>(Prefix.D, "100", 1, _ => { })));
        Assert.AreEqual((ushort)0xC059, ex.ErrorCode);

        Assert.ThrowsException<McProtocolException>(() =>
            mcpx.Write(b => b.Add(Prefix.D, "0", new short[] { 1 }).Add(Prefix.D, "100", new short[] { 2 })));
        CollectionAssert.AreEqual(new ushort[] { 0x0406, 0x1406 }, transport.Requests.Select(q => q.Command).ToArray());
    }

    /// <summary>
    /// 3Eフレーム(バイナリ)・Q/L形式の 0401 / 1401 / 0406 / 1406 に応答するダミートランスポート。
    /// </summary>
    private sealed class FakeMultiBlockTransport : IPlcTransport
    {
        internal sealed record Block(uint Number, byte Code, ushort Points);

        internal sealed record RecordedRequest(ushort Command, ushort SubCommand, List<Block> Blocks)
        {
            internal int Points => Blocks.Sum(b => b.Points);
        }

        // false の場合、0406 / 1406 にエラーコード C059（非対応）を返す
        internal bool SupportsMultiBlock { get; set; } = true;

        internal List<RecordedRequest> Requests { get; } = new();

        // ワードデバイス: (コード, 番号) → ワード値、ビットデバイス: (コード, 番号) → ビット値
        private readonly Dictionary<(byte, uint), ushort> words = new();
        private readonly Dictionary<(byte, uint), bool> bits = new();

        private static readonly byte[] BitDeviceCodes = [0x9C, 0x9D, 0x90, 0x92, 0x93, 0x94, 0xA0, 0xA1];

        internal int Count(ushort command) => Requests.Count(q => q.Command == command);

        private static bool IsBit(byte code) => BitDeviceCodes.Contains(code);

        // 1点分のワード（ビットデバイスは番号から16ビット分）
        private ushort GetWord(byte code, uint number) => IsBit(code)
            ? (ushort)Enumerable.Range(0, 16).Sum(i => bits.TryGetValue((code, number + (uint)i), out var b) && b ? 1 << i : 0)
            : words.TryGetValue((code, number), out var w) ? w : (ushort)0;

        private void SetWord(byte code, uint number, ushort value)
        {
            if (IsBit(code))
            {
                for (int i = 0; i < 16; i++)
                {
                    bits[(code, number + (uint)i)] = (value >> i & 1) == 1;
                }
            }
            else
            {
                words[(code, number)] = value;
            }
        }

        public byte[] Request(byte[] packet, IReceiveLengthParser receiveLengthParser)
        {
            // 50 00 | NW PC IO(2) ST | 長さ(2) | 監視タイマ(2) | コマンド(2) | サブコマンド(2) | 要求データ
            ushort command = BitConverter.ToUInt16(packet, 11);
            ushort subCommand = BitConverter.ToUInt16(packet, 13);
            var data = packet.Skip(15).ToArray();

            static Block ReadBlock(byte[] d, int i) => new((uint)(d[i] | d[i + 1] << 8 | d[i + 2] << 16), d[i + 3], BitConverter.ToUInt16(d, i + 4));

            var content = new List<byte>();
            switch (command)
            {
                case 0x0401:
                case 0x1401:
                {
                    var block = ReadBlock(data, 0);
                    Requests.Add(new RecordedRequest(command, subCommand, [block]));
                    var values = data.Skip(6).ToArray();
                    if (subCommand == 0x0001)
                    {
                        // ビット単位: 1バイトに2点（上位4ビットが先）
                        for (int i = 0; i < block.Points; i++)
                        {
                            var key = (block.Code, block.Number + (uint)i);
                            if (command == 0x1401)
                            {
                                bits[key] = ((values[i / 2] >> (i % 2 == 0 ? 4 : 0)) & 1) == 1;
                            }
                            else
                            {
                                if (i % 2 == 0) content.Add(0);
                                content[^1] |= (byte)((bits.TryGetValue(key, out var b) && b ? 1 : 0) << (i % 2 == 0 ? 4 : 0));
                            }
                        }
                    }
                    else
                    {
                        uint step = IsBit(block.Code) ? 16u : 1u;
                        for (int i = 0; i < block.Points; i++)
                        {
                            if (command == 0x1401) SetWord(block.Code, block.Number + (uint)i * step, BitConverter.ToUInt16(values, i * 2));
                            else content.AddRange(BitConverter.GetBytes(GetWord(block.Code, block.Number + (uint)i * step)));
                        }
                    }
                    break;
                }
                case 0x0406:
                case 0x1406:
                {
                    // 非対応の場合は、何も読み書きせずにエラーを返す
                    if (!SupportsMultiBlock)
                    {
                        Requests.Add(new RecordedRequest(command, subCommand, []));
                        return Response(0xC059, []);
                    }

                    int count = data[0] + data[1];
                    var blocks = new List<Block>();
                    int index = 2;
                    for (int n = 0; n < count; n++)
                    {
                        var block = ReadBlock(data, index);
                        blocks.Add(block);
                        index += 6;
                        uint step = IsBit(block.Code) ? 16u : 1u;
                        for (int i = 0; i < block.Points; i++)
                        {
                            if (command == 0x1406)
                            {
                                SetWord(block.Code, block.Number + (uint)i * step, BitConverter.ToUInt16(data, index));
                                index += 2;
                            }
                            else
                            {
                                content.AddRange(BitConverter.GetBytes(GetWord(block.Code, block.Number + (uint)i * step)));
                            }
                        }
                    }
                    Requests.Add(new RecordedRequest(command, subCommand, blocks));
                    break;
                }
                default:
                    return Response(0xC059, []);
            }

            return Response(0, content.ToArray());
        }

        // D0 00 | NW PC IO(2) ST | 長さ(2) | 終了コード(2) | 応答データ（異常時はエラー情報9バイト）
        private static byte[] Response(ushort endCode, byte[] content)
        {
            if (endCode != 0)
            {
                content = [0x00, 0xFF, 0xFF, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00];
            }

            return [0xD0, 0x00, 0x00, 0xFF, 0xFF, 0x03, 0x00, .. BitConverter.GetBytes((ushort)(content.Length + 2)), .. BitConverter.GetBytes(endCode), .. content];
        }

        public Task<byte[]> RequestAsync(byte[] packet, IReceiveLengthParser receiveLengthParser)
            => Task.FromResult(Request(packet, receiveLengthParser));

        [Obsolete]
        public byte[] Request(byte[] packet) => throw new NotSupportedException();

        [Obsolete]
        public Task<byte[]> RequestAsync(byte[] packet) => throw new NotSupportedException();

        public void Dispose()
        {
        }
    }
}
