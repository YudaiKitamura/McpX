using System.Text;
using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Utils;

namespace McpXLib.Builders;

internal class DeviceValueListPayloadBuilder<T1, T2>((Prefix prefix, string address, T1 value)[] wordDevices, (Prefix prefix, string address, T2 value)[] doubleWordDevices, ProcessorSeries series = ProcessorSeries.Q) : IPayloadBuilder
    where T1 : unmanaged
    where T2 : unmanaged
{
    public void AppendPayload(List<byte> packets, bool isAscii)
    {
        var writer = new PacketWriter(packets, isAscii, series);
        writer.Byte((byte)wordDevices.Length);
        writer.Byte((byte)doubleWordDevices.Length);

        foreach (var (prefix, address, value) in wordDevices)
        {
            writer.Device(prefix, address);
            writer.Words(DeviceConverter.StructToBytes(value));
        }

        foreach (var (prefix, address, value) in doubleWordDevices)
        {
            writer.Device(prefix, address);
            writer.Value(DeviceConverter.StructToBytes(value));
        }
    }
}
