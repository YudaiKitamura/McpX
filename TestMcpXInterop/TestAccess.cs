using McpXLib.Enums;
using McpXNative;
using TestMcpX;

namespace TestMcpXInterop;

[TestClass]
public sealed unsafe class TestAccess
{
    [TestMethod]
    public void TestBatchReadWords()
    {
        var transport = new FakePlcTransport();
        var client = Native.Register(transport);
        var err = Native.NewError();
        var values = new short[5];

        fixed (short* p = values)
        {
            var status = Native.BatchRead(client, Prefix.D, "100", McpxType.I16, 5, p, (nuint)(values.Length * 2), &err);
            Assert.AreEqual((int)McpxStatus.Ok, status, Native.Message(err));
        }

        // ダミー PLC はデバイス番号をそのまま値として返す
        CollectionAssert.AreEqual(new short[] { 100, 101, 102, 103, 104 }, values);
        Assert.AreEqual((int)McpxStatus.Ok, err.Status);
        Assert.AreEqual(string.Empty, Native.Message(err));
    }

    [TestMethod]
    public void TestReadDoubleWord()
    {
        var client = Native.Register(new FakePlcTransport());
        var err = Native.NewError();
        int value = 0;

        Assert.AreEqual((int)McpxStatus.Ok, Native.Read(client, Prefix.D, "10", McpxType.I32, &value, sizeof(int), &err), Native.Message(err));

        // D10 = 10（下位ワード）、D11 = 11（上位ワード）
        Assert.AreEqual((11 << 16) | 10, value);
    }

    [TestMethod]
    public void TestBatchWriteAndWrite()
    {
        var transport = new FakePlcTransport();
        var client = Native.Register(transport);
        var err = Native.NewError();
        var values = new int[] { 0x01020304, -1 };

        fixed (int* p = values)
        {
            Assert.AreEqual((int)McpxStatus.Ok, Native.BatchWrite(client, Prefix.D, "200", McpxType.I32, 2, p, 8, &err), Native.Message(err));
        }

        byte bit = 1;
        Assert.AreEqual((int)McpxStatus.Ok, Native.Write(client, Prefix.M, "0", McpxType.Bool, &bit, 1, &err), Native.Message(err));

        Assert.AreEqual(200u, transport.Requests[0].DeviceNumber);
        Assert.AreEqual((ushort)4, transport.Requests[0].Points);
        CollectionAssert.AreEqual(new byte[] { 0x04, 0x03, 0x02, 0x01, 0xFF, 0xFF, 0xFF, 0xFF }, transport.Requests[0].Data);
        Assert.AreEqual(2, transport.Requests.Count);
    }

    [TestMethod]
    public void TestInvalidArguments()
    {
        var client = Native.Register(new FakePlcTransport());
        var err = Native.NewError();
        var buffer = new short[4];

        fixed (short* p = buffer)
        {
            // 未知のデバイスコード・型
            Assert.AreEqual((int)McpxStatus.InvalidArgument, Native.BatchRead(client, (Prefix)0x01, "0", McpxType.I16, 1, p, 8, &err));
            Assert.AreEqual((int)McpxStatus.InvalidArgument, Native.BatchRead(client, Prefix.D, "0", (McpxType)0, 1, p, 8, &err));
            Assert.AreEqual((int)McpxStatus.InvalidArgument, Native.BatchRead(client, Prefix.D, "0", (McpxType)12, 1, p, 8, &err));

            // 点数の範囲外
            Assert.AreEqual((int)McpxStatus.InvalidArgument, Native.BatchRead(client, Prefix.D, "0", McpxType.I16, 0, p, 8, &err));
            Assert.AreEqual((int)McpxStatus.InvalidArgument, Native.BatchRead(client, Prefix.D, "0", McpxType.I16, 65536, p, 8, &err));

            // バッファが小さい・NULL
            Assert.AreEqual((int)McpxStatus.BufferTooSmall, Native.BatchRead(client, Prefix.D, "0", McpxType.I16, 5, p, 8, &err));
            Assert.AreEqual((int)McpxStatus.InvalidArgument, Native.BatchRead(client, Prefix.D, "0", McpxType.I16, 1, null, 8, &err));

            // アドレスが NULL・空・長すぎる
            Assert.AreEqual((int)McpxStatus.InvalidArgument, Native.BatchRead(client, (byte)Prefix.D, null, (byte)McpxType.I16, 1, p, 8, &err));
            Assert.AreEqual((int)McpxStatus.InvalidArgument, Native.BatchRead(client, Prefix.D, "", McpxType.I16, 1, p, 8, &err));
            Assert.AreEqual((int)McpxStatus.InvalidArgument, Native.BatchRead(client, Prefix.D, new string('1', 65), McpxType.I16, 1, p, 8, &err));
        }

        Assert.AreEqual((int)McpxStatus.InvalidArgument, err.Status);
        StringAssert.Contains(Native.Message(err), "address");
    }

    [TestMethod]
    public void TestInvalidDeviceAddress()
    {
        var client = Native.Register(new FakePlcTransport());
        var err = Native.NewError();
        short value = 0;

        // D は10進デバイスのため、16進の文字を含むアドレスは不正
        var status = Native.Read(client, Prefix.D, "1A", McpxType.I16, &value, 2, &err);

        Assert.AreEqual((int)McpxStatus.InvalidDeviceAddress, status);
        StringAssert.StartsWith(Native.Message(err), "DeviceAddressException");
    }
}
