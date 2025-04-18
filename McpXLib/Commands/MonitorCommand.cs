using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Parsers;
using McpXLib.Utils;
using McpXLib.Builders;

namespace McpXLib.Commands;

internal sealed class MonitorCommand<T1, T2> : IPlcCommand<(T1[] wordValues, T2[] doubleValues)>
    where T1 : unmanaged
    where T2 : unmanaged
{
    private readonly int wordLength;
    private readonly int doubleWordLength;
    private readonly CommandPacketBuilder commandPacketBuilder;

    internal MonitorCommand((Prefix prefix, string address)[] wordDevices, (Prefix prefix, string address)[] doubleWordDevices)
    {
        wordLength = wordDevices.Length;
        doubleWordLength = doubleWordDevices.Length;

        ValidatePramater();

        commandPacketBuilder = new CommandPacketBuilder(
            command: [0x02, 0x08],
            subCommand: [0x00, 0x00],
            monitoringTimer: [0x00, 0x00]
        );
    }

    internal void ValidatePramater()
    {
    }

    public async Task<(T1[] wordValues, T2[] doubleValues)> ExecuteAsync(IPlc plc)
    {
        var requestFrameSelector = new RequestFrameSelector(plc, commandPacketBuilder);
        var responseFrameSelector = new ResponseFrameSelector(
            plc,
            requestFrameSelector.GetSerialNumber(),
            wordLength: wordLength,
            doubleWordLength: doubleWordLength
        );

        var responseContent = responseFrameSelector.ParsePacket(
            await plc.RequestAsync(requestFrameSelector.GetRequestPacket(), responseFrameSelector)
        );

        return (
            wordValues: DeviceConverter.ConvertValueArray<T1>(responseContent
                .Take(wordLength * DeviceConverter.GetWordLength<T1>() * 2)
                .ToArray()
            ),
            doubleValues: DeviceConverter.ConvertValueArray<T2>(responseContent
                .Skip(wordLength * DeviceConverter.GetWordLength<T1>() * 2)
                .Take(doubleWordLength * DeviceConverter.GetWordLength<T2>() * 2)
                .ToArray()
            )
        );
    }

    public (T1[] wordValues, T2[] doubleValues) Execute(IPlc plc)
    {
        var requestFrameSelector = new RequestFrameSelector(plc, commandPacketBuilder);
        var responseFrameSelector = new ResponseFrameSelector(
            plc,
            requestFrameSelector.GetSerialNumber(),
            wordLength: wordLength,
            doubleWordLength: doubleWordLength
        );

        var responseContent = responseFrameSelector.ParsePacket(
            plc.Request(requestFrameSelector.GetRequestPacket(), responseFrameSelector)
        );

        return (
            wordValues: DeviceConverter.ConvertValueArray<T1>(responseContent
                .Take(wordLength * DeviceConverter.GetWordLength<T1>() * 2)
                .ToArray()
            ),
            doubleValues: DeviceConverter.ConvertValueArray<T2>(responseContent
                .Skip(wordLength * DeviceConverter.GetWordLength<T1>() * 2)
                .Take(doubleWordLength * DeviceConverter.GetWordLength<T2>() * 2)
                .ToArray()
            )
        );
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
