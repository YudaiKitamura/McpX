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
internal sealed class MultiBlockReadCommand : PlcCommandBase<ushort[][]>
{
    internal const int MAX_POINTS = 960;

    private readonly DeviceBlock[] blocks;
    private readonly DeviceBlock[] wordBlocks;
    private readonly DeviceBlock[] bitBlocks;

    // ワードデバイスブロック数+ビットデバイスブロック数の上限（Q/L: 120、iQ-R: 60）
    internal static int GetMaxBlockCount(ProcessorSeries series)
        => series == ProcessorSeries.iQR ? 60 : 120;

    internal MultiBlockReadCommand(DeviceBlock[] blocks, ushort monitoringTimer = 0, ProcessorSeries series = ProcessorSeries.Q)
    {
        this.blocks = blocks;
        wordBlocks = blocks.Where(b => !b.IsBitDevice).ToArray();
        bitBlocks = blocks.Where(b => b.IsBitDevice).ToArray();

        ValidateParameters(series);

        commandPacketBuilder = new CommandPacketBuilder(
            command: [0x06, 0x04],
            subCommand: DeviceConverter.ToSubCommand([0x00, 0x00], series),
            payloadBuilder: new MultiBlockPayloadBuilder(wordBlocks, bitBlocks, series),
            monitoringTimer: monitoringTimer
        );
    }

    private void ValidateParameters(ProcessorSeries series)
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

    protected override ResponseFrameSelector CreateResponseSelector(IPlc plc, ushort serialNumber)
        => new(plc, serialNumber, DeviceAccessMode.Word);

    protected override ushort[][] Parse(byte[] responseContent)
        => ToBlockWords(responseContent);

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
                throw new Exceptions.ReceivePacketException("Received packet had an invalid content.");
            }
            result[block] = words.Skip(index).Take(block.Points).ToArray();
            index += block.Points;
        }

        return blocks.Select(b => result[b]).ToArray();
    }
}
