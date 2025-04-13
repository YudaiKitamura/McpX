using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Utils;
using McpXLib.Builders;

namespace McpXLib.Commands;

internal sealed class WordBatchWriteCommand<T> : IPlcCommand<bool>
    where T : unmanaged
{
    internal const ushort MIN_WORD_LENGTH = 1;
    internal const ushort MAX_WORD_LENGTH = 960;
    private readonly ushort wordLength;
    private readonly CommandPacketBuilder commandPacketBuilder;

    internal WordBatchWriteCommand(Prefix prefix, string address, T[] values)
    {
        wordLength = (ushort)(values.Length * DeviceConverter.GetWordLength<T>());

        ValidatePramater();

        commandPacketBuilder = new CommandPacketBuilder(
            command: [0x01, 0x14],
            subCommand: [0x00, 0x00],
            payloadBuilder: new DeviceValuePayloadBuilder<T>(prefix, address, values),
            monitoringTimer: [0x00, 0x00]
        );
    }

    internal void ValidatePramater()
    {
        if (wordLength < MIN_WORD_LENGTH || wordLength > MAX_WORD_LENGTH)
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
            DeviceAccessMode.Word
        );

        responseFrameSelector.ParsePacket(
            await plc.RequestAsync(requestFrameSelector.GetRequestPacket())
        );

        return true;
    }

    public bool Execute(IPlc plc)
    {
        var requestFrameSelector = new RequestFrameSelector(plc, commandPacketBuilder);
        var responseFrameSelector = new ResponseFrameSelector(
            plc,
            requestFrameSelector.GetSerialNumber(),
            DeviceAccessMode.Word
        );

        responseFrameSelector.ParsePacket(
            plc.Request(requestFrameSelector.GetRequestPacket())
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
