using McpXLib.Builders;
using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Utils;

namespace McpXLib.Commands;

/// <summary>
/// 複数ブロック一括読出し（コマンド: 0406）
/// </summary>
/// <remarks>
/// 応答はワードデバイスのブロック、ビットデバイスのブロックの順に格納されるため、指定順に並べ直して返す。
/// </remarks>
internal sealed class MultiBlockReadCommand : IPlcCommand<ushort[][]>
{
    internal const int MAX_POINTS = 960;

    private readonly DeviceBlock[] blocks;
    private readonly DeviceBlock[] wordBlocks;
    private readonly DeviceBlock[] bitBlocks;
    private readonly CommandPacketBuilder commandPacketBuilder;

    // ワードデバイスブロック数+ビットデバイスブロック数の上限（Q/L: 120、iQ-R: 60）
    internal static int GetMaxBlockCount(ProcessorSeries series)
        => series == ProcessorSeries.iQR ? 60 : 120;

    internal MultiBlockReadCommand(DeviceBlock[] blocks, ushort monitoringTimer = 0, ProcessorSeries series = ProcessorSeries.Q)
    {
        this.blocks = blocks;
        wordBlocks = blocks.Where(b => !b.IsBitDevice).ToArray();
        bitBlocks = blocks.Where(b => b.IsBitDevice).ToArray();

        ValidatePramater(series);

        commandPacketBuilder = new CommandPacketBuilder(
            command: [0x06, 0x04],
            subCommand: DeviceConverter.ToSubCommand([0x00, 0x00], series),
            payloadBuilder: new MultiBlockPayloadBuilder(wordBlocks, bitBlocks, series),
            monitoringTimer: monitoringTimer
        );
    }

    private void ValidatePramater(ProcessorSeries series)
    {
        int maxBlockCount = GetMaxBlockCount(series);
        if (blocks.Length < 1 || blocks.Length > maxBlockCount)
        {
            throw new ArgumentException($"Block count can be from 1 to {maxBlockCount}.");
        }

        int points = blocks.Sum(b => b.Points);
        if (blocks.Any(b => b.Points == 0) || points > MAX_POINTS)
        {
            throw new ArgumentException($"Total points can be from 1 to {MAX_POINTS}.");
        }
    }

    public async Task<ushort[][]> ExecuteAsync(IPlc plc)
    {
        var requestFrameSelector = new RequestFrameSelector(plc, commandPacketBuilder);
        var responseFrameSelector = new ResponseFrameSelector(
            plc,
            requestFrameSelector.GetSerialNumber(),
            DeviceAccessMode.Word
        );

        return ToBlockWords(responseFrameSelector.ParsePacket(
            await plc.RequestAsync(requestFrameSelector.GetRequestPacket(), responseFrameSelector)
        ));
    }

    public ushort[][] Execute(IPlc plc)
    {
        var requestFrameSelector = new RequestFrameSelector(plc, commandPacketBuilder);
        var responseFrameSelector = new ResponseFrameSelector(
            plc,
            requestFrameSelector.GetSerialNumber(),
            DeviceAccessMode.Word
        );

        return ToBlockWords(responseFrameSelector.ParsePacket(
            plc.Request(requestFrameSelector.GetRequestPacket(), responseFrameSelector)
        ));
    }

    // 応答データ（ワードブロック→ビットブロックの順）を、指定したブロックの順に分ける
    private ushort[][] ToBlockWords(byte[] content)
    {
        var words = DeviceConverter.ConvertValueArray<ushort>(content);
        var result = new Dictionary<DeviceBlock, ushort[]>();

        int index = 0;
        foreach (var block in wordBlocks.Concat(bitBlocks))
        {
            if (index + block.Points > words.Length)
            {
                throw new Exceptions.RecivePacketException("Received packet had an invalid content.");
            }
            result[block] = words.Skip(index).Take(block.Points).ToArray();
            index += block.Points;
        }

        return blocks.Select(b => result[b]).ToArray();
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
