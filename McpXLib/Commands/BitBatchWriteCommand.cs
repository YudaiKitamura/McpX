using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Parsers;
using McpXLib.Builders;

namespace McpXLib.Commands;

public sealed class BitBatchWriteCommand : IPlcCommand<bool>
{
    public const ushort MIN_BIT_LENGTH = 1;
    public const ushort MAX_BIT_LENGTH = 7168;
    private readonly ushort bitLength;
    private readonly CommandPacketBuilder commandPacketBuilder;

    public BitBatchWriteCommand(Prefix prefix, string address, bool[] values) : base()
    {
        bitLength = (ushort)values.Length;

        ValidatePramater();

        commandPacketBuilder = new CommandPacketBuilder(
            command: [0x01, 0x14],
            subCommand: [0x01, 0x00],
            payloadBuilder: new BitDeviceValuePayloadBuilder(prefix, address, values),
            monitoringTimer: [0x00, 0x00]
        );
    }

    public RequestPacketBuilder GetPacketBuilder()
    {
        return new RequestPacketBuilder(
            subHeaderPacketBuilder: new SubHeaderPacketBuilder(),
            routePacketBuilder: new RoutePacketBuilder(),
            commandPacketBuilder: commandPacketBuilder
        );
    }

    public void ValidatePramater()
    {
        if (bitLength < MIN_BIT_LENGTH || bitLength > MAX_BIT_LENGTH)
        {
            throw new ArgumentException($"Bit length can be from {MIN_BIT_LENGTH} to {MAX_BIT_LENGTH}.");
        }
    }

    public async Task<bool> ExecuteAsync(IPlc plc)
    {
        if (plc.IsAscii) 
        {
            return new ResponseAsciiPacketParser(
                await plc.RequestAsync(GetPacketBuilder().ToAsciiBytes())
            ).errCode == 0;
        }
        else 
        {
            return new ResponsePacketParser(
                await plc.RequestAsync(GetPacketBuilder().ToBinaryBytes())
            ).errCode == 0;
        }
    }

    public bool Execute(IPlc plc)
    {
        if (plc.IsAscii) 
        {
            return new ResponseAsciiPacketParser(
                plc.Request(GetPacketBuilder().ToAsciiBytes())
            ).errCode == 0;
        }
        else 
        {
            return new ResponsePacketParser(
                plc.Request(GetPacketBuilder().ToBinaryBytes())
            ).errCode == 0;
        }
    }

    public byte[] ToBinaryBytes()
    {
        return commandPacketBuilder.ToBinaryBytes();
    }

    public byte[] ToAsciiBytes()
    {
        return commandPacketBuilder.ToAsciiBytes();
    }
}
