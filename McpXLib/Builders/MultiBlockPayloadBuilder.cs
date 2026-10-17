using System.Text;
using McpXLib.Commands;
using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Utils;

namespace McpXLib.Builders;

/// <summary>
/// 複数ブロック一括読出し・書込み（0406 / 1406）の要求データ。
/// </summary>
/// <remarks>
/// [ワードデバイスブロック数][ビットデバイスブロック数] に続けて、ワードデバイス、ビットデバイスの順に
/// [先頭デバイス][デバイス点数]（書込み時は [書込みデータ] も）を並べる。
/// </remarks>
internal class MultiBlockPayloadBuilder(DeviceBlock[] wordBlocks, DeviceBlock[] bitBlocks, ProcessorSeries series = ProcessorSeries.Q) : IPayloadBuilder
{
    public void AppendPayload(List<byte> packets, bool isAscii)
    {
        var writer = new PacketWriter(packets, isAscii, series);
        writer.Byte((byte)wordBlocks.Length);
        writer.Byte((byte)bitBlocks.Length);

        foreach (var block in wordBlocks.Concat(bitBlocks))
        {
            writer.Device(block.Prefix, block.Address);
            writer.Number(block.Points, 2);

            if (block.Words != null)
            {
                foreach (var word in block.Words)
                {
                    writer.Number(word, 2);
                }
            }
        }
    }
}
