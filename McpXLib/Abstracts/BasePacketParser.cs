using McpXLib.Exceptions;

namespace McpXLib.Abstructs;

public abstract class BasePacketParser
{
    public byte[] Content => content;
    public string ErrCode => errCode.ToString("X");
    internal byte[] content;
    internal readonly uint errCode;

    internal const int SUBHEADER_LENGTH = 2;
    internal const int DATA_LENGTH_INDEX = 7;
    internal const int DATA_LENGTH_LENGTH = 2;
    internal const int ERROR_CODE_INDEX = 9;
    internal const int ERROR_CODE_LENGTH = 2;
    internal const int CONTENT_INDEX = 11;

    public BasePacketParser(byte[] bytes)
    {
        if (bytes.Length < CONTENT_INDEX - 1) 
        {
            throw new RecivePacketException("Received packet is invalid.");
        }

        var subHeader = bytes.Take(SUBHEADER_LENGTH).ToArray();
        if (!subHeader.SequenceEqual(new byte[] { 0xD0, 0x00 })) 
        {
            throw new RecivePacketException("Received packet had an invalid subheader.");
        }

        errCode = BitConverter.ToUInt16(bytes.Skip(ERROR_CODE_INDEX).Take(ERROR_CODE_LENGTH).ToArray(), 0);
        if (errCode != 0) 
        {
            throw new McProtocolException($"An error code was received from PLC. ({ErrCode})");
        }

        var length = BitConverter.ToUInt16(bytes.Skip(DATA_LENGTH_INDEX).Take(DATA_LENGTH_LENGTH).ToArray(), 0) - ERROR_CODE_LENGTH;
        content = bytes.Skip(CONTENT_INDEX).ToArray();
        if (content.Length != length) 
        {
            throw new RecivePacketException("Received packet had an invalid content.");
        }
    }
}
