using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Parsers;
using McpXLib.Utils;
using McpXLib.Builders;

namespace McpXLib.Commands;

internal sealed class MonitorCommand<T1, T2> : PlcCommandBase<(T1[] wordValues, T2[] doubleValues)>
    where T1 : unmanaged
    where T2 : unmanaged
{
    private readonly int wordLength;
    private readonly int doubleWordLength;

    internal MonitorCommand((Prefix prefix, string address)[] wordDevices, (Prefix prefix, string address)[] doubleWordDevices, ushort monitoringTimer = 0, ProcessorSeries series = ProcessorSeries.Q)
    {
        wordLength = wordDevices.Length;
        doubleWordLength = doubleWordDevices.Length;


        commandPacketBuilder = new CommandPacketBuilder(
            command: [0x02, 0x08],
            subCommand: DeviceConverter.ToSubCommand([0x00, 0x00], series),
            monitoringTimer: monitoringTimer
        );
    }

    protected override ResponseFrameSelector CreateResponseSelector(IPlc plc, ushort serialNumber)
        => new(plc, serialNumber, wordLength: wordLength, doubleWordLength: doubleWordLength);

    protected override (T1[] wordValues, T2[] doubleValues) Parse(byte[] responseContent)
    {
        return SplitWordAndDoubleWord<T1, T2>(responseContent, wordLength, doubleWordLength);
    }
}
