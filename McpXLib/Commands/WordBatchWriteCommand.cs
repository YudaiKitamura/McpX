using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Utils;
using McpXLib.Builders;

namespace McpXLib.Commands;

internal sealed class WordBatchWriteCommand<T> : PlcCommandBase<bool>
    where T : unmanaged
{
    internal const ushort MIN_WORD_LENGTH = 1;
    internal const ushort MAX_WORD_LENGTH = 960;
    private readonly ushort wordLength;

    internal WordBatchWriteCommand(Prefix prefix, string address, T[] values, ushort monitoringTimer = 0, ProcessorSeries series = ProcessorSeries.Q)
    {
        wordLength = (ushort)(values.Length * DeviceConverter.GetWordLength<T>());

        ValidateParameters();

        commandPacketBuilder = new CommandPacketBuilder(
            command: [0x01, 0x14],
            subCommand: DeviceConverter.ToSubCommand([0x00, 0x00], series),
            payloadBuilder: new DeviceValuePayloadBuilder<T>(prefix, address, values, series),
            monitoringTimer: monitoringTimer
        );
    }

    internal void ValidateParameters()
    {
        if (wordLength < MIN_WORD_LENGTH || wordLength > MAX_WORD_LENGTH)
        {
            throw new ArgumentException($"Word length can be from {MIN_WORD_LENGTH} to {MAX_WORD_LENGTH}.");
        }
    }

    protected override ResponseFrameSelector CreateResponseSelector(IPlc plc, ushort serialNumber)
        => new(plc, serialNumber, DeviceAccessMode.Word);

    protected override bool Parse(byte[] responseContent)
        => true;
}
