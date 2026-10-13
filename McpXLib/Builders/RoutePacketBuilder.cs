using System.Text;
using McpXLib.Interfaces;

namespace McpXLib.Builders;

internal class RoutePacketBuilder : IPacketBuilder
{
    private readonly byte networkNumber;
    private readonly byte pcNumber;
    // 要求先ユニットI/O番号（2バイト。バイナリは下位→上位、ASCII は上位→下位の順）
    private readonly ushort moduleIoNumber;
    private readonly byte moduleStationNumber;

    internal RoutePacketBuilder(
        byte networkNumber = 0x00,
        byte pcNumber = 0xFF,
        ushort moduleIoNumber = 0x03FF,
        byte moduleStationNumber = 0x00)
    {
        this.networkNumber = networkNumber;
        this.pcNumber = pcNumber;
        this.moduleIoNumber = moduleIoNumber;
        this.moduleStationNumber = moduleStationNumber;
    }

    internal RoutePacketBuilder(McpXRoute route)
        : this(route.NetworkNo, route.PcNo, route.ModuleIoNo, route.ModuleStationNo)
    {
    }

    public byte[] ToBinaryBytes()
    {
        return
        [
            networkNumber,
            pcNumber,
            (byte)(moduleIoNumber & 0xFF),
            (byte)(moduleIoNumber >> 8),
            moduleStationNumber
        ];
    }

    public byte[] ToAsciiBytes()
    {
        return Encoding.ASCII.GetBytes(
            networkNumber.ToString("X2") + pcNumber.ToString("X2") + moduleIoNumber.ToString("X4") + moduleStationNumber.ToString("X2")
        );
    }
}
