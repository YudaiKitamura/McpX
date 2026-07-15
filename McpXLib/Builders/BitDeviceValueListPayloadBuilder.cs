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
                    binaryBytes: new[] { bitDevice.value ? (byte)0x01 : (byte)0x00 },
                    isReverse: false
                ));
            }
        }
        else
        {
            packets.Add((byte)bitDevices.Length);

            foreach (var bitDevice in bitDevices)
            {
                packets.AddRange(DeviceConverter.ToByteAddress(bitDevice.prefix, bitDevice.address, series));
                packets.Add(bitDevice.value ? (byte)0x01 : (byte)0x00);
            }
        }
    }
}
