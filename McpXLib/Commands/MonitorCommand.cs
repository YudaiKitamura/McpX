using McpXLib.Abstructs;
using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Helpers;
using McpXLib.Utils;

namespace McpXLib.Commands;

public sealed class MonitorCommand<T1, T2> : BaseCommand, IPlcCommand<(T1[] wordValues, T2[] doubleValues)>
    where T1 : unmanaged
    where T2 : unmanaged
{
    private readonly int wordLength;
    private readonly int doubleWordLength;

    public MonitorCommand((Prefix, string)[] wordAddresses, (Prefix, string)[] doubleWordAddresses) : base()
    {
        wordLength = wordAddresses.Length;
        doubleWordLength = doubleWordAddresses.Length;

        ValidatePramater();

        content = new ContentPacketHelper(
            command: [0x02, 0x08],
            subCommand: [0x00, 0x00],
            commandContent: [],
            monitoringTimer: [0x00, 0x00]
        );
    }

    public void ValidatePramater()
    {
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
