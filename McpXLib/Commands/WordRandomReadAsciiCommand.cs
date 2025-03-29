using McpXLib.Abstructs;
using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Helpers;
using McpXLib.Utils;

namespace McpXLib.Commands;

public sealed class WordRandomReadAsciiCommand<T1, T2> : BaseAsciiCommand, IPlcCommand<(T1[] wordValues, T2[] doubleValues)>
    where T1 : unmanaged
    where T2 : unmanaged
{
    public const ushort MIN_WORD_LENGTH = 1;
    public const ushort MAX_WORD_LENGTH = 192;
    private readonly int wordLength;
    private readonly int doubleWordLength;

    public WordRandomReadAsciiCommand((Prefix prefix, string address)[] wordAddresses, (Prefix prefix, string address)[] doubleWordAddresses) : base()
    {
        wordLength = wordAddresses.Length;
        doubleWordLength = doubleWordAddresses.Length;

        ValidatePramater();

        var commandContent = new List<string>();
        commandContent.Add(wordLength.ToString("X").PadLeft(2, '0'));
        commandContent.Add(doubleWordLength.ToString("X").PadLeft(2, '0'));

        foreach (var wordAddress in wordAddresses)
        { 
            commandContent.Add(DeviceConverter.ToASCIIAddress(wordAddress.prefix, wordAddress.address));
        }

        foreach (var doubleWordAddress in doubleWordAddresses)
        { 
            commandContent.Add(DeviceConverter.ToASCIIAddress(doubleWordAddress.prefix, doubleWordAddress.address));
        }

        content = new ContentAsciiPacketHelper(
            command: "0403",
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

    public async Task<(T1[] wordValues, T2[] doubleValues)> ExecuteAsync(IPlc plc)
    {
        Route = plc.Route;
        var response = new RandomResponseAsciiPacketHelper(
            await plc.RequestAsync(ToBytes()),
            wordLength,
            doubleWordLength
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

    public (T1[] wordValues, T2[] doubleValues) Execute(IPlc plc)
    {
        Route = plc.Route;
        var response = new RandomResponseAsciiPacketHelper(
            plc.Request(ToBytes()),
            wordLength,
            doubleWordLength
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
