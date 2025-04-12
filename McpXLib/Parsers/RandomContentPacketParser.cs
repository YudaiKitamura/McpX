using System.Text;
using McpXLib.Abstructs;
using McpXLib.Exceptions;
using McpXLib.Interfaces;
using McpXLib.Utils;

namespace McpXLib.Parsers;

public class RandomContentPacketParser : BaseContentPacketParser
{
    private int wordLength;
    private int doubleWordLength;

    public RandomContentPacketParser(IPacketParser prevPacketParser, int wordLength, int doubleWordLength, bool isAscii = false) : base (
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
            throw new RecivePacketException("Received packet had an invalid content.");
        }
    }

    internal override byte[] ConvertAsciiBytesToBinalyBytes(byte[] bytes)
    {
        var asciiHex = Encoding.ASCII.GetString(bytes);
        var wordDeviceBytes = DeviceConverter.ReverseByTwoBytes(Enumerable.Range(0, asciiHex.Length / wordLength)
            .Select(i => Convert.ToByte(asciiHex.Substring(i * 2, 2), 16))
            .ToArray()
        );

        asciiHex = Encoding.ASCII.GetString(bytes.Skip(wordLength * 4).Take(doubleWordLength * 8).ToArray());
        var doubleWordDeviceBytes = Enumerable.Range(0, asciiHex.Length / 2)
            .Select(i => Convert.ToByte(asciiHex.Substring(i * 2, 2), 16))
            .Reverse()
            .ToArray();

        doubleWordDeviceBytes = Enumerable.Range(0, doubleWordDeviceBytes.Length / 4)
            .Select(i => doubleWordDeviceBytes.Skip(i * 4).Take(4).ToArray())
            .Reverse()
            .SelectMany(x => x)
            .ToArray();

        return wordDeviceBytes.Concat(doubleWordDeviceBytes).ToArray();
    }
}
