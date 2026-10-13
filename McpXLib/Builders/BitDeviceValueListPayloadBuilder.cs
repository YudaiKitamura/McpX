using System.Text;
using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Utils;

namespace McpXLib.Builders;

internal class BitDeviceValueListPayloadBuilder((Prefix prefix, string address, bool value)[] bitDevices, ProcessorSeries series = ProcessorSeries.Q) : IPayloadBuilder
{
    public void AppendPayload(List<byte> packets, bool isAscii)
    {
        var writer = new PacketWriter(packets, isAscii, series);
        writer.Byte((byte)bitDevices.Length);

        // セット/リセット指定は Q/L 形式（サブコマンド0001）が1バイト、iQ-R 形式（サブコマンド0003）が2バイト。
        int setResetSize = series == ProcessorSeries.iQR ? 2 : 1;
        foreach (var (prefix, address, value) in bitDevices)
        {
            writer.Device(prefix, address);
            writer.Number(value ? 1u : 0u, setResetSize);
        }
    }
}
