using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Parsers;
using McpXLib.Utils;
using McpXLib.Builders;

namespace McpXLib.Commands;

public sealed class WordRandomReadCommand<T1, T2> : IPlcCommand<(T1[] wordValues, T2[] doubleValues)>
    where T1 : unmanaged
    where T2 : unmanaged
{
    public const ushort MIN_WORD_LENGTH = 1;
    public const ushort MAX_WORD_LENGTH = 192;
    private readonly int wordLength;
    private readonly int doubleWordLength;
    private readonly CommandPacketBuilder commandPacketBuilder;

    public WordRandomReadCommand((Prefix prefix, string address)[] wordDevices, (Prefix prefix, string address)[] doubleWordDevices) : base()
    {
        wordLength = wordDevices.Length;
        doubleWordLength = doubleWordDevices.Length;

        ValidatePramater();

        commandPacketBuilder = new CommandPacketBuilder(
            command: [0x03, 0x04],
            subCommand: [0x00, 0x00],
            payloadBuilder: new DeviceListPayloadBuilder(wordDevices, doubleWordDevices),
            monitoringTimer: [0x00, 0x00]
        );
    }

    public void ValidatePramater()
    {
        var totalLength = wordLength + doubleWordLength;
        if (totalLength < MIN_WORD_LENGTH || totalLength > MAX_WORD_LENGTH)
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

    public async Task<(T1[] wordValues, T2[] doubleValues)> ExecuteAsync(IPlc plc)
    {
        byte[] responseContent;

        if (plc.IsAscii) 
        {
            responseContent = new RandomResponseAsciiPacketParser(
                await plc.RequestAsync(GetPacketBuilder().ToAsciiBytes()),
                wordLength,
                doubleWordLength
            ).Content;
        }
        else 
        {
            responseContent = new ResponsePacketParser(
                await plc.RequestAsync(GetPacketBuilder().ToBinaryBytes())
            ).Content;
        }

        return (
            wordValues: DeviceConverter.ConvertValueArray<T1>(responseContent
                .Take(wordLength * DeviceConverter.GetWordLength<T1>() * 2)
                .ToArray()
            ),
            doubleValues: DeviceConverter.ConvertValueArray<T2>(responseContent
                .Skip(wordLength * DeviceConverter.GetWordLength<T1>() * 2)
                .Take(doubleWordLength * DeviceConverter.GetWordLength<T2>() * 2)
                .ToArray()
            )
        );
    }

    public (T1[] wordValues, T2[] doubleValues) Execute(IPlc plc)
    {
        byte[] responseContent;

        if (plc.IsAscii) 
        {
            responseContent = new RandomResponseAsciiPacketParser(
                plc.Request(GetPacketBuilder().ToAsciiBytes()),
                wordLength,
                doubleWordLength
            ).Content;
        }
        else 
        {
            responseContent = new ResponsePacketParser(
                plc.Request(GetPacketBuilder().ToBinaryBytes())
            ).Content;
        }

        return (
            wordValues: DeviceConverter.ConvertValueArray<T1>(responseContent
                .Take(wordLength * DeviceConverter.GetWordLength<T1>() * 2)
                .ToArray()
            ),
            doubleValues: DeviceConverter.ConvertValueArray<T2>(responseContent
                .Skip(wordLength * DeviceConverter.GetWordLength<T1>() * 2)
                .Take(doubleWordLength * DeviceConverter.GetWordLength<T2>() * 2)
                .ToArray()
            )
        );
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
