using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Parsers;
using McpXLib.Utils;
using McpXLib.Builders;

namespace McpXLib.Commands;

public sealed class WordBatchWriteCommand<T> : IPlcCommand<bool>
    where T : unmanaged
{
    public const ushort MIN_WORD_LENGTH = 1;
    public const ushort MAX_WORD_LENGTH = 960;
    private readonly ushort wordLength;
    private readonly CommandPacketBuilder commandPacketBuilder;

    public WordBatchWriteCommand(Prefix prefix, string address, T[] values)
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

    public void ValidatePramater()
    {
        if (wordLength < MIN_WORD_LENGTH || wordLength > MAX_WORD_LENGTH)
        {
            throw new ArgumentException($"Word length can be from {MIN_WORD_LENGTH} to {MAX_WORD_LENGTH}.");
        }
    }

    public RequestPacketBuilder GetPacketBuilder()
    {
        return new RequestPacketBuilder(
            subHeaderPacketBuilder: new SubHeaderPacketBuilder(),
            routePacketBuilder: new RoutePacketBuilder(),
            commandPacketBuilder: commandPacketBuilder
        );
    }

    public async Task<bool> ExecuteAsync(IPlc plc)
    {
        if (plc.IsAscii) 
        {
            return new ResponseAsciiPacketParser(
                await plc.RequestAsync(GetPacketBuilder().ToAsciiBytes())
            ).errCode == 0;
        }
        else 
        {
            return new ResponsePacketParser(
                await plc.RequestAsync(GetPacketBuilder().ToBinaryBytes())
            ).errCode == 0;
        }
    }

    public bool Execute(IPlc plc)
    {
        if (plc.IsAscii) 
        {
            return new ResponseAsciiPacketParser(
                plc.Request(GetPacketBuilder().ToAsciiBytes())
            ).errCode == 0;
        }
        else 
        {
            return new ResponsePacketParser(
                plc.Request(GetPacketBuilder().ToBinaryBytes())
            ).errCode == 0;
        }
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
