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

    // 読み込み時に返すワード値。未設定のデバイスは「デバイス番号の下位16ビット」を返す。
    internal Dictionary<uint, ushort> Words { get; } = new();

    public byte[] Request(byte[] packet, IReceiveLengthParser receiveLengthParser)
    {
        // 50 00 | NW PC IO(2) ST | 長さ(2) | 監視タイマ(2) | コマンド(2) | サブコマンド(2) | デバイス番号(3) コード(1) | 点数(2) | データ
        ushort command = BitConverter.ToUInt16(packet, 11);
        uint deviceNumber = (uint)(packet[15] | packet[16] << 8 | packet[17] << 16);
        byte deviceCode = packet[18];
        ushort points = BitConverter.ToUInt16(packet, 19);
        byte[] data = packet.Skip(21).ToArray();

        Requests.Add(new RecordedRequest(command, deviceNumber, deviceCode, points, data));

        var content = new List<byte>();
        if (command == 0x0401)
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

    [Obsolete]
    public byte[] Request(byte[] packet) => throw new NotSupportedException();

    [Obsolete]
    public Task<byte[]> RequestAsync(byte[] packet) => throw new NotSupportedException();

    public void Dispose()
    {
    }
}
