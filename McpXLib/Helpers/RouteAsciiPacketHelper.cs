using System.Text;
using McpXLib.Interfaces;

namespace McpXLib.Helpers;

public class RouteAsciiPacketHelper : IRoute
{
    public byte NetworkNumber => networkNumber;
    public byte PCNumber => pcNumber;
    public byte UnitNumber => unitNumber;
    public byte IONumber => ioNumber;
    public byte StationNumber => stationNumber;
    private readonly byte networkNumber;
    private readonly byte pcNumber;
    private readonly byte unitNumber;
    private readonly byte ioNumber;
    private readonly byte stationNumber;

    public RouteAsciiPacketHelper(
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
    
    public byte[] ToBytes()
    {
        var packets = new List<byte>();
        packets.AddRange(Encoding.ASCII.GetBytes(NetworkNumber.ToString("X2")));
        packets.AddRange(Encoding.ASCII.GetBytes(PCNumber.ToString("X2")));
        packets.AddRange(Encoding.ASCII.GetBytes(IONumber.ToString("X2")));
        packets.AddRange(Encoding.ASCII.GetBytes(UnitNumber.ToString("X2")));
        packets.AddRange(Encoding.ASCII.GetBytes(StationNumber.ToString("X2")));
        
        return packets.ToArray();
    }
}
