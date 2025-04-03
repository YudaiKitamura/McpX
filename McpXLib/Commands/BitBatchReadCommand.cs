using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Helpers;
using McpXLib.Utils;
using McpXLib.Builders;

namespace McpXLib.Commands;

public sealed class BitBatchReadCommand : IPlcCommand<bool[]>
{
    public const ushort MIN_BIT_LENGTH = 1;
    public const ushort MAX_BIT_LENGTH = 7168;
    private readonly ushort bitLength;
    private readonly CommandPacketBuilder commandPacketBuilder;

    public BitBatchReadCommand(Prefix prefix, string address, ushort bitLength) : base()
    {
        this.bitLength = bitLength;

        ValidatePramater();

        commandPacketBuilder = new CommandPacketBuilder(
            command: [0x01, 0x04],
            subCommand: [0x01, 0x00],
            payloadBuilder: new DevicePayloadBuilder(prefix, address, bitLength),
            monitoringTimer: [0x00, 0x00]
        );
    }

    public void ValidatePramater()
    {
        if (bitLength < MIN_BIT_LENGTH || bitLength > MAX_BIT_LENGTH)
        {
            throw new ArgumentException($"Bit length can be from {MIN_BIT_LENGTH} to {MAX_BIT_LENGTH}.");
        }
    }

    public RequestPacketBuilder GetPacketBuilder()
    {
        return new RequestPacketBuilder(
            subHeaderPacketBuilder: new SubHeaderPacketBuilder(),
            routePacketBuilder: new RoutePacketBuilder(),
            commandPacketBuilder: commandPacketBuilder
        );
    }

    public async Task<bool[]> ExecuteAsync(IPlc plc)
    {
        byte[] responseContent;

        if (plc.IsAscii) 
        {
            responseContent = new BitResponseAsciiPacketHelper(
                await plc.RequestAsync(GetPacketBuilder().ToAsciiBytes())
            ).Content;
        }
        else 
        {
            responseContent = new BitResponsePacketHelper(
                await plc.RequestAsync(GetPacketBuilder().ToBinaryBytes())
            ).Content;
        }

        return DeviceConverter.ConvertValueArray<bool>(responseContent);
    }

    public bool[] Execute(IPlc plc)
    {
        byte[] responseContent;

        if (plc.IsAscii) 
        {
            responseContent = new BitResponseAsciiPacketHelper(
                plc.Request(GetPacketBuilder().ToAsciiBytes())
            ).Content;
        }
        else 
        {
            responseContent = new BitResponsePacketHelper(
                plc.Request(GetPacketBuilder().ToBinaryBytes())
            ).Content;
        }

        return DeviceConverter.ConvertValueArray<bool>(responseContent);
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
