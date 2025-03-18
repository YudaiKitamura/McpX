using McpXLib.Interfaces;
using McpXLib.Helpers;
using McpXLib.Exceptions;

namespace McpXLib.Abstructs;

public abstract class BasePacketHelper : I3EResponseFrame
{
    public byte[] SubHeader => subHeader;
    public IRoute Route => route;
    public byte[] Content => content;
    public string ErrCode => errCode.ToString("X");

    internal readonly byte[] subHeader;
    internal readonly IRoute route;
    internal byte[] content;
    internal readonly uint errCode;

    public BasePacketHelper(byte[] bytes)
    {
        if (bytes.Length < 9) 
        {
            throw new RecivePacketException("Received packet is invalid.");
        }

        subHeader = bytes.Take(2).ToArray();
        if (!subHeader.SequenceEqual(new byte[] { 0xD0, 0x00 })) 
        {
            throw new RecivePacketException("Received packet had an invalid subheader.");
        }

        route = new RoutePacketHelper(
            networkNumber: bytes[2],
            pcNumber: bytes[3],
            unitNumber: bytes[4],
            ioNumber: bytes[5],
            stationNumber: bytes[6]
        );

        var length = BitConverter.ToUInt16(bytes.Skip(7).Take(2).ToArray(), 0) - 2;

        errCode = BitConverter.ToUInt16(bytes.Skip(9).Take(2).ToArray(), 0);

        if (errCode != 0) 
        {
            throw new McProtocolException($"An error code was received from PLC. ({ErrCode})");
        }

        content = bytes.Skip(11).ToArray();
        if (content.Length != length) 
        {
            throw new RecivePacketException("Received packet had an invalid content.");
        }
    }
}
