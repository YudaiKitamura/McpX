using System.Text;
using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Utils;

namespace McpXLib.Builders;

public class BitDeviceValuePayloadBuilder(Prefix prefix, string address, bool[] values) : IPayloadBuilder
{
    public void AppendPayload(List<byte> packets, bool isAscii)
    {
        ushort bitLength = (ushort)values.Length;

        if (isAscii)
        {
            packets.AddRange(Encoding.ASCII.GetBytes(DeviceConverter.ToASCIIAddress(prefix, address)));
            packets.AddRange(CommandPacketBuilder.BinaryBytesToAsciiBytes(
                binaryBytes: BitConverter.GetBytes(bitLength),
                isReverse: true
            ));

            packets.AddRange(CommandPacketBuilder.BinaryBytesToAsciiByte(
                binaryBytes: DeviceConverter.ConvertByteValueArray(values),
                isReverse: false
            ));
        }
        else
        {
            packets.AddRange(DeviceConverter.ToByteAddress(prefix, address));
            packets.AddRange(BitConverter.GetBytes(bitLength));

            var valueBytes = new List<byte>();
            int i = 0;
            foreach (var value in DeviceConverter.ConvertByteValueArray(values)) 
            {
                if (valueBytes.Count == 0 || i % 2 == 0)
                {
                    valueBytes.Add((value & 0x01) != 0 ? (byte)0x10 : (byte)0x00);
                }
                else
                {
                    byte mask = value == 0x01 ? (byte)0x01 : (byte)0x00;
                    valueBytes[valueBytes.Count - 1] |= mask;
                }

                i++;
            }

            packets.AddRange(valueBytes);
        }
    }
}
