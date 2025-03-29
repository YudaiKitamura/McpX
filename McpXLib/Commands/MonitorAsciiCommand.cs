using McpXLib.Abstructs;
using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Helpers;
using McpXLib.Utils;

namespace McpXLib.Commands;

public sealed class MonitorAsciiCommand<T1, T2> : BaseAsciiCommand, IPlcCommand<(T1[] wordValues, T2[] doubleValues)>
    where T1 : unmanaged
    where T2 : unmanaged
{
    public const int MIN_WORD_LENGTH = 1;
    public const int MAX_WORD_LENGTH = 1920;
    public const int WORD_SIZE = 12;
    public const int DOUBLE_WORD_SIZE = 14;
    private readonly int wordLength;
    private readonly int doubleWordLength;

    public MonitorAsciiCommand((Prefix prefix, string address)[] wordDevices, (Prefix prefix, string address)[] doubleWordDevices) : base()
    {
        wordLength = wordDevices.Length;
        doubleWordLength = doubleWordDevices.Length;

        content = new ContentAsciiPacketHelper(
            command: "0802",
            subCommand: "0000",
            commandContent: "",
            monitoringTimer: "0000"
        );
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
