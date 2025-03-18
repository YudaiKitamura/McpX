using McpXLib.Abstructs;
using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Helpers;
using McpXLib.Utils;

namespace McpXLib.Commands;

public sealed class MonitorRegistCommand : BaseCommand, IPlcCommand<bool>
{
    public const int MIN_WORD_LENGTH = 1;
    public const int MAX_WORD_LENGTH = 192;
    private readonly int wordLength;
    private readonly int doubleWordLength;

    public MonitorRegistCommand((Prefix, string)[] wordAddresses, (Prefix, string)[] doubleWordAddresses) : base()
    {
        wordLength = wordAddresses.Length;
        doubleWordLength = doubleWordAddresses.Length;

        ValidatePramater();

        var commandContent = new List<byte>();
        commandContent.Add(BitConverter.GetBytes(wordAddresses.Length).First());
        commandContent.Add(BitConverter.GetBytes(doubleWordAddresses.Length).First());

        foreach (var wordAddress in wordAddresses)
        { 
            commandContent.AddRange(DeviceConverter.ToByteAddress(wordAddress.Item1, wordAddress.Item2));
        }

        foreach (var doubleWordAddress in doubleWordAddresses)
        { 
            commandContent.AddRange(DeviceConverter.ToByteAddress(doubleWordAddress.Item1, doubleWordAddress.Item2));
        }

        content = new ContentPacketHelper(
            command: [0x01, 0x08],
            subCommand: [0x00, 0x00],
            commandContent: commandContent.ToArray(),
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

    public async Task<bool> ExecuteAsync(Interfaces.IPlc plc)
    {
        Route = plc.Route;
        return new ResponsePacketHelper(
            await plc.RequestAsync(ToBytes())
        ).errCode == 0;
    }

    public bool Execute(Interfaces.IPlc plc)
    {
        Route = plc.Route;
        return new ResponsePacketHelper(
            plc.Request(ToBytes())
        ).errCode == 0;
    }
}
