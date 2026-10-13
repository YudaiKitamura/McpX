using McpXLib.Interfaces;

namespace McpXLib.Builders;

internal class RequestPacketBuilder(IPacketBuilder subHeaderPacketBuilder, IPacketBuilder routePacketBuilder, IPacketBuilder commandPacketBuilder) : IPacketBuilder
{
    public byte[] ToAsciiBytes()
    {
        List<byte> packets = new List<byte>();
        packets.AddRange(subHeaderPacketBuilder.ToAsciiBytes());
        packets.AddRange(routePacketBuilder.ToAsciiBytes());
        packets.AddRange(commandPacketBuilder.ToAsciiBytes());
        return packets.ToArray();
    }

    public byte[] ToBinaryBytes()
    {
        List<byte> packets = new List<byte>();
        packets.AddRange(subHeaderPacketBuilder.ToBinaryBytes());
        packets.AddRange(routePacketBuilder.ToBinaryBytes());
        packets.AddRange(commandPacketBuilder.ToBinaryBytes());
        return packets.ToArray();  
    }
}
