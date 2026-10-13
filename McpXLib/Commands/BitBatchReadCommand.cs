using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Utils;
using McpXLib.Builders;

namespace McpXLib.Commands;

internal sealed class BitBatchReadCommand : PlcCommandBase<bool[]>
{
    internal const ushort MIN_BIT_LENGTH = 1;
    internal const ushort MAX_BIT_LENGTH = 7168;
    // Ethernet（E71）の ASCII コード交信では、ビット単位の一括読出し・書込みは 3584点まで
    internal const ushort MAX_ASCII_BIT_LENGTH = 3584;
    private readonly ushort bitLength;

    internal BitBatchReadCommand(Prefix prefix, string address, ushort bitLength, ushort monitoringTimer = 0, ProcessorSeries series = ProcessorSeries.Q) : base()
    {
        this.bitLength = bitLength;

        ValidateParameters();

        commandPacketBuilder = new CommandPacketBuilder(
            command: [0x01, 0x04],
            subCommand: DeviceConverter.ToSubCommand([0x01, 0x00], series),
            payloadBuilder: new DevicePayloadBuilder(prefix, address, bitLength, series),
            monitoringTimer: monitoringTimer
        );
    }

    internal void ValidateParameters()
    {
        if (bitLength < MIN_BIT_LENGTH || bitLength > MAX_BIT_LENGTH)
        {
            throw new ArgumentException($"Bit length can be from {MIN_BIT_LENGTH} to {MAX_BIT_LENGTH}.");
        }
    }

    protected override ResponseFrameSelector CreateResponseSelector(IPlc plc, ushort serialNumber)
        => new(plc, serialNumber, DeviceAccessMode.Bit);

    protected override bool[] Parse(byte[] responseContent)
    {
        // MEMO:
        //  読出しビット数が奇数の場合、余分な4ビットを含む1バイトをクリア
        if (bitLength % 2 != 0) 
        {
            Array.Resize(ref responseContent, responseContent.Length - 1);
        }

        return DeviceConverter.ConvertValueArray<bool>(responseContent);
    }
}
