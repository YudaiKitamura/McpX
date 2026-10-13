namespace McpXLib.Interfaces;

internal interface IRequestPacketBuilder : IPacketBuilder
{
    IPacketBuilder SubHeaderPacketBuilder { get; }
    IPacketBuilder RoutePacketBuilder { get; }
    IPacketBuilder CommandPacketBuilder { get; }
}