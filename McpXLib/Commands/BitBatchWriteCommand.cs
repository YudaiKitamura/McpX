using McpXLib.Abstructs;
using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Helpers;
using McpXLib.Utils;

namespace McpXLib.Commands;

public sealed class BitBatchWriteCommand : BaseCommand, IPlcCommand<bool>
{
    public const ushort MIN_BIT_LENGTH = 1;
    public const ushort MAX_BIT_LENGTH = 7168;
    private readonly ushort bitLength;

    public BitBatchWriteCommand(Prefix prefix, string address, bool[] values) : base()
    {
        bitLength = (ushort)values.Length;

        ValidatePramater();

        var commandContent = new List<byte>();
        commandContent.AddRange(DeviceConverter.ToByteAddress(prefix, address));
        commandContent.AddRange(BitConverter.GetBytes(bitLength));

        var bytes = new List<byte>();
        int i = 0;
        foreach (var value in DeviceConverter.ConvertByteValueArray(values)) 
        {
            if (bytes.Count == 0 || i % 2 == 0)
            {
                bytes.Add((value & 0x01) != 0 ? (byte)0x10 : (byte)0x00);
            }
            else
            {
                byte mask = value == 0x01 ? (byte)0x01 : (byte)0x00;
                bytes[bytes.Count - 1] |= mask;
            }

            i++;
        }

        commandContent.AddRange(bytes);

        this.content = new ContentPacketHelper(
            command: [0x01, 0x14],
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

    public async Task<bool> ExecuteAsync(Interfaces.IPlc mcp)
    {
        Route = mcp.Route;
        return new BitResponsePacketHelper(
            await mcp.RequestAsync(ToBytes())
        ).errCode == 0;
    }

    public bool Execute(Interfaces.IPlc mcp)
    {
        Route = mcp.Route;
        return new BitResponsePacketHelper(
            mcp.Request(ToBytes())
        ).errCode == 0;
    }
}
