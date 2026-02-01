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
    private readonly CommandPacketBuilder commandPacketBuilder;

    internal MonitorRegistCommand((Prefix, string)[] wordDevices, (Prefix, string)[] doubleWordDevices, ushort monitoringTimer) : base()
    {
        wordLength = wordDevices.Length;
        doubleWordLength = doubleWordDevices.Length;

        ValidatePramater();

        commandPacketBuilder = new CommandPacketBuilder(
            command: [0x01, 0x08],
            subCommand: [0x00, 0x00],
            payloadBuilder: new DeviceListPayloadBuilder(wordDevices, doubleWordDevices),
            monitoringTimer: monitoringTimer
        );
    }

    internal void ValidatePramater()
    {
        var totalLength = wordLength + doubleWordLength;
        if (totalLength < MIN_WORD_LENGTH || totalLength > MAX_WORD_LENGTH)
        {
            throw new ArgumentException($"Word length can be from {MIN_WORD_LENGTH} to {MAX_WORD_LENGTH}.");
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
