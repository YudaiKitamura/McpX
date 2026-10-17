using McpXLib.Abstracts;
using McpXLib.Exceptions;
using McpXLib.Utils;

namespace McpXLib.Parsers;

internal class RandomContentPacketParser : BaseContentPacketParser
{
    private int wordLength;
    private int doubleWordLength;

    internal RandomContentPacketParser(BasePacketParser prevPacketParser, int wordLength, int doubleWordLength, bool isAscii = false) : base (
        isAscii: isAscii,
        prevPacketParser: prevPacketParser
    )
    {
        this.wordLength = wordLength;
        this.doubleWordLength = doubleWordLength;
    }

    internal override void Validation(byte[] bytes)
    {
        // MEMO:
        //  ASCIIの場合は、一度、バイナリに変換して処理を行う為、半分のパケット数になる
        if (bytes.Length != (!IsAscii ? Length : Length / 2)) 
        {
            throw new ReceivePacketException("Received packet had an invalid content.");
        }
    }

    internal override byte[] ConvertAsciiBytesToBinaryBytes(byte[] bytes)
    {
        // ASCII はワード4桁・ダブルワード8桁の16進（上位桁から）。バイナリと同じリトルエンディアンのバイト列にする。
        int words = Math.Min(wordLength, bytes.Length / 4);
        int doubleWords = Math.Min(doubleWordLength, Math.Max(0, bytes.Length - wordLength * 4) / 8);

        var result = new byte[words * 2 + doubleWords * 4];
        for (int i = 0; i < words; i++)
        {
            WriteLittleEndian(result, i * 2, ParseHex(bytes, i * 4, 4), 2);
        }

        for (int i = 0; i < doubleWords; i++)
        {
            WriteLittleEndian(result, words * 2 + i * 4, ParseHex(bytes, wordLength * 4 + i * 8, 8), 4);
        }

        return result;
    }

    private static uint ParseHex(byte[] bytes, int offset, int digits)
    {
        uint value = 0;
        for (int i = 0; i < digits; i++)
        {
            value = value << 4 | (uint)ParseHexDigit(bytes[offset + i]);
        }

        return value;
    }

    private static void WriteLittleEndian(byte[] buffer, int offset, uint value, int size)
    {
        for (int i = 0; i < size; i++)
        {
            buffer[offset + i] = (byte)(value >> (8 * i));
        }
    }
}
