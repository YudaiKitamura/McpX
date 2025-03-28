using McpXLib.Interfaces;
using McpXLib.Helpers;
using McpXLib.Exceptions;
using System.Text;
using McpXLib.Utils;

namespace McpXLib.Abstructs;

public abstract class BaseAsciiPacketHelper : I3EResponseFrame
{
    public byte[] SubHeader => subHeader;
    public IRoute Route => route;
    public byte[] Content => content;
    public string ErrCode => errCode.ToString("X");

    internal readonly byte[] subHeader;
    internal readonly IRoute route;
    internal byte[] content;
    internal readonly uint errCode;

    public BaseAsciiPacketHelper(byte[] bytes)
    {
        if (bytes.Length < 22) 
        {
            throw new RecivePacketException("Received packet is invalid.");
        }

        subHeader = bytes.Take(4).ToArray();
        if (!subHeader.SequenceEqual(Encoding.ASCII.GetBytes("D000"))) 
        {
            throw new RecivePacketException("Received packet had an invalid subheader.");
        }

        route = new RouteAsciiPacketHelper(
            networkNumber: Convert.ToByte(Encoding.ASCII.GetString(bytes.Skip(4).Take(2).ToArray()), 16),
            pcNumber: Convert.ToByte(Encoding.ASCII.GetString(bytes.Skip(6).Take(2).ToArray()), 16),
            ioNumber: Convert.ToByte(Encoding.ASCII.GetString(bytes.Skip(8).Take(2).ToArray()), 16),
            unitNumber: Convert.ToByte(Encoding.ASCII.GetString(bytes.Skip(10).Take(2).ToArray()), 16),
            stationNumber: Convert.ToByte(Encoding.ASCII.GetString(bytes.Skip(12).Take(2).ToArray()), 16)
        );

        var contentLength = Convert.ToUInt16(Encoding.ASCII.GetString(bytes.Skip(14).Take(4).ToArray()), 16) - 4;

        errCode = Convert.ToUInt16(Encoding.ASCII.GetString(bytes.Skip(18).Take(4).ToArray()), 16);

        if (errCode != 0) 
        {
            throw new McProtocolException($"An error code was received from PLC. ({ErrCode})");
        }

        content = GetContent(bytes, contentLength);
        ValidateContentLength(contentLength);
    }

    public virtual void ValidateContentLength(int contentLength)
    {
        if (content.Length != contentLength / 2) 
        {
            throw new RecivePacketException("Received packet had an invalid content.");
        }
    }

    public virtual byte[] GetContent(byte[] bytes, int contentLength)
    {
        var asciiHex = Encoding.ASCII.GetString(bytes.Skip(22).Take(contentLength).ToArray());
        return DeviceConverter.ReverseByTwoBytes(Enumerable.Range(0, asciiHex.Length / 2)
            .Select(i => Convert.ToByte(asciiHex.Substring(i * 2, 2), 16))
            .ToArray()
        );
    } 
}
