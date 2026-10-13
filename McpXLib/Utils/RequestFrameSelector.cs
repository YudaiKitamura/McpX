using McpXLib.Builders;
using McpXLib.Enums;
using McpXLib.Interfaces;

namespace McpXLib.Utils;

internal class RequestFrameSelector 
{
    private readonly RequestPacketBuilder requestPacketBuilder;
    // 4E フレームのシリアル番号の採番（全インスタンス共通。ushort に収まる下位16ビットを使う）
    private static int nextSerialNumber = -1;
    private ushort serialNumber;
    private IPlc plc;

    internal RequestFrameSelector(IPlc plc, CommandPacketBuilder commandPacketBuilder)
    {
        this.plc = plc;

        serialNumber = (ushort)Interlocked.Increment(ref nextSerialNumber);

        requestPacketBuilder = new RequestPacketBuilder(
            subHeaderPacketBuilder: this.plc.RequestFrame == RequestFrame.E3 ? new SubHeaderPacketBuilder() : new SubHeader4EPacketBuilder(serialNumber),
            routePacketBuilder: this.plc.Route,
            commandPacketBuilder: commandPacketBuilder
        );
    }

    internal byte[] GetRequestPacket()
    {
        return plc.IsAscii ? requestPacketBuilder.ToAsciiBytes() : requestPacketBuilder.ToBinaryBytes();
    }

    internal ushort GetSerialNumber()
    {
        return serialNumber;
    }
}
