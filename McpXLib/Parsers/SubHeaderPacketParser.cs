using McpXLib.Abstructs;
using McpXLib.Enums;
using McpXLib.Exceptions;
using McpXLib.Interfaces;

namespace McpXLib.Parsers;

internal class SubHeaderPacketParser : BasePacketParser
{    
    internal override int BinaryLength => 2;
    internal override int AsciiLength => 4;
    private RequestFrame requestFrame;

    internal SubHeaderPacketParser(RequestFrame requestFrame = RequestFrame.E3, bool isAscii = false, IPacketParser? prevPacketParser = null) : base(
        isAscii: isAscii,
        prevPacketParser: prevPacketParser,
        isReverse: false
    )
    {
        this.requestFrame = requestFrame;
    }

    internal override void Validation(byte[] subHeader)
    {
        if (!subHeader.SequenceEqual(requestFrame == RequestFrame.E3 ? new byte[] { 0xD0, 0x00 } : new byte[] { 0xD4, 0x00 })) 
        {
            throw new RecivePacketException("Received packet had an invalid subheader.");
        }
    }
}
