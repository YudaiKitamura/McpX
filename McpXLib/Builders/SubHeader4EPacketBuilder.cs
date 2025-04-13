using System.Text;
using McpXLib.Interfaces;

namespace McpXLib.Builders;

internal class SubHeader4EPacketBuilder : IPacketBuilder
{
    private readonly byte[] subHeader;
    private readonly SerialPacketBuilder serial;

    internal SubHeader4EPacketBuilder(ushort serialNumber)
    {
        subHeader = [ 0x54, 0x00 ];
        this.serial = new SerialPacketBuilder(serialNumber);
    }
    
    public byte[] ToBinaryBytes()
    {
        List<byte> bytes = new List<byte>();
        bytes.AddRange(subHeader);
        bytes.AddRange(serial.ToBinaryBytes());

        return bytes.ToArray();
    }

    public byte[] ToAsciiBytes()
    {
        List<byte> bytes = new List<byte>();
        bytes.AddRange(Encoding.ASCII.GetBytes(
            string.Concat(subHeader.Select(b => b.ToString("X2")))
        ));
        bytes.AddRange(serial.ToAsciiBytes());

        return bytes.ToArray();
    }
}
