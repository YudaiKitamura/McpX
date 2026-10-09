namespace TestMcpX;

/// <summary>
/// 3Eフレーム(バイナリ)の要求に応答するダミー PLC の中核（Q/L 形式のデバイス指定）。
/// 一括読み書き(0401/1401)、ランダム読み書き(0403/1402)、複数ブロック一括読み書き(0406/1406)、モニタ(0801/0802)に応答し、
/// 受信した要求を記録する。<see cref="FakePlcTransport"/>（C# のテスト）と、TCP のダミー PLC サーバー（他言語のテスト）で共有する。
/// </summary>
internal sealed class FakePlcCore
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

    /// <summary>
    /// 書き込んだ値を保持し、読み込みで返すか（TCP のダミー PLC サーバーで使う）。
    /// 有効な場合、値はデバイスコードごとに保持し、ビットデバイスをワードで読むと 16 点分のビットを返す。
    /// 未書き込みのワードは、無効な場合と同じく Words の値（未設定は「デバイス番号の下位16ビット」）を返す。
    /// </summary>
    internal bool Stateful { get; init; }

    // 直近のモニタ登録(0801)の点数。モニタ(0802)の応答長に使う。
    private int monitorWords;
    private int monitorDoubleWords;
    private (byte code, uint number)[] monitorDevices = [];

    private readonly Dictionary<(byte code, uint number), ushort> storedWords = new();
    private readonly HashSet<(byte code, uint number)> storedBits = new();

    // ビットデバイスのデバイスコード
    private static readonly HashSet<byte> bitDevices =
    [
        0x9C, 0x9D, 0x90, 0x92, 0x93, 0x94, 0xA0, 0xC1, 0xC0, 0xC7, 0xC6, 0xC4, 0xC3, 0xA1, 0x98, 0xA2, 0xA3, 0x91,
    ];

    private ushort Word(uint n) => Words.TryGetValue(n, out var w) ? w : (ushort)(n & 0xFFFF);

    private ushort GetWord(byte code, uint n)
    {
        if (!Stateful)
        {
            return Word(n);
        }

        if (bitDevices.Contains(code))
        {
            int value = 0;
            for (int b = 0; b < 16; b++)
            {
                value |= storedBits.Contains((code, n + (uint)b)) ? 1 << b : 0;
            }
            return (ushort)value;
        }

        return storedWords.TryGetValue((code, n), out var w) ? w : Word(n);
    }

    private bool GetBit(byte code, uint n) => Stateful ? storedBits.Contains((code, n)) : Bits.Contains(n);

    private void SetWord(byte code, uint n, ushort value)
    {
        if (bitDevices.Contains(code))
        {
            for (int b = 0; b < 16; b++)
            {
                SetBit(code, n + (uint)b, (value >> b & 1) != 0);
            }
            return;
        }

        storedWords[(code, n)] = value;
    }

    private void SetBit(byte code, uint n, bool value)
    {
        if (value)
        {
            storedBits.Add((code, n));
        }
        else
        {
            storedBits.Remove((code, n));
        }
    }

    private static (byte code, uint number) DeviceSpecAt(byte[] packet, int offset) => (packet[offset + 3], DeviceAt(packet, offset));

    /// <summary>
    /// 書き込みコマンドの値を保持します（Stateful のときのみ）。
    /// </summary>
    private void ApplyWrite(byte[] packet, ushort command, ushort subCommand, byte code, uint number, ushort points)
    {
        bool bitUnit = (subCommand & 0x0001) != 0;
        if (command == 0x1401 && bitUnit)
        {
            // ビット単位：1バイトに2点（上位4ビットが先の点）
            for (int i = 0; i < points; i++)
            {
                byte b = packet[21 + i / 2];
                SetBit(code, number + (uint)i, ((i % 2 == 0 ? b >> 4 : b) & 0x0F) != 0);
            }
        }
        else if (command == 0x1401)
        {
            for (int i = 0; i < points; i++)
            {
                SetWord(code, number + (uint)i, BitConverter.ToUInt16(packet, 21 + i * 2));
            }
        }
        else if (command == 0x1402 && bitUnit)
        {
            // ビット単位のランダム書き込み：点数(1) + [デバイス(4) + 値(1)]
            int count = packet[15];
            for (int i = 0; i < count; i++)
            {
                var (c, n) = DeviceSpecAt(packet, 16 + i * 5);
                SetBit(c, n, packet[16 + i * 5 + 4] != 0);
            }
        }
        else if (command == 0x1402)
        {
            // ワード単位のランダム書き込み：ワード点数(1) ダブルワード点数(1) + [デバイス(4) + 値(2)] + [デバイス(4) + 値(4)]
            int words = packet[15];
            int doubleWords = packet[16];
            int offset = 17;
            for (int i = 0; i < words; i++, offset += 6)
            {
                var (c, n) = DeviceSpecAt(packet, offset);
                SetWord(c, n, BitConverter.ToUInt16(packet, offset + 4));
            }
            for (int i = 0; i < doubleWords; i++, offset += 8)
            {
                var (c, n) = DeviceSpecAt(packet, offset);
                SetWord(c, n, BitConverter.ToUInt16(packet, offset + 4));
                SetWord(c, n + 1, BitConverter.ToUInt16(packet, offset + 6));
            }
        }
        else if (command == 0x1406)
        {
            // 複数ブロック一括書き込み：ワードブロック数(1) ビットブロック数(1) + [デバイス(4) + 点数(2) + データ(点数×2)]
            int blocks = packet[15] + packet[16];
            int offset = 17;
            for (int b = 0; b < blocks; b++)
            {
                var (c, n) = DeviceSpecAt(packet, offset);
                ushort blockPoints = BitConverter.ToUInt16(packet, offset + 4);
                offset += 6;
                for (int i = 0; i < blockPoints; i++, offset += 2)
                {
                    // ビットブロックの点数はワード単位（1点 = 16ビット）
                    SetWord(c, bitDevices.Contains(c) ? n + (uint)(i * 16) : n + (uint)i, BitConverter.ToUInt16(packet, offset));
                }
            }
        }
    }

    private static uint DeviceAt(byte[] packet, int offset) => (uint)(packet[offset] | packet[offset + 1] << 8 | packet[offset + 2] << 16);

    /// <summary>
    /// 要求パケットを処理し、応答パケットを返します。
    /// </summary>
    internal byte[] Handle(byte[] packet)
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
        if (Stateful)
        {
            ApplyWrite(packet, command, subCommand, deviceCode, deviceNumber, points);
        }

        if (command == 0x0801)
        {
            monitorWords = packet[15];
            monitorDoubleWords = packet[16];
            monitorDevices = Enumerable.Range(0, monitorWords + monitorDoubleWords).Select(i => DeviceSpecAt(packet, 17 + i * 4)).ToArray();
        }
        else if (command == 0x0802)
        {
            // モニタは、Stateful でない場合は Words に設定した値を返す（未設定は 0）
            ushort MonitorWord(byte code, uint n) => Stateful ? GetWord(code, n) : Words.TryGetValue(n, out var w) ? w : (ushort)0;
            for (int i = 0; i < monitorDevices.Length; i++)
            {
                var (code, n) = monitorDevices[i];
                content.AddRange(BitConverter.GetBytes(MonitorWord(code, n)));
                if (i >= monitorWords)
                {
                    content.AddRange(BitConverter.GetBytes(MonitorWord(code, n + 1)));
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
                var (code, n) = DeviceSpecAt(packet, 17 + i * 4);
                content.AddRange(BitConverter.GetBytes(GetWord(code, n)));
                if (i >= words)
                {
                    content.AddRange(BitConverter.GetBytes(GetWord(code, n + 1)));
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
                var (code, n) = DeviceSpecAt(packet, offset);
                ushort blockPoints = BitConverter.ToUInt16(packet, offset + 4);
                for (uint i = 0; i < blockPoints; i++)
                {
                    // ビットブロックの点数はワード単位（1点 = 16ビット）
                    content.AddRange(BitConverter.GetBytes(GetWord(code, Stateful && bitDevices.Contains(code) ? n + i * 16 : n + i)));
                }
            }
        }
        else if (command == 0x0401 && (subCommand & 0x0001) != 0)
        {
            // ビット単位：1バイトに2点（上位4ビットが先の点）
            for (uint i = 0; i < points; i += 2)
            {
                int hi = GetBit(deviceCode, deviceNumber + i) ? 0x10 : 0x00;
                int lo = i + 1 < points && GetBit(deviceCode, deviceNumber + i + 1) ? 0x01 : 0x00;
                content.Add((byte)(hi | lo));
            }
        }
        else if (command == 0x0401)
        {
            for (uint i = 0; i < points; i++)
            {
                content.AddRange(BitConverter.GetBytes(GetWord(deviceCode, deviceNumber + i)));
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
}
