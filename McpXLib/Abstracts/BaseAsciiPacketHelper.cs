using McpXLib.Exceptions;
using System.Text;
using McpXLib.Utils;

namespace McpXLib.Abstructs;

public abstract class BaseAsciiPacketHelper
{
    public byte[] Content => content;
    public string ErrCode => errCode.ToString("X");
    internal byte[] content;
    internal readonly uint errCode;

    internal const int SUBHEADER_LENGTH = 4;
    internal const int DATA_LENGTH_INDEX = 14;
    internal const int DATA_LENGTH_LENGTH = 4;
    internal const int ERROR_CODE_INDEX = 18;
    internal const int ERROR_CODE_LENGTH = 4;
    internal const int CONTENT_INDEX = 22;

    public BaseAsciiPacketHelper(byte[] bytes)
    {
        if (bytes.Length < CONTENT_INDEX - 1) 
        {
            throw new RecivePacketException("Received packet is invalid.");
        }

        var subHeader = bytes.Take(SUBHEADER_LENGTH).ToArray();
        if (!subHeader.SequenceEqual(Encoding.ASCII.GetBytes("D000"))) 
        {
            throw new RecivePacketException("Received packet had an invalid subheader.");
        }

        errCode = Convert.ToUInt16(Encoding.ASCII.GetString(bytes.Skip(ERROR_CODE_INDEX).Take(ERROR_CODE_LENGTH).ToArray()), 16);
        if (errCode != 0) 
        {
            throw new McProtocolException($"An error code was received from PLC. ({ErrCode})");
        }

        var contentLength = Convert.ToUInt16(Encoding.ASCII.GetString(bytes.Skip(DATA_LENGTH_INDEX).Take(DATA_LENGTH_LENGTH).ToArray()), 16) - ERROR_CODE_LENGTH;
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
        var asciiHex = Encoding.ASCII.GetString(bytes.Skip(CONTENT_INDEX).Take(contentLength).ToArray());
        return DeviceConverter.ReverseByTwoBytes(Enumerable.Range(0, asciiHex.Length / 2)
            .Select(i => Convert.ToByte(asciiHex.Substring(i * 2, 2), 16))
            .ToArray()
        );
    } 
}
