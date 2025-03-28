using McpXLib.Abstructs;
using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Helpers;
using McpXLib.Utils;

namespace McpXLib.Commands;

public sealed class BitBatchWriteAsciiCommand : BaseAsciiCommand, IPlcCommand<bool>
{
    public const ushort MIN_BIT_LENGTH = 1;
    public const ushort MAX_BIT_LENGTH = 7168;
    private readonly ushort bitLength;

    public BitBatchWriteAsciiCommand(Prefix prefix, string address, bool[] values) : base()
    {
        bitLength = (ushort)values.Length;

        ValidatePramater();

        var hexValueStr = string.Concat(
            values.Select(x => x ? "1" : "0")
        );

        content = new ContentAsciiPacketHelper(
            command: "1401",
            subCommand: "0001",
            commandContent: $"{ DeviceConverter.ToASCIIAddress(prefix, address) }{ bitLength.ToString("X").PadLeft(4, '0') }{ hexValueStr }",
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

    public async Task<bool> ExecuteAsync(IPlc mcp)
    {
        Route = mcp.Route;
        return new BitResponseAsciiPacketHelper(
            await mcp.RequestAsync(ToBytes())
        ).errCode == 0;
    }

    public bool Execute(IPlc mcp)
    {
        Route = mcp.Route;
        return new BitResponseAsciiPacketHelper(
            mcp.Request(ToBytes())
        ).errCode == 0;
    }
}
