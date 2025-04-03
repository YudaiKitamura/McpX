namespace McpXLib.Interfaces;

public interface IRequestPacketBuilder : IPacketBuilder
{
    IPacketBuilder SubHeaderPacketBuilder { get; }
    IPacketBuilder RoutePacketBuilder { get; }
    IPacketBuilder CommandPacketBuilder { get; }
}