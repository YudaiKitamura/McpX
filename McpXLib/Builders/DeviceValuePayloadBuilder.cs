using System.Text;
using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Utils;

namespace McpXLib.Builders;

internal class DeviceValuePayloadBuilder<T>(Prefix prefix, string address, T[] values, ProcessorSeries series = ProcessorSeries.Q) : IPayloadBuilder
    where T : unmanaged
{
    public void AppendPayload(List<byte> packets, bool isAscii)
    {
        var writer = new PacketWriter(packets, isAscii, series);
        writer.Device(prefix, address);
        writer.Number((ushort)(values.Length * DeviceConverter.GetWordLength<T>()), 2);
        writer.Words(DeviceConverter.ConvertByteValueArray(values));
    }
}
