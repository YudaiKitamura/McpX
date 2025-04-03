using System.Text;
using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Utils;

namespace McpXLib.Builders;

public class DevicePayloadBuilder(Prefix prefix, string address, ushort length) : IPayloadBuilder
{
    public void AppendPayload(List<byte> packets, bool isAscii)
    {
        if (isAscii)
        {
            packets.AddRange(Encoding.ASCII.GetBytes(DeviceConverter.ToASCIIAddress(prefix, address)));
            packets.AddRange(CommandPacketBuilder.BinaryBytesToAsciiBytes(
                binaryBytes: BitConverter.GetBytes(length),
                isReverse: true
            ));
        }
        else
        {
            packets.AddRange(DeviceConverter.ToByteAddress(prefix, address));
            packets.AddRange(BitConverter.GetBytes(length));
        }
    }
}
