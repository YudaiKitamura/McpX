using System.Text;
using McpXLib.Interfaces;

namespace McpXLib.Builders;

internal class CommandPacketBuilder : IPacketBuilder
{
    private readonly byte[] command;
    private readonly byte[] subCommand;
    private readonly IPayloadBuilder? payloadBuilder;
    private readonly byte[] monitoringTimer;

    internal CommandPacketBuilder(byte[] command, byte[] subCommand, IPayloadBuilder payloadBuilder, byte[] monitoringTimer)
    {   
        this.command = command;
        this.subCommand = subCommand;
        this.payloadBuilder = payloadBuilder;
        this.monitoringTimer = monitoringTimer;
    }

    internal CommandPacketBuilder(byte[] command, byte[] subCommand, byte[] monitoringTimer)
    {   
        this.command = command;
        this.subCommand = subCommand;
        this.monitoringTimer = monitoringTimer;
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
        return Encoding.ASCII.GetBytes(
            string.Concat(
                isReverse ? binaryBytes.Reverse().Select(b => b.ToString("X2")) : binaryBytes.Select(b => b.ToString("X2"))
            )  
        );
    }

    internal static byte[] BinaryBytesToAsciiByte(byte[] binaryBytes, bool isReverse)
    {
        return Encoding.ASCII.GetBytes(
            string.Concat(
                isReverse ? binaryBytes.Reverse().Select(b => b.ToString("X")) : binaryBytes.Select(b => b.ToString("X"))
            )  
        );
    }
}
