using System.Text;
using McpXLib.Interfaces;

namespace McpXLib.Builders;

internal class RoutePacketBuilder : IPacketBuilder
{
    private readonly byte networkNumber;
    private readonly byte pcNumber;
    private readonly byte unitNumber;
    private readonly byte ioNumber;
    private readonly byte stationNumber;

    internal RoutePacketBuilder(
        byte networkNumber = 0x00,
        byte pcNumber = 0xFF,
        byte unitNumber = 0xFF,
        byte ioNumber = 0x03,
        byte stationNumber = 0x00) 
    {
        this.networkNumber = networkNumber;
        this.pcNumber = pcNumber;
        this.unitNumber = unitNumber;
        this.ioNumber = ioNumber;
        this.stationNumber = stationNumber;
    }
    
    public byte[] ToBinaryBytes()
    {
        return
        [
            networkNumber,
            pcNumber,
            unitNumber,
            ioNumber,
            stationNumber
        ];
    }

    public byte[] ToAsciiBytes()
    {
        var packets = new List<byte>();
        packets.AddRange(Encoding.ASCII.GetBytes(networkNumber.ToString("X2")));
        packets.AddRange(Encoding.ASCII.GetBytes(pcNumber.ToString("X2")));
        packets.AddRange(Encoding.ASCII.GetBytes(ioNumber.ToString("X2")));
        packets.AddRange(Encoding.ASCII.GetBytes(unitNumber.ToString("X2")));
        packets.AddRange(Encoding.ASCII.GetBytes(stationNumber.ToString("X2")));
        
        return packets.ToArray();
    }
}
