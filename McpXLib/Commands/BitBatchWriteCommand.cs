using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Builders;
using McpXLib.Utils;

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

    public void ValidatePramater()
    {
        if (bitLength < MIN_BIT_LENGTH || bitLength > MAX_BIT_LENGTH)
        {
            throw new ArgumentException($"Bit length can be from {MIN_BIT_LENGTH} to {MAX_BIT_LENGTH}.");
        }
    }

    public async Task<bool> ExecuteAsync(IPlc plc)
    {
        var requestFrameSelector = new RequestFrameSelector(plc, commandPacketBuilder);
        var responseFrameSelector = new ResponseFrameSelector(
            plc,
            requestFrameSelector.GetSerialNumber(),
            DeviceAccessMode.Bit
        );

        responseFrameSelector.ParsePacket(
            await plc.RequestAsync(requestFrameSelector.GetRequestPacket())
        );

        return true;
    }

    public bool Execute(IPlc plc)
    {
        var requestFrameSelector = new RequestFrameSelector(plc, commandPacketBuilder);
        var responseFrameSelector = new ResponseFrameSelector(
            plc,
            requestFrameSelector.GetSerialNumber(),
            DeviceAccessMode.Bit
        );

        responseFrameSelector.ParsePacket(
            plc.Request(requestFrameSelector.GetRequestPacket())
        );

        return true;
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
