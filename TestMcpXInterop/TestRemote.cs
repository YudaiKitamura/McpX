using McpXNative;
using TestMcpX;

namespace TestMcpXInterop;

[TestClass]
public sealed unsafe class TestRemote
{
    [TestMethod]
    public void TestRemoteOperationsSendCommands()
    {
        var transport = new FakePlcTransport();
        var client = Native.Register(transport);
        var err = Native.NewError();

        Assert.AreEqual((int)McpxStatus.Ok, Native.RemoteRun(client, 1, 2, &err), Native.Message(err));
        Assert.AreEqual((int)McpxStatus.Ok, Native.RemoteStop(client, &err), Native.Message(err));
        Assert.AreEqual((int)McpxStatus.Ok, Native.RemotePause(client, 0, &err), Native.Message(err));
        Assert.AreEqual((int)McpxStatus.Ok, Native.RemoteLatchClear(client, &err), Native.Message(err));
        // 偽のトランスポートは再接続できないため、RESET は送信だけ行う
        Assert.AreEqual((int)McpxStatus.Ok, Native.RemoteReset(client, -1, &err), Native.Message(err));

        CollectionAssert.AreEqual(
            new ushort[] { 0x1001, 0x1002, 0x1003, 0x1005, 0x1006 },
            transport.Requests.Select(r => r.Command).ToArray()
        );
    }

    [TestMethod]
    public void TestInvalidRemoteArguments()
    {
        var client = Native.Register(new FakePlcTransport());
        var err = Native.NewError();

        Assert.AreEqual((int)McpxStatus.InvalidArgument, Native.RemoteRun(client, 0, 3, &err));
        Assert.AreEqual((int)McpxStatus.InvalidHandle, Native.RemoteStop(987654321, &err));
    }

    [TestMethod]
    public void TestRemoteOperationProtocolError()
    {
        var transport = new FakePlcTransport();
        transport.EndCodes[0x1001] = 0x4013;
        var client = Native.Register(transport);
        var err = Native.NewError();

        Assert.AreEqual((int)McpxStatus.Protocol, Native.RemoteRun(client, 0, 0, &err));
        Assert.AreEqual((ushort)0x4013, err.EndCode);
    }
}
