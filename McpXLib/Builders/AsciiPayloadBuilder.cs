using System.Text;
using McpXLib.Interfaces;

namespace McpXLib.Builders;

internal class AsciiPayloadBuilder(string value) : IPayloadBuilder
{
    public void AppendPayload(List<byte> packets, bool isAscii)
    {
        var writer = new PacketWriter(packets, isAscii);
        writer.Number((ushort)value.Length, 2);
        writer.Raw(Encoding.ASCII.GetBytes(value));
    }
}
