using System.Text;
using McpXLib.Interfaces;

namespace McpXLib.Builders;

public class SerialPacketBuilder : IPacketBuilder
{
    private readonly byte[] serialNumber;

    public SerialPacketBuilder(ushort serialNumber)
    {
        List<byte> bytes = new List<byte>();
        bytes.AddRange(BitConverter.GetBytes(serialNumber));
        bytes.AddRange([ 0x00, 0x00 ]);

        this.serialNumber = bytes.ToArray();
    }
    
    public byte[] ToBinaryBytes()
    {
        return serialNumber;
    }

    public byte[] ToAsciiBytes()
    {   
        return Encoding.ASCII.GetBytes(
            string.Concat(serialNumber.Select(b => b.ToString("X2")))
        );
    }
}
