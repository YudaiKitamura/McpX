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
        AppendByte(packets, isAscii, (byte)wordBlocks.Length);
        AppendByte(packets, isAscii, (byte)bitBlocks.Length);

        foreach (var block in wordBlocks.Concat(bitBlocks))
        {
            if (isAscii)
            {
                packets.AddRange(Encoding.ASCII.GetBytes(DeviceConverter.ToASCIIAddress(block.Prefix, block.Address, series)));
                packets.AddRange(CommandPacketBuilder.BinaryBytesToAsciiBytes(BitConverter.GetBytes(block.Points), isReverse: true));
            }
            else
            {
                packets.AddRange(DeviceConverter.ToByteAddress(block.Prefix, block.Address, series));
                packets.AddRange(BitConverter.GetBytes(block.Points));
            }

            if (block.Words != null)
            {
                foreach (var word in block.Words)
                {
                    packets.AddRange(isAscii
                        ? CommandPacketBuilder.BinaryBytesToAsciiBytes(BitConverter.GetBytes(word), isReverse: true)
                        : BitConverter.GetBytes(word));
                }
            }
        }
    }

    private static void AppendByte(List<byte> packets, bool isAscii, byte value)
    {
        if (isAscii)
        {
            packets.AddRange(CommandPacketBuilder.BinaryBytesToAsciiBytes([value], isReverse: false));
        }
        else
        {
            packets.Add(value);
        }
    }
}
