using McpXLib.Interfaces;

namespace TestMcpX;

/// <summary>
/// 3Eフレーム(バイナリ)のワード一括読み書き(0401/1401 サブコマンド0000)に応答する偽トランスポート。
/// 受信した要求を記録し、正常応答を返す。
/// </summary>
internal sealed class FakePlcTransport : IPlcTransport
{
    internal sealed record RecordedRequest(ushort Command, uint DeviceNumber, byte DeviceCode, ushort Points, byte[] Data);

    internal List<RecordedRequest> Requests { get; } = new();

    // 最後に受信した要求パケット（ヘッダ含む）
    internal byte[]? LastPacket { get; private set; }

    // 読み込み時に返すワード値。未設定のデバイスは「デバイス番号の下位16ビット」を返す。
    internal Dictionary<uint, ushort> Words { get; } = new();

    // コマンドごとに返す終了コード（未設定は 0000 = 正常）。例：リモートアンロック(1630)を C201 で失敗させる。
    internal Dictionary<ushort, ushort> EndCodes { get; } = new();

    internal bool Disposed { get; private set; }

    // 直近のモニタ登録(0801)の点数。モニタ(0802)の応答長に使う。
    private int monitorWords;
    private int monitorDoubleWords;

    public byte[] Request(byte[] packet, IReceiveLengthParser receiveLengthParser)
    {
        // 50 00 | NW PC IO(2) ST | 長さ(2) | 監視タイマ(2) | コマンド(2) | サブコマンド(2) | デバイス番号(3) コード(1) | 点数(2) | データ
        ushort command = BitConverter.ToUInt16(packet, 11);
        bool hasDevice = packet.Length >= 21;
        uint deviceNumber = hasDevice ? (uint)(packet[15] | packet[16] << 8 | packet[17] << 16) : 0;
        byte deviceCode = hasDevice ? packet[18] : (byte)0;
        ushort points = hasDevice ? BitConverter.ToUInt16(packet, 19) : (ushort)0;
        byte[] data = packet.Skip(21).ToArray();

        Requests.Add(new RecordedRequest(command, deviceNumber, deviceCode, points, data));
        LastPacket = packet;

        if (EndCodes.TryGetValue(command, out var endCode) && endCode != 0)
        {
            // D0 00 | NW PC IO(2) ST | 長さ(2) | 終了コード(2) | エラー情報(9)
            var error = new List<byte> { 0xD0, 0x00 };
            error.AddRange(packet.Skip(2).Take(5));
            error.AddRange(BitConverter.GetBytes((ushort)11));
            error.AddRange(BitConverter.GetBytes(endCode));
            error.AddRange(packet.Skip(2).Take(5));
            error.AddRange(packet.Skip(11).Take(4));
            return error.ToArray();
        }

        var content = new List<byte>();
        if (command == 0x0801)
        {
            monitorWords = packet[15];
            monitorDoubleWords = packet[16];
        }
        else if (command == 0x0802)
        {
            content.AddRange(new byte[monitorWords * 2 + monitorDoubleWords * 4]);
        }
        else if (command == 0x0401)
        {
            for (uint i = 0; i < points; i++)
            {
                uint n = deviceNumber + i;
                content.AddRange(BitConverter.GetBytes(Words.TryGetValue(n, out var w) ? w : (ushort)(n & 0xFFFF)));
            }
        }

        // D0 00 | NW PC IO(2) ST | 長さ(2) | 終了コード(2) | データ
        var response = new List<byte> { 0xD0, 0x00 };
        response.AddRange(packet.Skip(2).Take(5));
        response.AddRange(BitConverter.GetBytes((ushort)(content.Count + 2)));
        response.AddRange([0x00, 0x00]);
        response.AddRange(content);
        return response.ToArray();
    }

    public Task<byte[]> RequestAsync(byte[] packet, IReceiveLengthParser receiveLengthParser)
        => Task.FromResult(Request(packet, receiveLengthParser));

    public void Dispose()
    {
        Disposed = true;
    }
}
