using System.Diagnostics;
using McpXLib.Enums;
using McpXNative;
using TestMcpX;

namespace TestMcpXInterop;

[TestClass]
public sealed unsafe class TestConcurrency
{
    private static Task<(int status, NativeError err)> ReadInBackground(ulong client)
    {
        return Task.Run(() =>
        {
            var err = Native.NewError();
            short value = 0;
            int status = Native.Read(client, Prefix.D, "0", McpxType.I16, &value, 2, &err);
            return (status, err);
        });
    }

    [TestMethod]
    [Timeout(10000)]
    public void TestOtherClientIsNotBlockedWhileOneWaits()
    {
        // 旧 Interop は全接続を1つのロックで直列化していた（D3）。1台が応答待ちでも、別の接続は進めること。
        var blocking = new BlockingTransport(TimeSpan.FromSeconds(2));
        var slow = Native.Register(blocking);
        var fast = Native.Register(new FakePlcTransport());

        var slowRead = ReadInBackground(slow);
        Assert.IsTrue(blocking.Entered.Wait(5000));

        var elapsed = Stopwatch.StartNew();
        var err = Native.NewError();
        short value = 0;
        Assert.AreEqual((int)McpxStatus.Ok, Native.Read(fast, Prefix.D, "0", McpxType.I16, &value, 2, &err), Native.Message(err));
        elapsed.Stop();

        Assert.IsFalse(slowRead.IsCompleted);
        Assert.IsTrue(elapsed.ElapsedMilliseconds < 1000, $"elapsed: {elapsed.ElapsedMilliseconds}ms");

        Assert.AreEqual((int)McpxStatus.Timeout, slowRead.Result.status);
    }

    [TestMethod]
    [Timeout(10000)]
    public void TestCloseDuringCallReturnsClosed()
    {
        var blocking = new BlockingTransport();
        var client = Native.Register(blocking);

        var read = ReadInBackground(client);
        Assert.IsTrue(blocking.Entered.Wait(5000));

        var err = Native.NewError();
        Assert.AreEqual((int)McpxStatus.Ok, Native.Close(client, &err));

        // 通信中の呼び出しは、close によって速やかに MCPX_E_CLOSED で戻ること
        Assert.IsTrue(read.Wait(3000));
        Assert.AreEqual((int)McpxStatus.Closed, read.Result.status, Native.Message(read.Result.err));
    }
}
