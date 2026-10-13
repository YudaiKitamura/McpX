using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Builders;
using McpXLib.Utils;

namespace McpXLib.Commands;

internal sealed class BitBatchWriteCommand : PlcCommandBase<bool>
{
    internal const ushort MIN_BIT_LENGTH = 1;
    internal const ushort MAX_BIT_LENGTH = 7168;
    private readonly ushort bitLength;

    internal BitBatchWriteCommand(Prefix prefix, string address, bool[] values, ushort monitoringTimer = 0, ProcessorSeries series = ProcessorSeries.Q) : base()
    {
        bitLength = (ushort)values.Length;

        ValidatePramater();

        commandPacketBuilder = new CommandPacketBuilder(
            command: [0x01, 0x14],
            subCommand: DeviceConverter.ToSubCommand([0x01, 0x00], series),
            payloadBuilder: new BitDeviceValuePayloadBuilder(prefix, address, values, series),
            monitoringTimer: monitoringTimer
        );
    }

    internal void ValidatePramater()
    {
        if (bitLength < MIN_BIT_LENGTH || bitLength > MAX_BIT_LENGTH)
        {
            throw new ArgumentException($"Bit length can be from {MIN_BIT_LENGTH} to {MAX_BIT_LENGTH}.");
        }
    }

    protected override ResponseFrameSelector CreateResponseSelector(IPlc plc, ushort serialNumber)
        => new(plc, serialNumber, DeviceAccessMode.Bit);

    protected override bool Parse(byte[] responseContent)
        => true;
}
