using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Builders;
using McpXLib.Utils;

namespace McpXLib.Commands;

internal sealed class BitRandomWriteCommand : PlcCommandBase<bool>
{
    internal const ushort MIN_BIT_LENGTH = 1;
    internal const ushort MAX_BIT_LENGTH = 188;
    private readonly int bitLength;
    private readonly ProcessorSeries series;

    internal static ushort GetMaxBitLength(ProcessorSeries series)
        => (ushort)(series == ProcessorSeries.iQR ? 94 : MAX_BIT_LENGTH);

    internal BitRandomWriteCommand((Prefix prefix, string address, bool value)[] bitDevices, ushort monitoringTimer = 0, ProcessorSeries series = ProcessorSeries.Q)
    {
        bitLength = bitDevices.Length;
        this.series = series;

        ValidatePramater();

        commandPacketBuilder = new CommandPacketBuilder(
            command: [0x02, 0x14],
            subCommand: DeviceConverter.ToSubCommand([0x01, 0x00], series),
            payloadBuilder: new BitDeviceValueListPayloadBuilder(bitDevices, series),
            monitoringTimer: monitoringTimer
        );
    }

    internal void ValidatePramater()
    {
        var maxBitLength = GetMaxBitLength(series);
        if (bitLength < MIN_BIT_LENGTH || bitLength > maxBitLength)
        {
            throw new ArgumentException($"Bit length can be from {MIN_BIT_LENGTH} to {maxBitLength}.");
        }
    }

    protected override ResponseFrameSelector CreateResponseSelector(IPlc plc, ushort serialNumber)
        => new(plc, serialNumber, DeviceAccessMode.Bit);

    protected override bool Parse(byte[] responseContent)
        => true;
}
