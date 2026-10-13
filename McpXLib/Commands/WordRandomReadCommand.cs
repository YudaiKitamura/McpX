using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Utils;
using McpXLib.Builders;

namespace McpXLib.Commands;

internal sealed class WordRandomReadCommand<T1, T2> : PlcCommandBase<(T1[] wordValues, T2[] doubleValues)>
    where T1 : unmanaged
    where T2 : unmanaged
{
    internal const ushort MIN_WORD_LENGTH = 1;
    internal const ushort MAX_WORD_LENGTH = 192;
    private readonly int wordLength;
    private readonly int doubleWordLength;
    private readonly ProcessorSeries series;

    internal static ushort GetMaxWordLength(ProcessorSeries series)
        => (ushort)(series == ProcessorSeries.iQR ? 96 : MAX_WORD_LENGTH);

    internal WordRandomReadCommand((Prefix prefix, string address)[] wordDevices, (Prefix prefix, string address)[] doubleWordDevices, ushort monitoringTimer = 0, ProcessorSeries series = ProcessorSeries.Q) : base()
    {
        wordLength = wordDevices.Length;
        doubleWordLength = doubleWordDevices.Length;
        this.series = series;

        ValidatePramater();

        commandPacketBuilder = new CommandPacketBuilder(
            command: [0x03, 0x04],
            subCommand: DeviceConverter.ToSubCommand([0x00, 0x00], series),
            payloadBuilder: new DeviceListPayloadBuilder(wordDevices, doubleWordDevices, series),
            monitoringTimer: monitoringTimer
        );
    }

    internal void ValidatePramater()
    {
        var maxWordLength = GetMaxWordLength(series);
        var totalLength = wordLength + doubleWordLength;
        if (totalLength < MIN_WORD_LENGTH || totalLength > maxWordLength)
        {
            throw new ArgumentException($"Word length can be from {MIN_WORD_LENGTH} to {maxWordLength}.");
        }
    }

    protected override ResponseFrameSelector CreateResponseSelector(IPlc plc, ushort serialNumber)
        => new(plc, serialNumber, wordLength: wordLength, doubleWordLength: doubleWordLength);

    protected override (T1[] wordValues, T2[] doubleValues) Parse(byte[] responseContent)
    {
        return SplitWordAndDoubleWord<T1, T2>(responseContent, wordLength, doubleWordLength);
    }
}
