using McpXLib.Interfaces;

namespace McpXLib.Builders;

/// <summary>
/// 要求電文の「要求データ長」以降（監視タイマ、コマンド、サブコマンド、要求データ）
/// </summary>
internal class CommandPacketBuilder : IPacketBuilder
{
    private readonly byte[] command;
    private readonly byte[] subCommand;
    private readonly IPayloadBuilder? payloadBuilder;
    private readonly byte[] monitoringTimer;

    /// <param name="command">コマンド（リトルエンディアン。例: 0401H は [0x01, 0x04]）</param>
    /// <param name="subCommand">サブコマンド（リトルエンディアン）</param>
    /// <param name="payloadBuilder">要求データ（ないコマンドは null）</param>
    /// <param name="monitoringTimer">監視タイマ（ミリ秒、0 または 250 以上。250ms 単位）</param>
    internal CommandPacketBuilder(byte[] command, byte[] subCommand, IPayloadBuilder? payloadBuilder, ushort monitoringTimer)
    {
        if (monitoringTimer > 0 && monitoringTimer < 250)
        {
            throw new ArgumentOutOfRangeException(nameof(monitoringTimer), "The allowable monitoring timer values are 0 or 250 ms or greater.");
        }

        this.command = command;
        this.subCommand = subCommand;
        this.payloadBuilder = payloadBuilder;
        this.monitoringTimer = BitConverter.GetBytes((ushort)(monitoringTimer / 250)); // 1 = 250ms
    }

    internal CommandPacketBuilder(byte[] command, byte[] subCommand, ushort monitoringTimer)
        : this(command, subCommand, null, monitoringTimer)
    {
    }

    public byte[] ToBinaryBytes() => Build(isAscii: false);

    public byte[] ToAsciiBytes() => Build(isAscii: true);

    private byte[] Build(bool isAscii)
    {
        var body = new List<byte>();
        var writer = new PacketWriter(body, isAscii);
        writer.Value(monitoringTimer);
        writer.Value(command);
        writer.Value(subCommand);
        payloadBuilder?.AppendPayload(body, isAscii);

        // 要求データ長（バイナリはバイト数、ASCII は文字数）を先頭に付ける
        var packet = new List<byte>(body.Count + 4);
        new PacketWriter(packet, isAscii).Number((ushort)body.Count, 2);
        packet.AddRange(body);
        return packet.ToArray();
    }
}
