using McpXLib.Interfaces;

namespace McpXLib.Helpers;

public class RoutePacketHelper : IRoute
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

    public RoutePacketHelper(
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
        return
        [
            NetworkNumber,
            PCNumber,
            UnitNumber,
            IONumber,
            StationNumber
        ];
    }
}
