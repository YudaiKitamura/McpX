using System.Text;
using McpXLib.Abstructs;
using McpXLib.Exceptions;
using McpXLib.Interfaces;

namespace McpXLib.Parsers;

public class BitContentPacketParser : BaseContentPacketParser
{
    public BitContentPacketParser(IPacketParser prevPacketParser, bool isAscii = false) : base (
        isAscii: isAscii,
        prevPacketParser: prevPacketParser
    )
    {
    }

    internal override void Validation(byte[] bytes)
    {
        // MEMO:
        //  ASCIIの場合は、一度、バイナリに変換して処理を行う為、半分のパケット数になる
        if (bytes.Length != (!IsAscii ? Length : Length / 2)) 
        {
            throw new RecivePacketException("Received packet had an invalid content.");
        }
    }

    internal override byte[] ConvertAsciiBytesToBinalyBytes(byte[] bytes)
    {
        var asciiHex = Encoding.ASCII.GetString(bytes).TrimEnd('\0');
        
        List<byte> contentList = new List<byte>();
        for (int i = 0; i < asciiHex.Length; i++)
        {
            if (i % 2 == 0) 
            {
                contentList.Add(asciiHex.Substring(i, 1) == "1" ? (byte)0x10 : (byte)0x00);
            }
            else
            {
                int lastIndex = contentList.Count - 1;
                byte flag = asciiHex.Substring(i, 1) == "1" ? (byte)0x01 : (byte)0x00;
                contentList[lastIndex] = (byte)(contentList.Last() | flag);
            }
        }

        return contentList.ToArray();
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
