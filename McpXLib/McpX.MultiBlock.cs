using McpXLib.Commands;
using McpXLib.Enums;
using McpXLib.Exceptions;
using McpXLib.Utils;

namespace McpXLib;

// 複数ブロック一括読出し・書込み（コマンド: 0406 / 1406）
public partial class McpX
{
    /// <summary>
    /// 統合アクセス（<see cref="Read(Action{ReadBuilder})"/> / <see cref="Write(Action{WriteBuilder})"/>）で、
    /// 複数ブロック一括読出し・書込み（コマンド: 0406 / 1406）を使う場合に<c>true</c>を指定します。（デフォルトは、<c>false</c>です。）
    /// </summary>
    /// <remarks>
    /// <c>true</c> の場合、連続アクセスの範囲が2つ以上あれば、複数ブロックで1回の交信にまとめます。<br/>
    /// CPUユニットの内蔵Ethernetポートなど、接続先によっては複数ブロックに対応していません。
    /// 非対応の接続先で <c>true</c> にすると、PLCからエラーコード <c>C059</c> が返り <see cref="McProtocolException"/> をスローするため、
    /// 接続先が対応している場合のみ指定してください。
    /// </remarks>
    public bool UseMultiBlockAccess { get; set; }

    /// <summary>
    /// 複数ブロック一括読み込み（ビルダー）
    /// </summary>
    /// <remarks>
    /// 複数の範囲（ブロック）を、複数ブロック一括読出し（コマンド: 0406）でまとめて読み込みます。<br/>
    /// ブロック数・点数の上限（MELSEC-Q/Lシリーズ: 120ブロック、iQ-Rシリーズ: 60ブロック、合計960点）を超える場合は、自動的に複数のリクエストに分割します。<br/>
    /// 接続先が複数ブロックに非対応の場合は、<see cref="McProtocolException"/>（<see cref="McProtocolException.ErrorCode"/> = <c>0xC059</c>）をスローします。<br/>
    /// ユニバーサルモデルQCPU・LCPUでは、CPUユニットの"サービス処理設定"によってデータが泣き別れることがあります。
    /// </remarks>
    /// <param name="build">読み込むデバイス範囲とコールバックを登録するビルダー操作を指定します。</param>
    /// <exception cref="ArgumentException">複数ブロックで読み込めない指定（ワードデバイスへの <c>bool</c>）の場合に例外をスローします。</exception>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="RecivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    public void BlockRead(Action<BlockReadBuilder> build)
    {
        var builder = new BlockReadBuilder();
        build(builder);
        ExecuteBlockRead(builder.entries);
    }

    /// <summary>
    /// 複数ブロック一括読み込み（ビルダー・非同期）
    /// </summary>
    /// <remarks>
    /// 複数の範囲（ブロック）を、複数ブロック一括読出し（コマンド: 0406）でまとめて非同期で読み込みます。<br/>
    /// 詳細は <see cref="BlockRead(Action{BlockReadBuilder})"/> を参照してください。
    /// </remarks>
    /// <param name="build">読み込むデバイス範囲とコールバックを登録するビルダー操作を指定します。</param>
    /// <exception cref="ArgumentException">複数ブロックで読み込めない指定（ワードデバイスへの <c>bool</c>）の場合に例外をスローします。</exception>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="RecivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    public async Task BlockReadAsync(Action<BlockReadBuilder> build)
    {
        var builder = new BlockReadBuilder();
        build(builder);
        await ExecuteBlockReadAsync(builder.entries);
    }

    /// <summary>
    /// 複数ブロック一括書き込み（ビルダー）
    /// </summary>
    /// <remarks>
    /// 複数の範囲（ブロック）に、複数ブロック一括書込み（コマンド: 1406）でまとめて書き込みます。<br/>
    /// 上限（MELSEC-Q/Lシリーズ: (ブロック数×4)+点数、iQ-Rシリーズ: (ブロック数×9)+点数 が960以下）を超える場合は、自動的に複数のリクエストに分割します。<br/>
    /// ビットデバイスは16点（1ワード）単位で書き込むため、<c>bool</c> は16の倍数の要素数を指定してください。<br/>
    /// 接続先が複数ブロックに非対応の場合は、<see cref="McProtocolException"/>（<see cref="McProtocolException.ErrorCode"/> = <c>0xC059</c>）をスローします。<br/>
    /// ユニバーサルモデルQCPU・LCPUでは、CPUユニットの"サービス処理設定"によってデータが泣き別れることがあります。
    /// </remarks>
    /// <param name="build">書き込むデバイス範囲と値を登録するビルダー操作を指定します。</param>
    /// <exception cref="ArgumentException">複数ブロックで書き込めない指定の場合に例外をスローします。</exception>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="RecivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    public void BlockWrite(Action<BlockWriteBuilder> build)
    {
        var builder = new BlockWriteBuilder();
        build(builder);
        ExecuteBlockWrite(builder.entries);
    }

    /// <summary>
    /// 複数ブロック一括書き込み（ビルダー・非同期）
    /// </summary>
    /// <remarks>
    /// 複数の範囲（ブロック）に、複数ブロック一括書込み（コマンド: 1406）でまとめて非同期で書き込みます。<br/>
    /// 詳細は <see cref="BlockWrite(Action{BlockWriteBuilder})"/> を参照してください。
    /// </remarks>
    /// <param name="build">書き込むデバイス範囲と値を登録するビルダー操作を指定します。</param>
    /// <exception cref="ArgumentException">複数ブロックで書き込めない指定の場合に例外をスローします。</exception>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="RecivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    public async Task BlockWriteAsync(Action<BlockWriteBuilder> build)
    {
        var builder = new BlockWriteBuilder();
        build(builder);
        await ExecuteBlockWriteAsync(builder.entries);
    }

    // 1リクエスト分のブロック。entryIndex の範囲の、offset（点）から Block.Points 点分。
    private sealed class BlockChunk(DeviceBlock block, int entryIndex, int offset)
    {
        internal DeviceBlock Block { get; } = block;

        internal int EntryIndex { get; } = entryIndex;

        internal int Offset { get; } = offset;
    }

    // 統合アクセスで複数ブロックを使うか（まとめる範囲が2つ以上の場合のみ効果がある）
    private bool CanUseMultiBlock(int entryCount) => UseMultiBlockAccess && entryCount >= 2;

    // 範囲を maxPoints 点ごとのブロックに分け、上限に収まるようにリクエストへ詰める。
    // ビットデバイスは1点=16デバイス分のため、分割時の先頭アドレスを16倍進める。
    private List<List<BlockChunk>> PackBlocks(IReadOnlyList<(Prefix prefix, string address, int points)> ranges, Func<int, int, ushort[]?> getWords, int blockCost)
    {
        int maxBlockCount = MultiBlockReadCommand.GetMaxBlockCount(ProcessorSeries);
        int maxPoints = MultiBlockReadCommand.MAX_POINTS - blockCost;

        var requests = new List<List<BlockChunk>>();
        var current = new List<BlockChunk>();
        int currentPoints = 0;

        for (int i = 0; i < ranges.Count; i++)
        {
            var (prefix, address, points) = ranges[i];
            int addressStep = DeviceConverter.IsBitDevice(prefix) ? 16 : 1;

            for (int offset = 0; offset < points; offset += maxPoints)
            {
                int length = Math.Min(maxPoints, points - offset);
                if (current.Count + 1 > maxBlockCount || (current.Count + 1) * blockCost + currentPoints + length > MultiBlockReadCommand.MAX_POINTS)
                {
                    requests.Add(current);
                    current = new List<BlockChunk>();
                    currentPoints = 0;
                }

                var block = new DeviceBlock(
                    prefix,
                    DeviceConverter.GetOffsetAddress(prefix, address, offset * addressStep),
                    (ushort)length,
                    getWords(i, offset)?.Take(length).ToArray());

                current.Add(new BlockChunk(block, i, offset));
                currentPoints += length;
            }
        }

        if (current.Count > 0)
        {
            requests.Add(current);
        }

        return requests;
    }

    private List<List<BlockChunk>> PackReadBlocks(IReadOnlyList<BlockReadEntry> entries)
        => PackBlocks(entries.Select(e => (e.Prefix, e.Address, e.Points)).ToList(), (_, _) => null, 0);

    private List<List<BlockChunk>> PackWriteBlocks(IReadOnlyList<BlockWriteEntry> entries)
        => PackBlocks(
            entries.Select(e => (e.Prefix, e.Address, e.Words.Length)).ToList(),
            (i, offset) => entries[i].Words.Skip(offset).ToArray(),
            MultiBlockWriteCommand.GetBlockCost(ProcessorSeries));

    private void ExecuteBlockRead(IReadOnlyList<BlockReadEntry> entries)
    {
        var buffers = entries.Select(e => new ushort[e.Points]).ToArray();

        foreach (var request in PackReadBlocks(entries))
        {
            var words = MultiBlockRead(request.Select(c => c.Block).ToArray());
            CopyToBuffers(request, words, buffers);
        }

        // 全リクエストの完了後に通知する（途中で失敗した場合は通知しない）
        for (int i = 0; i < entries.Count; i++)
        {
            entries[i].OnWords(buffers[i]);
        }
    }

    private async Task ExecuteBlockReadAsync(IReadOnlyList<BlockReadEntry> entries)
    {
        var buffers = entries.Select(e => new ushort[e.Points]).ToArray();

        foreach (var request in PackReadBlocks(entries))
        {
            var words = await MultiBlockReadAsync(request.Select(c => c.Block).ToArray());
            CopyToBuffers(request, words, buffers);
        }

        for (int i = 0; i < entries.Count; i++)
        {
            entries[i].OnWords(buffers[i]);
        }
    }

    private static void CopyToBuffers(List<BlockChunk> request, ushort[][] words, ushort[][] buffers)
    {
        for (int j = 0; j < request.Count; j++)
        {
            Array.Copy(words[j], 0, buffers[request[j].EntryIndex], request[j].Offset, words[j].Length);
        }
    }

    private void ExecuteBlockWrite(IReadOnlyList<BlockWriteEntry> entries)
    {
        foreach (var request in PackWriteBlocks(entries))
        {
            MultiBlockWrite(request.Select(c => c.Block).ToArray());
        }
    }

    private async Task ExecuteBlockWriteAsync(IReadOnlyList<BlockWriteEntry> entries)
    {
        foreach (var request in PackWriteBlocks(entries))
        {
            await MultiBlockWriteAsync(request.Select(c => c.Block).ToArray());
        }
    }
}
