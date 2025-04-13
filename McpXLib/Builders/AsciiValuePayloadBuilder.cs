using System.Text;
using McpXLib.Interfaces;

namespace McpXLib.Builders;

internal class AsciiPayloadBuilder(string value) : IPayloadBuilder
{
    public void AppendPayload(List<byte> packets, bool isAscii)
    {
        if (isAscii)
        {
            packets.AddRange(CommandPacketBuilder.BinaryBytesToAsciiBytes(
                binaryBytes: BitConverter.GetBytes((ushort)value.Length),
                isReverse: true
            ));
        }
        else
        {
            packets.AddRange(BitConverter.GetBytes((ushort)value.Length));
        }

        packets.AddRange(Encoding.ASCII.GetBytes(value));
    }
}
