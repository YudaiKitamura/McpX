using System.Text;
using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Utils;

namespace McpXLib.Builders;

internal class DeviceListPayloadBuilder((Prefix prefix, string address)[] wordDevices, (Prefix prefix, string address)[] doubleWordDevices, ProcessorSeries series = ProcessorSeries.Q) : IPayloadBuilder
{
    public void AppendPayload(List<byte> packets, bool isAscii)
    {
        var writer = new PacketWriter(packets, isAscii, series);
        writer.Byte((byte)wordDevices.Length);
        writer.Byte((byte)doubleWordDevices.Length);

        foreach (var (prefix, address) in wordDevices.Concat(doubleWordDevices))
        {
            writer.Device(prefix, address);
        }
    }
}
