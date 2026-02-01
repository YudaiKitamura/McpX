using System.Text;
using McpXLib.Interfaces;

namespace McpXLib.Builders;

internal class CommandPacketBuilder : IPacketBuilder
{
    private readonly byte[] command;
    private readonly byte[] subCommand;
    private readonly IPayloadBuilder? payloadBuilder;
    private byte[] monitoringTimer;

    [Obsolete]
    internal CommandPacketBuilder(byte[] command, byte[] subCommand, IPayloadBuilder payloadBuilder, byte[] monitoringTimer)
    {   
        this.command = command;
        this.subCommand = subCommand;
        this.payloadBuilder = payloadBuilder;
        this.monitoringTimer = monitoringTimer;
    }

    internal CommandPacketBuilder(byte[] command, byte[] subCommand, IPayloadBuilder payloadBuilder, ushort monitoringTimer)
    {
        if (monitoringTimer > 0 && monitoringTimer < 250)
        {
            throw new ArgumentOutOfRangeException("The allowable timeout values are 0 or 250 ms or greater.");
        }

        this.command = command;
        this.subCommand = subCommand;
        this.payloadBuilder = payloadBuilder;
        this.monitoringTimer = BitConverter.GetBytes((ushort)(monitoringTimer / 250)); // 1 = 250ms 
    }

    [Obsolete]
    internal CommandPacketBuilder(byte[] command, byte[] subCommand, byte[] monitoringTimer)
    {   
        this.command = command;
        this.subCommand = subCommand;
        this.monitoringTimer = monitoringTimer;
    }

    internal CommandPacketBuilder(byte[] command, byte[] subCommand, ushort monitoringTimer)
    {
        if (monitoringTimer > 0 && monitoringTimer < 250)
        {
            throw new ArgumentOutOfRangeException("The allowable timeout values are 0 or 250 ms or greater.");
        }

        this.command = command;
        this.subCommand = subCommand;
        this.monitoringTimer = BitConverter.GetBytes((ushort)(monitoringTimer / 250)); // 1 = 250ms 
    }

    public byte[] ToBinaryBytes()
    {
        var packets = new List<byte>();
        packets.AddRange(monitoringTimer);
        packets.AddRange(command);
        packets.AddRange(subCommand);

        if (payloadBuilder != null) 
        {
            payloadBuilder.AppendPayload(packets, false);
        }

        packets.InsertRange(0, BitConverter.GetBytes((ushort)packets.Count));

        return packets.ToArray();
    }

    public byte[] ToAsciiBytes()
    {
        var packets = new List<byte>();
        packets.AddRange(BinaryBytesToAsciiBytes(monitoringTimer, true));
        packets.AddRange(BinaryBytesToAsciiBytes(command, true));
        packets.AddRange(BinaryBytesToAsciiBytes(subCommand, true));

        if (payloadBuilder != null) 
        {
            payloadBuilder.AppendPayload(packets, true);
        }

        packets.InsertRange(0, BinaryBytesToAsciiBytes(
            BitConverter.GetBytes((ushort)packets.Count), true)
        );

        return packets.ToArray();
    }

    internal static byte[] BinaryBytesToAsciiBytes(byte[] binaryBytes, bool isReverse)
    {
        IEnumerable<byte> seq = isReverse 
            ? System.Linq.Enumerable.Reverse(binaryBytes) 
            : binaryBytes;

        return Encoding.ASCII.GetBytes(
            string.Concat(seq.Select(b => b.ToString("X2")))
        );
    }

    internal static byte[] BinaryBytesToAsciiByte(byte[] binaryBytes, bool isReverse)
    {
        IEnumerable<byte> seq = isReverse 
            ? System.Linq.Enumerable.Reverse(binaryBytes) 
            : binaryBytes;

        return Encoding.ASCII.GetBytes(
            string.Concat(seq.Select(b => b.ToString("X")))
        );
    }
}
