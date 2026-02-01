using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Builders;
using McpXLib.Utils;

namespace McpXLib.Commands;

internal sealed class WordRandomWriteCommand<T1, T2> : IPlcCommand<bool>
    where T1 : unmanaged
    where T2 : unmanaged
{
    internal const int MIN_WORD_LENGTH = 1;
    internal const int MAX_WORD_LENGTH = 1920;
    internal const int WORD_SIZE = 12;
    internal const int DOUBLE_WORD_SIZE = 14;
    private readonly int wordLength;
    private readonly int doubleWordLength;
        private readonly CommandPacketBuilder commandPacketBuilder;

    internal WordRandomWriteCommand((Prefix prefix, string address, T1 value)[] wordDevices, (Prefix prefix, string address, T2 value)[] doubleWordDevices, ushort monitoringTimer = 0)
    {
        wordLength = wordDevices.Length * WORD_SIZE;
        doubleWordLength = doubleWordDevices.Length * DOUBLE_WORD_SIZE;

        ValidatePramater();

        commandPacketBuilder = new CommandPacketBuilder(
            command: [0x02, 0x14],
            subCommand: [0x00, 0x00],
            payloadBuilder: new DeviceValueListPayloadBuilder<T1, T2>(wordDevices, doubleWordDevices),
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
