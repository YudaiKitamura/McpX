using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Builders;
using McpXLib.Utils;

namespace McpXLib.Commands;

internal sealed class BitRandomWriteCommand : IPlcCommand<bool>
{
    internal const ushort MIN_BIT_LENGTH = 1;
    internal const ushort MAX_BIT_LENGTH = 188;
    private readonly int bitLength;
    private readonly ProcessorSeries series;
    private readonly CommandPacketBuilder commandPacketBuilder;

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

    public async Task<bool> ExecuteAsync(IPlc plc)
    {
        var requestFrameSelector = new RequestFrameSelector(plc, commandPacketBuilder);
        var responseFrameSelector = new ResponseFrameSelector(
            plc,
            requestFrameSelector.GetSerialNumber(),
            DeviceAccessMode.Bit
        );

        responseFrameSelector.ParsePacket(
            await plc.RequestAsync(requestFrameSelector.GetRequestPacket(), responseFrameSelector)
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
            plc.Request(requestFrameSelector.GetRequestPacket(), responseFrameSelector)
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
