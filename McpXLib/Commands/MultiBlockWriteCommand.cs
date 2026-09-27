using McpXLib.Builders;
using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Utils;

namespace McpXLib.Commands;

/// <summary>
/// 複数ブロック一括書込み（コマンド: 1406）
/// </summary>
internal sealed class MultiBlockWriteCommand : IPlcCommand<bool>
{
    internal const int MAX_POINTS = 960;

    private readonly DeviceBlock[] blocks;
    private readonly CommandPacketBuilder commandPacketBuilder;

    // (ブロック数の合計×係数)+(デバイス点数の合計) ≦ 960 の係数（Q/L: 4、iQ-R: 9）
    internal static int GetBlockCost(ProcessorSeries series)
        => series == ProcessorSeries.iQR ? 9 : 4;

    internal MultiBlockWriteCommand(DeviceBlock[] blocks, ushort monitoringTimer = 0, ProcessorSeries series = ProcessorSeries.Q)
    {
        this.blocks = blocks;

        ValidatePramater(series);

        commandPacketBuilder = new CommandPacketBuilder(
            command: [0x06, 0x14],
            subCommand: DeviceConverter.ToSubCommand([0x00, 0x00], series),
            payloadBuilder: new MultiBlockPayloadBuilder(
                blocks.Where(b => !b.IsBitDevice).ToArray(),
                blocks.Where(b => b.IsBitDevice).ToArray(),
                series),
            monitoringTimer: monitoringTimer
        );
    }

    private void ValidatePramater(ProcessorSeries series)
    {
        int maxBlockCount = MultiBlockReadCommand.GetMaxBlockCount(series);
        if (blocks.Length < 1 || blocks.Length > maxBlockCount)
        {
            throw new ArgumentException($"Block count can be from 1 to {maxBlockCount}.");
        }

        if (blocks.Any(b => b.Points == 0 || b.Words == null || b.Words.Length != b.Points))
        {
            throw new ArgumentException("Each block must have write data for its points.");
        }

        int cost = GetBlockCost(series);
        if (blocks.Length * cost + blocks.Sum(b => b.Points) > MAX_POINTS)
        {
            throw new ArgumentException($"(Block count x {cost}) + total points must be {MAX_POINTS} or less.");
        }
    }

    public async Task<bool> ExecuteAsync(IPlc plc)
    {
        var requestFrameSelector = new RequestFrameSelector(plc, commandPacketBuilder);
        var responseFrameSelector = new ResponseFrameSelector(
            plc,
            requestFrameSelector.GetSerialNumber()
        );

        responseFrameSelector.ParsePacket(
            await plc.RequestAsync(requestFrameSelector.GetRequestPacket(), responseFrameSelector)
        );

        return true;
    }

    public bool Execute(IPlc plc)
    {
        var requestFrameSelector = new RequestFrameSelector(plc, commandPacketBuilder);
        var responseFrameSelector = new ResponseFrameSelector(
            plc,
            requestFrameSelector.GetSerialNumber()
        );

        responseFrameSelector.ParsePacket(
            plc.Request(requestFrameSelector.GetRequestPacket(), responseFrameSelector)
        );

        return true;
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
