using McpXLib.Abstructs;
using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Helpers;
using McpXLib.Utils;

namespace McpXLib.Commands;

public sealed class BitBatchReadCommand : BaseCommand, IPlcCommand<bool[]>
{
    public const ushort MIN_BIT_LENGTH = 1;
    public const ushort MAX_BIT_LENGTH = 7168;
    private readonly ushort bitLength;

    public BitBatchReadCommand(Prefix prefix, string address, ushort bitLength) : base()
    {
        this.bitLength = bitLength;

        ValidatePramater();

        var commandContent = new List<byte>();
        commandContent.AddRange(DeviceConverter.ToByteAddress(prefix, address));
        commandContent.AddRange(BitConverter.GetBytes(bitLength));

        content = new ContentPacketHelper(
            command: [0x01, 0x04],
            subCommand: [0x01, 0x00],
            commandContent: commandContent.ToArray(),
            monitoringTimer: [0x00, 0x00]
        );
    }

    public void ValidatePramater()
    {
        if (bitLength < MIN_BIT_LENGTH || bitLength > MAX_BIT_LENGTH)
        {
            throw new ArgumentException($"Bit length can be from {MIN_BIT_LENGTH} to {MAX_BIT_LENGTH}.");
        }
    }

    public async Task<bool[]> ExecuteAsync(Interfaces.IPlc plc)
    {
        Route = plc.Route;
        return DeviceConverter.ConvertValueArray<bool>(
            new BitResponsePacketHelper(
                await plc.RequestAsync(ToBytes())
            ).Content
        ).Take(bitLength).ToArray();
    }

    public bool[] Execute(Interfaces.IPlc plc)
    {
        Route = plc.Route;
        return DeviceConverter.ConvertValueArray<bool>(
            new BitResponsePacketHelper(
                plc.Request(ToBytes())
            ).Content
        ).Take(bitLength).ToArray();
    }
}
