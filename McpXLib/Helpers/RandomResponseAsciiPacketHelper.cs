using System.Text;
using McpXLib.Abstructs;
using McpXLib.Exceptions;
using McpXLib.Utils;

namespace McpXLib.Helpers;

public sealed class RandomResponseAsciiPacketHelper(byte[] bytes, int wordLength, int doubleWordLength) : BaseAsciiPacketHelper(bytes)
{
    public override void ValidateContentLength(int contentLength)
    {
        if (content.Length != contentLength / 2) 
        {
            throw new RecivePacketException("Received packet had an invalid content.");
        }
    }

    public override byte[] GetContent(byte[] bytes, int contentLength)
    {
        var asciiHex = Encoding.ASCII.GetString(bytes.Skip(CONTENT_INDEX).Take(wordLength * 4).ToArray());
        var wordDeviceBytes = DeviceConverter.ReverseByTwoBytes(Enumerable.Range(0, asciiHex.Length / wordLength)
            .Select(i => Convert.ToByte(asciiHex.Substring(i * 2, 2), 16))
            .ToArray()
        );

        asciiHex = Encoding.ASCII.GetString(bytes.Skip(CONTENT_INDEX + (wordLength * 4)).Take(doubleWordLength * 8).ToArray());
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
