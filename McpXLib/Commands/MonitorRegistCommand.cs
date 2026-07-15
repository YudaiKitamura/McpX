using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Builders;
using McpXLib.Utils;

namespace McpXLib.Commands;

internal sealed class MonitorRegistCommand : IPlcCommand<bool>
{
    internal const int MIN_WORD_LENGTH = 1;
    internal const int MAX_WORD_LENGTH = 192;
    private readonly int wordLength;
    private readonly int doubleWordLength;
    private readonly ProcessorSeries series;
    private readonly CommandPacketBuilder commandPacketBuilder;

    // モニタ登録(0801)の点数上限はワード単位のランダム読出し(0403)と同一（マニュアル p.118）。
    internal static int GetMaxWordLength(ProcessorSeries series)
        => series == ProcessorSeries.iQR ? 96 : MAX_WORD_LENGTH;

    internal MonitorRegistCommand((Prefix, string)[] wordDevices, (Prefix, string)[] doubleWordDevices, ushort monitoringTimer = 0, ProcessorSeries series = ProcessorSeries.Q) : base()
    {
        wordLength = wordDevices.Length;
        doubleWordLength = doubleWordDevices.Length;
        this.series = series;

        ValidatePramater();

        commandPacketBuilder = new CommandPacketBuilder(
            command: [0x01, 0x08],
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

    public async Task<bool> ExecuteAsync(IPlc plc)
    {
        var requestFrameSelector = new RequestFrameSelector(plc, commandPacketBuilder);
        var responseFrameSelector = new ResponseFrameSelector(
            plc,
            requestFrameSelector.GetSerialNumber(),
            wordLength: wordLength,
            doubleWordLength: doubleWordLength
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
            wordLength: wordLength,
            doubleWordLength: doubleWordLength
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
