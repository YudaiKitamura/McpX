using System.Text;
using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Utils;

namespace McpXLib.Builders;

internal class DevicePayloadBuilder(Prefix prefix, string address, ushort length, ProcessorSeries series = ProcessorSeries.Q) : IPayloadBuilder
{
    public void AppendPayload(List<byte> packets, bool isAscii)
    {
        if (isAscii)
        {
            packets.AddRange(Encoding.ASCII.GetBytes(DeviceConverter.ToASCIIAddress(prefix, address, series)));
            packets.AddRange(CommandPacketBuilder.BinaryBytesToAsciiBytes(
                binaryBytes: BitConverter.GetBytes(length),
                isReverse: true
            ));
        }
        else
        {
            packets.AddRange(DeviceConverter.ToByteAddress(prefix, address, series));
            packets.AddRange(BitConverter.GetBytes(length));
        }
    }
}
