using McpXLib.Interfaces;

namespace TestMcpX;

/// <summary>
/// 3Eフレーム(バイナリ)の要求に応答する偽トランスポート（Q/L 形式のデバイス指定）。
/// 一括読み書き(0401/1401)、ランダム読み書き(0403/1402)、複数ブロック一括読み書き(0406/1406)、モニタ(0801/0802)に応答し、
/// 受信した要求を記録する。
/// </summary>
internal sealed class FakePlcTransport : IPlcTransport
{
    internal sealed record RecordedRequest(ushort Command, uint DeviceNumber, byte DeviceCode, ushort Points, byte[] Data);

    internal List<RecordedRequest> Requests { get; } = new();

    // 最後に受信した要求パケット（ヘッダ含む）
    internal byte[]? LastPacket { get; private set; }

    // 読み込み時に返すワード値。未設定のデバイスは「デバイス番号の下位16ビット」を返す。
    internal Dictionary<uint, ushort> Words { get; } = new();

    // ビット単位の一括読み込みで ON を返すデバイス番号（それ以外は OFF）
    internal HashSet<uint> Bits { get; } = new();

    // コマンドごとに返す終了コード（未設定は 0000 = 正常）。例：リモートアンロック(1630)を C201 で失敗させる。
    internal Dictionary<ushort, ushort> EndCodes { get; } = new();

    internal bool Disposed { get; private set; }

    // 直近のモニタ登録(0801)の点数。モニタ(0802)の応答長に使う。
    private int monitorWords;
    private int monitorDoubleWords;
    private uint[] monitorDevices = [];

    private ushort Word(uint n) => Words.TryGetValue(n, out var w) ? w : (ushort)(n & 0xFFFF);

    private static uint DeviceAt(byte[] packet, int offset) => (uint)(packet[offset] | packet[offset + 1] << 8 | packet[offset + 2] << 16);

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
        ushort subCommand = BitConverter.ToUInt16(packet, 13);
        if (command == 0x0801)
        {
            monitorWords = packet[15];
            monitorDoubleWords = packet[16];
            monitorDevices = Enumerable.Range(0, monitorWords + monitorDoubleWords).Select(i => DeviceAt(packet, 17 + i * 4)).ToArray();
        }
        else if (command == 0x0802)
        {
            // モニタは Words に設定した値を返す（未設定は 0）
            for (int i = 0; i < monitorDevices.Length; i++)
            {
                uint n = monitorDevices[i];
                ushort lo = Words.TryGetValue(n, out var w0) ? w0 : (ushort)0;
                content.AddRange(BitConverter.GetBytes(lo));
                if (i >= monitorWords)
                {
                    content.AddRange(BitConverter.GetBytes(Words.TryGetValue(n + 1, out var w1) ? w1 : (ushort)0));
                }
            }
        }
        else if (command == 0x0403)
        {
            // ランダム読み込み：ワード → ダブルワードの順（ダブルワードは連続する2ワード）
            int words = packet[15];
            int doubleWords = packet[16];
            for (int i = 0; i < words + doubleWords; i++)
            {
                uint n = DeviceAt(packet, 17 + i * 4);
                content.AddRange(BitConverter.GetBytes(Word(n)));
                if (i >= words)
                {
                    content.AddRange(BitConverter.GetBytes(Word(n + 1)));
                }
            }
        }
        else if (command == 0x0406)
        {
            // 複数ブロック一括読み込み：ワードブロック → ビットブロックの順に、各ブロックの点数分のワードを返す
            int blocks = packet[15] + packet[16];
            for (int b = 0; b < blocks; b++)
            {
                int offset = 17 + b * 6;
                uint n = DeviceAt(packet, offset);
                ushort blockPoints = BitConverter.ToUInt16(packet, offset + 4);
                for (uint i = 0; i < blockPoints; i++)
                {
                    content.AddRange(BitConverter.GetBytes(Word(n + i)));
                }
            }
        }
        else if (command == 0x0401 && (subCommand & 0x0001) != 0)
        {
            // ビット単位：1バイトに2点（上位4ビットが先の点）
            for (uint i = 0; i < points; i += 2)
            {
                int hi = Bits.Contains(deviceNumber + i) ? 0x10 : 0x00;
                int lo = i + 1 < points && Bits.Contains(deviceNumber + i + 1) ? 0x01 : 0x00;
                content.Add((byte)(hi | lo));
            }
        }
        else if (command == 0x0401)
        {
            for (uint i = 0; i < points; i++)
            {
                uint n = deviceNumber + i;
                content.AddRange(BitConverter.GetBytes(Word(n)));
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
