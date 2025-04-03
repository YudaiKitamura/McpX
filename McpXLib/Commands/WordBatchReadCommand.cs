using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Parsers;
using McpXLib.Utils;
using McpXLib.Builders;

namespace McpXLib.Commands;

public sealed class WordBatchReadCommand<T> : IPlcCommand<T[]>
    where T : unmanaged
{
    public const ushort MIN_WORD_LENGTH = 1;
    public const ushort MAX_WORD_LENGTH = 960;
    private readonly ushort wordLength;
    private readonly CommandPacketBuilder commandPacketBuilder;

    public WordBatchReadCommand(Prefix prefix, string address, ushort wordLength)
    {
        this.wordLength = wordLength;

        ValidatePramater();

        commandPacketBuilder = new CommandPacketBuilder(
            command: [0x01, 0x04],
            subCommand: [0x00, 0x00],
            payloadBuilder: new DevicePayloadBuilder(prefix, address, (ushort)(wordLength * DeviceConverter.GetWordLength<T>())),
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

    public async Task<T[]> ExecuteAsync(IPlc plc)
    {
        byte[] responseContent;

        if (plc.IsAscii) 
        {
            responseContent = new ResponseAsciiPacketParser(
                await plc.RequestAsync(GetPacketBuilder().ToAsciiBytes())
            ).Content;
        }
        else 
        {
            responseContent = new ResponsePacketParser(
                await plc.RequestAsync(GetPacketBuilder().ToBinaryBytes())
            ).Content;
        }

        return DeviceConverter.ConvertValueArray<T>(responseContent);
    }

    public T[] Execute(IPlc plc)
    {
        byte[] responseContent;

        if (plc.IsAscii) 
        {
            responseContent = new ResponseAsciiPacketParser(
                plc.Request(GetPacketBuilder().ToAsciiBytes())
            ).Content;
        }
        else 
        {
            responseContent = new ResponsePacketParser(
                plc.Request(GetPacketBuilder().ToBinaryBytes())
            ).Content;
        }

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
