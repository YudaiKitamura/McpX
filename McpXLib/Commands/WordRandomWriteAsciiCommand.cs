using McpXLib.Abstructs;
using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Helpers;
using McpXLib.Utils;

namespace McpXLib.Commands;

public sealed class WordRandomWriteAsciiCommand<T1, T2> : BaseAsciiCommand, IPlcCommand<bool>
    where T1 : unmanaged
    where T2 : unmanaged
{
    public const int MIN_WORD_LENGTH = 1;
    public const int MAX_WORD_LENGTH = 1920;
    public const int WORD_SIZE = 12;
    public const int DOUBLE_WORD_SIZE = 14;
    private readonly int wordLength;
    private readonly int doubleWordLength;

    public WordRandomWriteAsciiCommand((Prefix prefix, string address, T1 value)[] wordDevices, (Prefix prefix, string address, T2 value)[] doubleWordDevices) : base()
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
            commandContent.Add(string.Concat(
                DeviceConverter.ReverseByTwoBytes(
                    DeviceConverter.StructToBytes(wordDevice.value)
                ).Select(x => x.ToString("X2"))
            ));
        }

        foreach (var doubleWordDevice in doubleWordDevices)
        { 
            commandContent.Add(DeviceConverter.ToASCIIAddress(doubleWordDevice.prefix, doubleWordDevice.address));

            var doubleWordDeviceStr = DeviceConverter.ReverseByTwoBytes(
                DeviceConverter.StructToBytes(doubleWordDevice.value)
            ).Select(x => x.ToString("X2")).ToArray();

            doubleWordDeviceStr = Enumerable.Range(0, doubleWordDeviceStr.Length / 2)
                .Select(i => doubleWordDeviceStr.Skip(i * 2).Take(2).ToArray())
                .Reverse()
                .SelectMany(x => x)
                .ToArray();

            commandContent.Add(string.Concat(doubleWordDeviceStr));
        }

        content = new ContentAsciiPacketHelper(
            command: "1402",
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
