using System.Text;
using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Utils;

namespace McpXLib.Builders;

public class DeviceValuePayloadBuilder<T>(Prefix prefix, string address, T[] values) : IPayloadBuilder
    where T : unmanaged
{
    public void AppendPayload(List<byte> packets, bool isAscii)
    {
        ushort wordLength = (ushort)(values.Length * DeviceConverter.GetWordLength<T>());

        if (isAscii)
        {
            packets.AddRange(Encoding.ASCII.GetBytes(DeviceConverter.ToASCIIAddress(prefix, address)));
            packets.AddRange(CommandPacketBuilder.BinaryBytesToAsciiBytes(
                binaryBytes: BitConverter.GetBytes(wordLength),
                isReverse: true
            ));

            packets.AddRange(CommandPacketBuilder.BinaryBytesToAsciiBytes(
                binaryBytes: DeviceConverter.ReverseByTwoBytes(
                    DeviceConverter.ConvertByteValueArray(values)
                ),
                isReverse: false
            ));
        }
        else
        {
            packets.AddRange(DeviceConverter.ToByteAddress(prefix, address));
            packets.AddRange(BitConverter.GetBytes(wordLength));
            packets.AddRange(DeviceConverter.ConvertByteValueArray(values));
        }
    }
}
