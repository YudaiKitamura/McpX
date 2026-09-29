using System.Runtime.InteropServices;
using System.Text;
using McpXLib;
using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXNative;

namespace TestMcpXInterop;

/// <summary>
/// C ABI の関数を、ネイティブからの呼び出しと同じく関数ポインタ（cdecl）経由で呼ぶテスト用ヘルパー。
/// </summary>
internal static unsafe class Native
{
    internal static NativeError NewError()
    {
        var err = default(NativeError);
        err.StructSize = (uint)sizeof(NativeError);
        return err;
    }

    internal static string Message(in NativeError err)
    {
        fixed (byte* p = err.Message)
        {
            return Marshal.PtrToStringUTF8((IntPtr)p) ?? string.Empty;
        }
    }

    internal static byte[] Utf8Z(string text) => Encoding.UTF8.GetBytes(text + "\0");

    // テスト用：偽のトランスポートを使う McpX をハンドル表に登録する
    internal static ulong Register(IPlcTransport transport) => HandleTable.Add(new McpX(transport));

    internal static int Read(ulong client, Prefix prefix, string address, McpxType type, void* output, nuint size, NativeError* err)
    {
        fixed (byte* a = Utf8Z(address))
        {
            var f = (delegate* unmanaged[Cdecl]<ulong, byte, byte*, byte, void*, nuint, NativeError*, int>)&AccessExports.Read;
            return f(client, (byte)prefix, a, (byte)type, output, size, err);
        }
    }

    internal static int Write(ulong client, Prefix prefix, string address, McpxType type, void* value, nuint size, NativeError* err)
    {
        fixed (byte* a = Utf8Z(address))
        {
            var f = (delegate* unmanaged[Cdecl]<ulong, byte, byte*, byte, void*, nuint, NativeError*, int>)&AccessExports.Write;
            return f(client, (byte)prefix, a, (byte)type, value, size, err);
        }
    }

    internal static int BatchRead(ulong client, byte prefix, byte* address, byte type, uint count, void* output, nuint size, NativeError* err)
    {
        var f = (delegate* unmanaged[Cdecl]<ulong, byte, byte*, byte, uint, void*, nuint, NativeError*, int>)&AccessExports.BatchRead;
        return f(client, prefix, address, type, count, output, size, err);
    }

    internal static int BatchRead(ulong client, Prefix prefix, string address, McpxType type, uint count, void* output, nuint size, NativeError* err)
    {
        fixed (byte* a = Utf8Z(address))
        {
            return BatchRead(client, (byte)prefix, a, (byte)type, count, output, size, err);
        }
    }

    internal static int BatchWrite(ulong client, Prefix prefix, string address, McpxType type, uint count, void* values, nuint size, NativeError* err)
    {
        fixed (byte* a = Utf8Z(address))
        {
            var f = (delegate* unmanaged[Cdecl]<ulong, byte, byte*, byte, uint, void*, nuint, NativeError*, int>)&AccessExports.BatchWrite;
            return f(client, (byte)prefix, a, (byte)type, count, values, size, err);
        }
    }

    internal static int ReadItems(ulong client, NativeItem* items, nuint count, NativeError* err)
        => ((delegate* unmanaged[Cdecl]<ulong, NativeItem*, nuint, NativeError*, int>)&ItemExports.ReadItems)(client, items, count, err);

    internal static int WriteItems(ulong client, NativeItem* items, nuint count, NativeError* err)
        => ((delegate* unmanaged[Cdecl]<ulong, NativeItem*, nuint, NativeError*, int>)&ItemExports.WriteItems)(client, items, count, err);

    internal static int BlockRead(ulong client, NativeItem* items, nuint count, NativeError* err)
        => ((delegate* unmanaged[Cdecl]<ulong, NativeItem*, nuint, NativeError*, int>)&ItemExports.BlockRead)(client, items, count, err);

    internal static int BlockWrite(ulong client, NativeItem* items, nuint count, NativeError* err)
        => ((delegate* unmanaged[Cdecl]<ulong, NativeItem*, nuint, NativeError*, int>)&ItemExports.BlockWrite)(client, items, count, err);

    internal static int ReadString(ulong client, Prefix prefix, string address, uint words, byte* output, nuint size, nuint* length, NativeError* err)
    {
        fixed (byte* a = Utf8Z(address))
        {
            var f = (delegate* unmanaged[Cdecl]<ulong, byte, byte*, uint, byte*, nuint, nuint*, NativeError*, int>)&StringExports.ReadString;
            return f(client, (byte)prefix, a, words, output, size, length, err);
        }
    }

    internal static int WriteString(ulong client, Prefix prefix, string address, string value, NativeError* err)
    {
        fixed (byte* a = Utf8Z(address))
        fixed (byte* v = Utf8Z(value))
        {
            var f = (delegate* unmanaged[Cdecl]<ulong, byte, byte*, byte*, NativeError*, int>)&StringExports.WriteString;
            return f(client, (byte)prefix, a, v, err);
        }
    }

    internal static int Close(ulong client, NativeError* err)
    {
        var f = (delegate* unmanaged[Cdecl]<ulong, NativeError*, int>)&ClientExports.Close;
        return f(client, err);
    }

    internal static int Connect(NativeConnectOptions* options, ulong* output, NativeError* err)
    {
        var f = (delegate* unmanaged[Cdecl]<NativeConnectOptions*, ulong*, NativeError*, int>)&ClientExports.Connect;
        return f(options, output, err);
    }

    internal static void ConnectOptionsInit(NativeConnectOptions* options)
    {
        var f = (delegate* unmanaged[Cdecl]<NativeConnectOptions*, void>)&LibraryExports.ConnectOptionsInit;
        f(options);
    }

    internal static void SimulatorOptionsInit(NativeSimulatorOptions* options)
    {
        var f = (delegate* unmanaged[Cdecl]<NativeSimulatorOptions*, void>)&LibraryExports.SimulatorOptionsInit;
        f(options);
    }

    internal static uint AbiVersion() => ((delegate* unmanaged[Cdecl]<uint>)&LibraryExports.GetAbiVersion)();

    internal static string Version() => Marshal.PtrToStringUTF8((IntPtr)((delegate* unmanaged[Cdecl]<byte*>)&LibraryExports.GetVersion)()) ?? string.Empty;

    internal static uint StructSize(uint which) => ((delegate* unmanaged[Cdecl]<uint, uint>)&LibraryExports.GetStructSize)(which);
}

/// <summary>
/// 応答を返さないトランスポート。Dispose されると ObjectDisposedException、timeout を過ぎると TimeoutException で戻る。
/// </summary>
internal sealed class BlockingTransport(TimeSpan? timeout = null) : IPlcTransport
{
    private readonly ManualResetEventSlim released = new();

    internal ManualResetEventSlim Entered { get; } = new();

    public byte[] Request(byte[] packet, IReceiveLengthParser receiveLengthParser)
    {
        Entered.Set();
        if (released.Wait(timeout ?? Timeout.InfiniteTimeSpan))
        {
            throw new ObjectDisposedException(nameof(BlockingTransport));
        }

        throw new TimeoutException("The request timed out.", new System.Net.Sockets.SocketException((int)System.Net.Sockets.SocketError.TimedOut));
    }

    public Task<byte[]> RequestAsync(byte[] packet, IReceiveLengthParser receiveLengthParser)
        => Task.Run(() => Request(packet, receiveLengthParser));

    public void Dispose() => released.Set();
}

/// <summary>
/// 要求のたびに指定した例外をスローするトランスポート。
/// </summary>
internal sealed class ThrowingTransport(Func<Exception> create) : IPlcTransport
{
    public byte[] Request(byte[] packet, IReceiveLengthParser receiveLengthParser) => throw create();

    public Task<byte[]> RequestAsync(byte[] packet, IReceiveLengthParser receiveLengthParser) => throw create();

    public void Dispose()
    {
    }
}
