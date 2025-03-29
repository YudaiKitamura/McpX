using System.Text;
using McpXLib.Abstructs;
using McpXLib.Exceptions;

namespace McpXLib.Helpers;

public sealed class BitResponseAsciiPacketHelper(byte[] bytes) : BaseAsciiPacketHelper(bytes)
{
    public override void ValidateContentLength(int contentLength)
    {
        if (content.Length != contentLength) 
        {
            throw new RecivePacketException("Received packet had an invalid content.");
        }
    }

    public override byte[] GetContent(byte[] bytes, int contentLength)
    {
        var asciiHex = Encoding.ASCII.GetString(bytes.Skip(22).Take(contentLength).ToArray());
        List<byte> contentList = new List<byte>();
        for (int i = 0; i < asciiHex.Length; i++) 
        {
            contentList.Add(asciiHex.Substring(i, 1) == "1" ? (byte)0x01 : (byte)0x00);
        }

        return contentList.ToArray();
    } 
}
