using System.Text;
using McpXLib.Interfaces;
using McpXLib.Utils;

namespace McpXLib.Abstructs;

internal abstract class BasePacketParser : IPacketParser
{
    internal bool IsAscii;
    internal IPacketParser? PrevPacketParser;
    internal bool? IsReverse;
    internal abstract int BinaryLength { get; }
    internal abstract int AsciiLength { get; }

    internal BasePacketParser(bool isAscii = false, IPacketParser? prevPacketParser = null, bool? isReverse = null)
    {
        IsAscii = isAscii;
        PrevPacketParser = prevPacketParser;
        IsReverse = isReverse;
    }

    public virtual byte[] ParsePacket(byte[] bytes)
    {
        byte[] parseBytes = GetBinaryBytes(bytes);
        if (IsAscii)
        {
            parseBytes = ConvertAsciiBytesToBinalyBytes(parseBytes);
        }

        Validation(parseBytes);

        return parseBytes;
    }

    internal virtual void Validation(byte[] bytes)
    {
    }

    public virtual int GetIndex()
    {
        if (PrevPacketParser == null) 
        {
            return 0;
        }

        return PrevPacketParser.GetIndex() + PrevPacketParser.GetLength();
    }

    public virtual int GetLength()
    {
        return IsAscii ? AsciiLength : BinaryLength;
    }

    internal virtual byte[] GetBinaryBytes(byte[] bytes)
    {
        return bytes.Skip(GetIndex()).Take(GetLength()).ToArray();
    }

    internal virtual byte[] ConvertAsciiBytesToBinalyBytes(byte[] bytes)
    {
        var asciiHex = Encoding.ASCII.GetString(bytes).TrimEnd('\0');
        
        if (IsReverse != null && IsReverse == true) 
        {
            return DeviceConverter.ReverseByTwoBytes(Enumerable.Range(0, asciiHex.Length / 2)
                .Select(i => Convert.ToByte(asciiHex.Substring(i * 2, 2), 16))
                .ToArray()
            );
        }
        else 
        {
            return Enumerable.Range(0, asciiHex.Length / 2)
                .Select(i => Convert.ToByte(asciiHex.Substring(i * 2, 2), 16))
                .ToArray();
        }
    }
}
