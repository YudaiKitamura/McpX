using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Utils;
using McpXLib.Builders;

namespace McpXLib.Commands;

internal sealed class WordBatchReadCommand<T> : PlcCommandBase<T[]>
    where T : unmanaged
{
    internal const ushort MIN_WORD_LENGTH = 1;
    internal const ushort MAX_WORD_LENGTH = 960;
    private readonly ushort wordLength;

    internal WordBatchReadCommand(Prefix prefix, string address, ushort wordLength, ushort monitoringTimer = 0, ProcessorSeries series = ProcessorSeries.Q)
    {
        this.wordLength = wordLength;

        ValidateParameters();

        commandPacketBuilder = new CommandPacketBuilder(
            command: [0x01, 0x04],
            subCommand: DeviceConverter.ToSubCommand([0x00, 0x00], series),
            payloadBuilder: new DevicePayloadBuilder(prefix, address, (ushort)(wordLength * DeviceConverter.GetWordLength<T>()), series),
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

    protected override T[] Parse(byte[] responseContent)
    {
        ThrowIfLengthMismatch(responseContent, wordLength * DeviceConverter.GetWordLength<T>() * 2);
        return DeviceConverter.ConvertValueArray<T>(responseContent);
    }
}
