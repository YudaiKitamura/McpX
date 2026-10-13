using System.Text;
using McpXLib.Utils;

namespace McpXLib.Abstracts;

internal abstract class BasePacketParser
{
    internal bool IsAscii;
    internal BasePacketParser? PrevPacketParser;
    internal bool? IsReverse;
    internal abstract int BinaryLength { get; }
    internal abstract int AsciiLength { get; }

    internal BasePacketParser(bool isAscii = false, BasePacketParser? prevPacketParser = null, bool? isReverse = null)
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
            parseBytes = ConvertAsciiBytesToBinaryBytes(parseBytes);
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
        // 応答が短い場合は、ある分だけを返す（長さの検証は各解析で行う）
        int index = GetIndex();
        int length = Math.Max(0, Math.Min(GetLength(), bytes.Length - index));
        var result = new byte[length];
        Array.Copy(bytes, index, result, 0, length);
        return result;
    }

    internal virtual byte[] ConvertAsciiBytesToBinaryBytes(byte[] bytes)
    {
        // 末尾の NUL は除く
        int length = bytes.Length;
        while (length > 0 && bytes[length - 1] == 0)
        {
            length--;
        }

        var result = new byte[length / 2];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = (byte)(ParseHexDigit(bytes[i * 2]) << 4 | ParseHexDigit(bytes[i * 2 + 1]));
        }

        return IsReverse == true ? DeviceConverter.ReverseByTwoBytes(result) : result;
    }

    protected static int ParseHexDigit(byte c)
    {
        if (c >= '0' && c <= '9') return c - '0';
        if (c >= 'A' && c <= 'F') return c - 'A' + 10;
        if (c >= 'a' && c <= 'f') return c - 'a' + 10;
        throw new FormatException($"'{(char)c}' is not a hexadecimal digit.");
    }
}
