using McpXLib.Interfaces;

namespace TestMcpX;

/// <summary>
/// <see cref="FakePlcCore"/> に要求を渡すダミートランスポート（C# のテスト用）。
/// </summary>
internal sealed class FakePlcTransport : IPlcTransport
{
    internal FakePlcCore Core { get; }

    /// <param name="stateful">書き込んだ値を保持し、読み込みで返す場合に <c>true</c>（<see cref="FakePlcCore.Stateful"/>）。</param>
    internal FakePlcTransport(bool stateful = false)
    {
        Core = new FakePlcCore { Stateful = stateful };
    }

    internal List<FakePlcCore.RecordedRequest> Requests => Core.Requests;

    internal byte[]? LastPacket => Core.LastPacket;

    internal Dictionary<uint, ushort> Words => Core.Words;

    internal HashSet<uint> Bits => Core.Bits;

    internal Dictionary<ushort, ushort> EndCodes => Core.EndCodes;

    internal bool Disposed { get; private set; }

    public byte[] Request(byte[] packet, IReceiveLengthParser receiveLengthParser) => Core.Handle(packet);

    public Task<byte[]> RequestAsync(byte[] packet, IReceiveLengthParser receiveLengthParser, CancellationToken cancellationToken)
        => Task.FromResult(Request(packet, receiveLengthParser));

    public void Dispose()
    {
        Disposed = true;
    }
}
