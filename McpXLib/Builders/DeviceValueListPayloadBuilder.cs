using System.Text;
using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Utils;

namespace McpXLib.Builders;

public class DeviceValueListPayloadBuilder<T1, T2>((Prefix prefix, string address, T1 value)[] wordDevices, (Prefix prefix, string address, T2 value)[] doubleWordDevices) : IPayloadBuilder
    where T1 : unmanaged
    where T2 : unmanaged
{
    public void AppendPayload(List<byte> packets, bool isAscii)
    {
        if (isAscii)
        {
            packets.AddRange(CommandPacketBuilder.BinaryBytesToAsciiByte(
                binaryBytes: BitConverter.GetBytes((ushort)wordDevices.Length),
                isReverse: true
            ));

            packets.AddRange(CommandPacketBuilder.BinaryBytesToAsciiByte(
                binaryBytes: BitConverter.GetBytes((ushort)doubleWordDevices.Length),
                isReverse: true
            ));

            foreach (var wordDevice in wordDevices)
            { 
                packets.AddRange(Encoding.ASCII.GetBytes(
                    DeviceConverter.ToASCIIAddress(wordDevice.prefix, wordDevice.address)
                ));

                packets.AddRange(CommandPacketBuilder.BinaryBytesToAsciiBytes(
                    binaryBytes: DeviceConverter.ReverseByTwoBytes(
                        DeviceConverter.StructToBytes(wordDevice.value)
                    ),
                    isReverse: false
                ));
            }

            foreach (var doubleWordDevice in doubleWordDevices)
            { 
                packets.AddRange(Encoding.ASCII.GetBytes(
                    DeviceConverter.ToASCIIAddress(doubleWordDevice.prefix, doubleWordDevice.address)
                ));

                packets.AddRange(CommandPacketBuilder.BinaryBytesToAsciiBytes(
                    DeviceConverter.StructToBytes(doubleWordDevice.value),
                    isReverse: true
                ));
            }
        }
        else
        {
            packets.AddRange(BitConverter.GetBytes((ushort)wordDevices.Length).First());
            packets.AddRange(BitConverter.GetBytes((ushort)doubleWordDevices.Length).First());

            foreach (var wordDevice in wordDevices)
            { 
                packets.AddRange(DeviceConverter.ToByteAddress(wordDevice.prefix, wordDevice.address));
                packets.AddRange(DeviceConverter.StructToBytes(wordDevice.value));
            }

            foreach (var doubleWordDevice in doubleWordDevices)
            { 
                packets.AddRange(DeviceConverter.ToByteAddress(doubleWordDevice.prefix, doubleWordDevice.address));
                packets.AddRange(DeviceConverter.StructToBytes(doubleWordDevice.value));
            }
        }
    }
}
