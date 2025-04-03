using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Parsers;
using McpXLib.Builders;

namespace McpXLib.Commands;

public sealed class WordRandomWriteCommand<T1, T2> : IPlcCommand<bool>
    where T1 : unmanaged
    where T2 : unmanaged
{
    public const int MIN_WORD_LENGTH = 1;
    public const int MAX_WORD_LENGTH = 1920;
    public const int WORD_SIZE = 12;
    public const int DOUBLE_WORD_SIZE = 14;
    private readonly int wordLength;
    private readonly int doubleWordLength;
        private readonly CommandPacketBuilder commandPacketBuilder;

    public WordRandomWriteCommand((Prefix prefix, string address, T1 value)[] wordDevices, (Prefix prefix, string address, T2 value)[] doubleWordDevices)
    {
        wordLength = wordDevices.Length * WORD_SIZE;
        doubleWordLength = doubleWordDevices.Length * DOUBLE_WORD_SIZE;

        ValidatePramater();

        commandPacketBuilder = new CommandPacketBuilder(
            command: [0x02, 0x14],
            subCommand: [0x00, 0x00],
            payloadBuilder: new DeviceValueListPayloadBuilder<T1, T2>(wordDevices, doubleWordDevices),
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
