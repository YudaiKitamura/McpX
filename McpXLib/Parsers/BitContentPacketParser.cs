using McpXLib.Abstracts;
using McpXLib.Exceptions;

namespace McpXLib.Parsers;

internal class BitContentPacketParser : BaseContentPacketParser
{
    internal BitContentPacketParser(BasePacketParser prevPacketParser, bool isAscii = false) : base (
        isAscii: isAscii,
        prevPacketParser: prevPacketParser
    )
    {
    }

    internal override void Validation(byte[] bytes)
    {
        // MEMO:
        //  ASCIIの場合は、一度、バイナリに変換して処理を行う為、半分のパケット数になる
        var length = Length;
        if (IsAscii) 
        {
            length = length % 2 == 0 ? length / 2 : length / 2 + 1; 
        }

        if (bytes.Length != length) 
        {
            throw new ReceivePacketException("Received packet had an invalid content.");
        }
    }

    internal override byte[] ConvertAsciiBytesToBinaryBytes(byte[] bytes)
    {
        // ASCII は1点1文字（'0' / '1'）。バイナリと同じく、2点を1バイト（上位4ビット・下位4ビット）にまとめる。
        int length = bytes.Length;
        while (length > 0 && bytes[length - 1] == 0)
        {
            length--;
        }

        var content = new byte[(length + 1) / 2];
        for (int i = 0; i < length; i++)
        {
            if (bytes[i] == '1')
            {
                content[i / 2] |= (byte)(i % 2 == 0 ? 0x10 : 0x01);
            }
        }

        return content;
    }

    public override byte[] ParsePacket(byte[] bytes)
    {
        var valueList = new List<byte>();
        foreach (var value in base.ParsePacket(bytes)) 
        {
            if ((value & 0x10) != 0) 
            {
                valueList.Add(0x01);
            }
            else 
            {
                valueList.Add(0x00);
            }

            if ((value & 0x01) != 0) 
            {
                valueList.Add(0x01);
            }
            else 
            {
                valueList.Add(0x00);
            }
        }

        return valueList.ToArray();
    }
}
