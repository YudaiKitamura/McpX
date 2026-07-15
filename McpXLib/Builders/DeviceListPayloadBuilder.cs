using System.Text;
using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Utils;

namespace McpXLib.Builders;

internal class DeviceListPayloadBuilder((Prefix prefix, string address)[] wordDevices, (Prefix prefix, string address)[] doubleWordDevices, ProcessorSeries series = ProcessorSeries.Q) : IPayloadBuilder
{
    public void AppendPayload(List<byte> packets, bool isAscii)
    {
        if (isAscii)
        {
            packets.AddRange(CommandPacketBuilder.BinaryBytesToAsciiBytes(
                binaryBytes: new[] { (byte)wordDevices.Length },
                isReverse: false
            ));

            packets.AddRange(CommandPacketBuilder.BinaryBytesToAsciiBytes(
                binaryBytes: new[] { (byte)doubleWordDevices.Length },
                isReverse: false
            ));

            foreach (var wordDevice in wordDevices)
            {
                packets.AddRange(Encoding.ASCII.GetBytes(
                    DeviceConverter.ToASCIIAddress(wordDevice.prefix, wordDevice.address, series)
                ));
            }

            foreach (var doubleWordDevice in doubleWordDevices)
            {
                packets.AddRange(Encoding.ASCII.GetBytes(
                    DeviceConverter.ToASCIIAddress(doubleWordDevice.prefix, doubleWordDevice.address, series)
                ));
            }
        }
        else
        {
            packets.Add(BitConverter.GetBytes((ushort)wordDevices.Length).First());
            packets.Add(BitConverter.GetBytes((ushort)doubleWordDevices.Length).First());

            foreach (var wordDevice in wordDevices)
            {
                packets.AddRange(DeviceConverter.ToByteAddress(wordDevice.prefix, wordDevice.address, series));
            }

            foreach (var doubleWordDevice in doubleWordDevices)
            {
                packets.AddRange(DeviceConverter.ToByteAddress(doubleWordDevice.prefix, doubleWordDevice.address, series));
            }
        }
    }
}
