using McpXLib.Abstructs;
using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Helpers;
using McpXLib.Utils;

namespace McpXLib.Commands;

public sealed class MonitorRegistAsciiCommand: BaseAsciiCommand, IPlcCommand<bool>
{
    public const int MIN_WORD_LENGTH = 1;
    public const int MAX_WORD_LENGTH = 1920;
    public const int WORD_SIZE = 12;
    public const int DOUBLE_WORD_SIZE = 14;
    private readonly int wordLength;
    private readonly int doubleWordLength;

    public MonitorRegistAsciiCommand((Prefix prefix, string address)[] wordDevices, (Prefix prefix, string address)[] doubleWordDevices) : base()
    {
        wordLength = wordDevices.Length * WORD_SIZE;
        doubleWordLength = doubleWordDevices.Length * DOUBLE_WORD_SIZE;

        ValidatePramater();

        var commandContent = new List<string>();
        commandContent.Add(wordDevices.Length.ToString("X").PadLeft(2, '0'));
        commandContent.Add(doubleWordDevices.Length.ToString("X").PadLeft(2, '0'));

        foreach (var wordDevice in wordDevices)
        { 
            commandContent.Add(DeviceConverter.ToASCIIAddress(wordDevice.prefix, wordDevice.address));
        }

        foreach (var doubleWordDevice in doubleWordDevices)
        { 
            commandContent.Add(DeviceConverter.ToASCIIAddress(doubleWordDevice.prefix, doubleWordDevice.address));
        }

        content = new ContentAsciiPacketHelper(
            command: "0801",
            subCommand: "0000",
            commandContent: string.Concat(commandContent.Select(x => x.ToString())),
            monitoringTimer: "0000"
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

    public async Task<bool> ExecuteAsync(IPlc plc)
    {
        Route = plc.Route;
        return new ResponseAsciiPacketHelper(
            await plc.RequestAsync(ToBytes())
        ).errCode == 0;
    }

    public bool Execute(IPlc plc)
    {
        Route = plc.Route;
        return new ResponseAsciiPacketHelper(
            plc.Request(ToBytes())
        ).errCode == 0;
    }
}
