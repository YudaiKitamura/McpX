using McpXLib.Abstructs;
using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Helpers;
using McpXLib.Utils;

namespace McpXLib.Commands;

public sealed class BitBatchReadAsciiCommand : BaseAsciiCommand, IPlcCommand<bool[]>
{
    public const ushort MIN_BIT_LENGTH = 1;
    public const ushort MAX_BIT_LENGTH = 7168;
    private readonly ushort bitLength;

    public BitBatchReadAsciiCommand(Prefix prefix, string address, ushort bitLength) : base()
    {
        this.bitLength = bitLength;

        ValidatePramater();

        content = new ContentAsciiPacketHelper(
            command: "0401",
            subCommand: "0001",
            commandContent: $"{ DeviceConverter.ToASCIIAddress(prefix, address) }{ bitLength.ToString("X").PadLeft(4, '0') }",
            monitoringTimer: "0000"
        );
    }

    public void ValidatePramater()
    {
        if (bitLength < MIN_BIT_LENGTH || bitLength > MAX_BIT_LENGTH)
        {
            throw new ArgumentException($"Bit length can be from {MIN_BIT_LENGTH} to {MAX_BIT_LENGTH}.");
        }
    }

    public async Task<bool[]> ExecuteAsync(IPlc plc)
    {
        Route = plc.Route;
        return DeviceConverter.ConvertValueArray<bool>(
            new BitResponseAsciiPacketHelper(
                await plc.RequestAsync(ToBytes())
            ).Content
        ).Take(bitLength).ToArray();
    }

    public bool[] Execute(IPlc plc)
    {
        Route = plc.Route;
        return DeviceConverter.ConvertValueArray<bool>(
            new BitResponseAsciiPacketHelper(
                plc.Request(ToBytes())
            ).Content
        ).Take(bitLength).ToArray();
    }
}
