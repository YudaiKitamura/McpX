using System.Text;
using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Utils;

namespace McpXLib.Builders;

public class DeviceListPayloadBuilder((Prefix prefix, string address)[] wordDevices, (Prefix prefix, string address)[] doubleWordDevices) : IPayloadBuilder
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
            }

            foreach (var doubleWordDevice in doubleWordDevices)
            { 
                packets.AddRange(Encoding.ASCII.GetBytes(
                    DeviceConverter.ToASCIIAddress(doubleWordDevice.prefix, doubleWordDevice.address)
                ));
            }
        }
        else
        {
            packets.Add(BitConverter.GetBytes((ushort)wordDevices.Length).First());
            packets.Add(BitConverter.GetBytes((ushort)doubleWordDevices.Length).First());

            foreach (var wordDevice in wordDevices)
            { 
                packets.AddRange(DeviceConverter.ToByteAddress(wordDevice.prefix, wordDevice.address));
            }

            foreach (var doubleWordDevice in doubleWordDevices)
            { 
                packets.AddRange(DeviceConverter.ToByteAddress(doubleWordDevice.prefix, doubleWordDevice.address));
            }
        }
    }
}
