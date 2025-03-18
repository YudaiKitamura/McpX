using McpXLib.Abstructs;

namespace McpXLib.Helpers;

public sealed class BitResponsePacketHelper : BasePacketHelper
{
    public BitResponsePacketHelper(byte[] bytes) : base(bytes)
    {
        var valueList = new List<byte>();
        foreach (var value in content) 
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

        content = valueList.ToArray();
    }
}
