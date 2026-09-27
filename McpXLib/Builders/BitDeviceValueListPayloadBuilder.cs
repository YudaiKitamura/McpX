using System.Text;
using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Utils;

namespace McpXLib.Builders;

internal class BitDeviceValueListPayloadBuilder((Prefix prefix, string address, bool value)[] bitDevices, ProcessorSeries series = ProcessorSeries.Q) : IPayloadBuilder
{
    public void AppendPayload(List<byte> packets, bool isAscii)
    {
        if (isAscii)
        {
            packets.AddRange(CommandPacketBuilder.BinaryBytesToAsciiBytes(
                binaryBytes: new[] { (byte)bitDevices.Length },
                isReverse: false
            ));

            foreach (var bitDevice in bitDevices)
            {
                packets.AddRange(Encoding.ASCII.GetBytes(
                    DeviceConverter.ToASCIIAddress(bitDevice.prefix, bitDevice.address, series)
                ));

                packets.AddRange(CommandPacketBuilder.BinaryBytesToAsciiBytes(
                    binaryBytes: ToSetResetBytes(bitDevice.value),
                    isReverse: true
                ));
            }
        }
        else
        {
            packets.Add((byte)bitDevices.Length);

            foreach (var bitDevice in bitDevices)
            {
                packets.AddRange(DeviceConverter.ToByteAddress(bitDevice.prefix, bitDevice.address, series));
                packets.AddRange(ToSetResetBytes(bitDevice.value));
            }
        }
    }

    // セット/リセット指定は Q/L 形式（サブコマンド0001）が1バイト、iQ-R 形式（サブコマンド0003）が2バイト。
    private byte[] ToSetResetBytes(bool value)
    {
        byte setReset = value ? (byte)0x01 : (byte)0x00;
        return series == ProcessorSeries.iQR ? [setReset, 0x00] : [setReset];
    }
}
