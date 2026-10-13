using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Builders;
using McpXLib.Utils;

namespace McpXLib.Commands;

internal sealed class WordRandomWriteCommand<T1, T2> : PlcCommandBase<bool>
    where T1 : unmanaged
    where T2 : unmanaged
{
    internal const int MIN_WORD_LENGTH = 1;
    internal const int MAX_WORD_LENGTH = 1920;
    internal const int WORD_SIZE = 12;
    internal const int DOUBLE_WORD_SIZE = 14;
    private readonly int wordLength;
    private readonly int doubleWordLength;
    private readonly ProcessorSeries series;
    
    internal static int GetMaxWordLength(ProcessorSeries series)
        => series == ProcessorSeries.iQR ? 960 : MAX_WORD_LENGTH;

    internal WordRandomWriteCommand((Prefix prefix, string address, T1 value)[] wordDevices, (Prefix prefix, string address, T2 value)[] doubleWordDevices, ushort monitoringTimer = 0, ProcessorSeries series = ProcessorSeries.Q)
    {
        wordLength = wordDevices.Length * WORD_SIZE;
        doubleWordLength = doubleWordDevices.Length * DOUBLE_WORD_SIZE;
        this.series = series;

        ValidatePramater();

        commandPacketBuilder = new CommandPacketBuilder(
            command: [0x02, 0x14],
            subCommand: DeviceConverter.ToSubCommand([0x00, 0x00], series),
            payloadBuilder: new DeviceValueListPayloadBuilder<T1, T2>(wordDevices, doubleWordDevices, series),
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

    protected override bool Parse(byte[] responseContent)
        => true;
}
