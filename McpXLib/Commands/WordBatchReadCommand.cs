using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Utils;
using McpXLib.Builders;

namespace McpXLib.Commands;

internal sealed class WordBatchReadCommand<T> : IPlcCommand<T[]>
    where T : unmanaged
{
    internal const ushort MIN_WORD_LENGTH = 1;
    internal const ushort MAX_WORD_LENGTH = 960;
    private readonly ushort wordLength;
    private readonly CommandPacketBuilder commandPacketBuilder;

    internal WordBatchReadCommand(Prefix prefix, string address, ushort wordLength, ushort monitoringTimer = 0)
    {
        this.wordLength = wordLength;

        ValidatePramater();

        commandPacketBuilder = new CommandPacketBuilder(
            command: [0x01, 0x04],
            subCommand: [0x00, 0x00],
            payloadBuilder: new DevicePayloadBuilder(prefix, address, (ushort)(wordLength * DeviceConverter.GetWordLength<T>())),
            monitoringTimer: monitoringTimer
        );
    }

    internal void ValidatePramater()
    {
        if (wordLength < MIN_WORD_LENGTH || wordLength > MAX_WORD_LENGTH)
        {
            throw new ArgumentException($"Word length can be from {MIN_WORD_LENGTH} to {MAX_WORD_LENGTH}.");
        }
    }

    public async Task<T[]> ExecuteAsync(IPlc plc)
    {
        var requestFrameSelector = new RequestFrameSelector(plc, commandPacketBuilder);
        var responseFrameSelector = new ResponseFrameSelector(
            plc,
            requestFrameSelector.GetSerialNumber(),
            DeviceAccessMode.Word
        );

        var responseContent = responseFrameSelector.ParsePacket(
            await plc.RequestAsync(requestFrameSelector.GetRequestPacket(), responseFrameSelector)
        );

        return DeviceConverter.ConvertValueArray<T>(responseContent);
    }

    public T[] Execute(IPlc plc)
    {
        var requestFrameSelector = new RequestFrameSelector(plc, commandPacketBuilder);
        var responseFrameSelector = new ResponseFrameSelector(
            plc,
            requestFrameSelector.GetSerialNumber(),
            DeviceAccessMode.Word
        );

        var responseContent = responseFrameSelector.ParsePacket(
            plc.Request(requestFrameSelector.GetRequestPacket(), responseFrameSelector)
        );

        return DeviceConverter.ConvertValueArray<T>(responseContent);
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
