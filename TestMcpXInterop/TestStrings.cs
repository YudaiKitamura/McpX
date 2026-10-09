using System.Text;
using McpXLib.Enums;
using McpXNative;
using TestMcpX;

namespace TestMcpXInterop;

[TestClass]
public sealed unsafe class TestStrings
{
    private const string Text = "三菱PLCテスト";

    // "三菱PLCテスト" の Shift_JIS（13バイト）＋ 終端 NUL を、ワード（リトルエンディアン）に詰めた値
    private static readonly ushort[] ShiftJisWords = ToWords(Convert.FromHexString("8E4F9548504C4383658358836700"));

    private static ushort[] ToWords(byte[] bytes)
    {
        var words = new ushort[bytes.Length / 2];
        for (int i = 0; i < words.Length; i++)
        {
            words[i] = (ushort)(bytes[i * 2] | bytes[i * 2 + 1] << 8);
        }
        return words;
    }

    [TestMethod]
    public void TestWriteStringSendsShiftJis()
    {
        var transport = new FakePlcTransport();
        var client = Native.Register(transport);
        var err = Native.NewError();

        Assert.AreEqual((int)McpxStatus.Ok, Native.WriteString(client, Prefix.D, "0", Text, &err), Native.Message(err));

        CollectionAssert.AreEqual(Convert.FromHexString("8E4F9548504C4383658358836700"), transport.Requests[0].Data);
    }

    [TestMethod]
    public void TestReadStringReturnsUtf8()
    {
        var transport = new FakePlcTransport();
        for (uint i = 0; i < ShiftJisWords.Length; i++)
        {
            transport.Words[i] = ShiftJisWords[i];
        }
        var client = Native.Register(transport);
        var err = Native.NewError();

        var buffer = new byte[10 * 6 + 1];
        nuint length = 0;
        fixed (byte* p = buffer)
        {
            Assert.AreEqual((int)McpxStatus.Ok, Native.ReadString(client, Prefix.D, "0", 10, p, (nuint)buffer.Length, &length, &err), Native.Message(err));
        }

        var expected = Encoding.UTF8.GetBytes(Text);
        Assert.AreEqual((nuint)expected.Length, length);
        Assert.AreEqual(Text, Encoding.UTF8.GetString(buffer, 0, (int)length));
        Assert.AreEqual((byte)0, buffer[(int)length]);
    }

    [TestMethod]
    public void TestReadStringReportsRequiredSize()
    {
        var transport = new FakePlcTransport();
        for (uint i = 0; i < ShiftJisWords.Length; i++)
        {
            transport.Words[i] = ShiftJisWords[i];
        }
        var client = Native.Register(transport);
        var err = Native.NewError();

        var small = new byte[4];
        nuint length = 0;
        fixed (byte* p = small)
        {
            Assert.AreEqual((int)McpxStatus.BufferTooSmall, Native.ReadString(client, Prefix.D, "0", 10, p, (nuint)small.Length, &length, &err));
        }

        // 必要なバイト数（NUL を除く）が返ること
        Assert.AreEqual((nuint)Encoding.UTF8.GetByteCount(Text), length);
    }

    [TestMethod]
    public void TestInvalidStringArguments()
    {
        var client = Native.Register(new FakePlcTransport());
        var err = Native.NewError();
        var buffer = new byte[8];
        nuint length = 0;

        fixed (byte* p = buffer)
        {
            Assert.AreEqual((int)McpxStatus.InvalidArgument, Native.ReadString(client, Prefix.D, "0", 0, p, 8, &length, &err));
            Assert.AreEqual((int)McpxStatus.InvalidArgument, Native.ReadString(client, Prefix.D, "0", 1, null, 8, &length, &err));
        }

        var f = (delegate* unmanaged[Cdecl]<ulong, byte, byte*, byte*, NativeError*, int>)&StringExports.WriteString;
        fixed (byte* a = Native.Utf8Z("0"))
        {
            Assert.AreEqual((int)McpxStatus.InvalidArgument, f(client, (byte)Prefix.D, a, null, &err));
        }
    }
}
