using McpXLib.Abstructs;
using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Helpers;
using McpXLib.Utils;

namespace McpXLib.Commands;

public sealed class WordRandomReadCommand<T1, T2> : BaseCommand, IPlcCommand<(T1[] wordValues, T2[] doubleValues)>
    where T1 : unmanaged
    where T2 : unmanaged
{
    public const ushort MIN_WORD_LENGTH = 1;
    public const ushort MAX_WORD_LENGTH = 192;
    private readonly int wordLength;
    private readonly int doubleWordLength;

    public WordRandomReadCommand((Prefix, string)[] wordAddresses, (Prefix, string)[] doubleWordAddresses) : base()
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
            command: [0x03, 0x04],
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

    public async Task<(T1[] wordValues, T2[] doubleValues)> ExecuteAsync(Interfaces.IPlc plc)
    {
        Route = plc.Route;
        var response = new ResponsePacketHelper(
            await plc.RequestAsync(ToBytes())
        );

        return (
            wordValues: DeviceConverter.ConvertValueArray<T1>(response.Content
                .Take(wordLength * DeviceConverter.GetWordLength<T1>() * 2)
                .ToArray()
            ),
            doubleValues: DeviceConverter.ConvertValueArray<T2>(response.Content
                .Skip(wordLength * DeviceConverter.GetWordLength<T1>() * 2)
                .Take(doubleWordLength * DeviceConverter.GetWordLength<T2>() * 2)
                .ToArray()
            )
        );
    }

    public (T1[] wordValues, T2[] doubleValues) Execute(Interfaces.IPlc plc)
    {
        Route = plc.Route;
        var response = new ResponsePacketHelper(
            plc.Request(ToBytes())
        );

        return (
            wordValues: DeviceConverter.ConvertValueArray<T1>(response.Content
                .Take(wordLength * DeviceConverter.GetWordLength<T1>() * 2)
                .ToArray()
            ),
            doubleValues: DeviceConverter.ConvertValueArray<T2>(response.Content
                .Skip(wordLength * DeviceConverter.GetWordLength<T1>() * 2)
                .Take(doubleWordLength * DeviceConverter.GetWordLength<T2>() * 2)
                .ToArray()
            )
        );
    }
}
