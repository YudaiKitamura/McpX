using System.Text;
using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Utils;

namespace McpXLib.Builders;

internal class BitDeviceValuePayloadBuilder(Prefix prefix, string address, bool[] values, ProcessorSeries series = ProcessorSeries.Q) : IPayloadBuilder
{
    public void AppendPayload(List<byte> packets, bool isAscii)
    {
        var writer = new PacketWriter(packets, isAscii, series);
        writer.Device(prefix, address);
        writer.Number((ushort)values.Length, 2);
        writer.Bits(values);
    }
}
