using System.Text;
using McpXLib.Interfaces;

namespace McpXLib.Builders;

internal class SubHeaderPacketBuilder : IPacketBuilder
{
    private readonly byte[] subHeader;

    internal SubHeaderPacketBuilder()
    {
        subHeader = [ 0x50, 0x00 ];
    }
    
    public byte[] ToBinaryBytes()
    {
        return subHeader;
    }

    public byte[] ToAsciiBytes()
    {   
        return Encoding.ASCII.GetBytes(
            string.Concat(subHeader.Select(b => b.ToString("X2")))
        );
    }
}
