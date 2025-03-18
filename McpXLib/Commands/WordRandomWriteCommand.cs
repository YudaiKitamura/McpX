using McpXLib.Abstructs;
using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Helpers;
using McpXLib.Utils;

namespace McpXLib.Commands;

public sealed class WordRandomWriteCommand<T1, T2> : BaseCommand, IPlcCommand<bool>
    where T1 : unmanaged
    where T2 : unmanaged
{
    public const int MIN_WORD_LENGTH = 1;
    public const int MAX_WORD_LENGTH = 1920;
    public const int WORD_SIZE = 12;
    public const int DOUBLE_WORD_SIZE = 14;
    private readonly int wordLength;
    private readonly int doubleWordLength;

    public WordRandomWriteCommand((Prefix prefix, string address, T1 value)[] wordDevices, (Prefix prefix, string address, T2 value)[] doubleWordDevices) : base()
    {
        wordLength = wordDevices.Length * WORD_SIZE;
        doubleWordLength = doubleWordDevices.Length * DOUBLE_WORD_SIZE;

        ValidatePramater();

        var commandContent = new List<byte>();
        commandContent.Add(BitConverter.GetBytes(wordDevices.Length).First());
        commandContent.Add(BitConverter.GetBytes(doubleWordDevices.Length).First());

        foreach (var wordDevice in wordDevices)
        { 
            commandContent.AddRange(DeviceConverter.ToByteAddress(wordDevice.prefix, wordDevice.address));
            commandContent.AddRange(DeviceConverter.StructToBytes(wordDevice.value));
        }

        foreach (var doubleWordDevice in doubleWordDevices)
        { 
            commandContent.AddRange(DeviceConverter.ToByteAddress(doubleWordDevice.prefix, doubleWordDevice.address));
            commandContent.AddRange(DeviceConverter.StructToBytes(doubleWordDevice.value));
        }

        content = new ContentPacketHelper(
            command: [0x02, 0x14],
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
