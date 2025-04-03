using McpXLib.Commands;
using McpXLib.Enums;
using McpXLib.Interfaces;
using Moq;
using Bogus;
using McpXLib.Builders;

namespace TestMcpX;

[TestClass]
public sealed class TestMcpX
{
    [TestMethod]
    public void TestToBytes()
    {
        using(var mcpx = new McpXLib.McpX("192.168.12.88", 10000, isAscii: true, isUdp: false))
        {
            mcpx.RandomWrite<short, int>([(Prefix.D, "3", 32767)], [(Prefix.D, "4", 2147483647)]);

            var d = mcpx.RandomRead<short, int>([(Prefix.D, "0"), (Prefix.D, "1")], [(Prefix.D, "2"), (Prefix.D, "4")]);

            mcpx.BatchWrite<float>(Prefix.D, "6", [(float)1.23]);

            var f = mcpx.BatchRead<float>(Prefix.D, "6", 1);
        }

        using(var mcpx = new McpXLib.McpX("192.168.12.88", 10001, isAscii: true, isUdp: true))
        {
            mcpx.RandomWrite<short, int>([(Prefix.D, "3", 32767)], [(Prefix.D, "4", 2147483647)]);

            var d = mcpx.RandomRead<short, int>([(Prefix.D, "0"), (Prefix.D, "1")], [(Prefix.D, "2"), (Prefix.D, "4")]);

            mcpx.BatchWrite<float>(Prefix.D, "6", [(float)1.23]);

            var f = mcpx.BatchRead<float>(Prefix.D, "6", 1);
        }
    }
}
