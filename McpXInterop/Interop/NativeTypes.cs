using System.Runtime.InteropServices;

namespace McpXNative;

// include/mcpx.h と同じ定義。値・レイアウトは ABI の一部のため、変更は末尾への追加のみとする。

internal enum McpxStatus
{
    Ok = 0,
    InvalidArgument = 1,
    InvalidHandle = 2,
    Timeout = 3,
    Disconnected = 4,
    ConnectionRefused = 5,
    Network = 6,
    Protocol = 7,
    ReceivePacket = 8,
    InvalidDeviceAddress = 9,
    Unsupported = 10,
    InvalidOperation = 11,
    BufferTooSmall = 12,
    Closed = 13,
    Internal = 99,
}

internal enum McpxType : byte
{
    Bool = 1,
    I8 = 2,
    U8 = 3,
    I16 = 4,
    U16 = 5,
    I32 = 6,
    U32 = 7,
    I64 = 8,
    U64 = 9,
    F32 = 10,
    F64 = 11,
}

internal enum McpxStruct : uint
{
    Error = 1,
    ConnectOptions = 2,
    SimulatorOptions = 3,
    Item = 4,
}

internal enum McpxItemKind : byte
{
    Single = 0,
    Range = 1,
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeError
{
    // メッセージを除いた先頭部分のサイズ（struct_size がこれ未満なら何も書き込まない）
    internal const int HeaderSize = 16;
    internal const int MessageCapacity = 512;

    public uint StructSize;
    public int Status;
    public ushort EndCode;
    public ushort Reserved;
    public int SocketError;
    public fixed byte Message[MessageCapacity];
}

[StructLayout(LayoutKind.Sequential)]
internal struct NativeConnectOptions
{
    public uint StructSize;
    public int Port;
    public IntPtr Host;
    public IntPtr Password;
    public uint TimeoutMs;
    public byte IsAscii;
    public byte IsUdp;
    public byte Frame;
    public byte Series;
    public byte UseMultiBlock;
    public byte Reserved0;
    public byte Reserved1;
    public byte Reserved2;
}

[StructLayout(LayoutKind.Sequential)]
internal struct NativeItem
{
    public IntPtr Address;
    public IntPtr Buffer;
    public nuint BufferSize;
    public uint Count;
    public byte Type;
    public byte Prefix;
    public byte Kind;
    public byte Reserved;
}

[StructLayout(LayoutKind.Sequential)]
internal struct NativeSimulatorOptions
{
    public uint StructSize;
    public int SystemNo;
    public int CpuNo;
    public IntPtr Host;
    public uint TimeoutMs;
    public byte Frame;
    public byte Series;
    public byte UseMultiBlock;
    public byte Reserved;
}
