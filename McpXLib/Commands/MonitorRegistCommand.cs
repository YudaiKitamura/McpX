using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Helpers;
using McpXLib.Builders;

namespace McpXLib.Commands;

public sealed class MonitorRegistCommand : IPlcCommand<bool>
{
    public const int MIN_WORD_LENGTH = 1;
    public const int MAX_WORD_LENGTH = 192;
    private readonly int wordLength;
    private readonly int doubleWordLength;
    private readonly CommandPacketBuilder commandPacketBuilder;


    public MonitorRegistCommand((Prefix, string)[] wordDevices, (Prefix, string)[] doubleWordDevices) : base()
    {
        wordLength = wordDevices.Length;
        doubleWordLength = doubleWordDevices.Length;

        ValidatePramater();

        commandPacketBuilder = new CommandPacketBuilder(
            command: [0x01, 0x08],
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

    public async Task<bool> ExecuteAsync(IPlc plc)
    {
        if (plc.IsAscii) 
        {
            return new ResponseAsciiPacketHelper(
                await plc.RequestAsync(GetPacketBuilder().ToAsciiBytes())
            ).errCode == 0;
        }
        else 
        {
            return new ResponsePacketHelper(
                await plc.RequestAsync(GetPacketBuilder().ToBinaryBytes())
            ).errCode == 0;
        }
    }

    public bool Execute(IPlc plc)
    {
        if (plc.IsAscii) 
        {
            return new ResponseAsciiPacketHelper(
                plc.Request(GetPacketBuilder().ToAsciiBytes())
            ).errCode == 0;
        }
        else 
        {
            return new ResponsePacketHelper(
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
