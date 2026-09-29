using System.Runtime.InteropServices;
using McpXLib.Enums;
using McpXNative;

namespace TestMcpXInterop;

/// <summary>
/// テスト用に mcpx_item の配列を組み立てる（アドレス文字列とバッファはネイティブメモリに確保し、Dispose で解放する）。
/// </summary>
internal sealed unsafe class ItemList : IDisposable
{
    private readonly List<NativeItem> items = new();
    private readonly List<IntPtr> allocations = new();

    internal int Count => items.Count;

    internal ItemList Add(Prefix prefix, string address, McpxType type, McpxItemKind kind, uint count, byte[]? initial = null, int size = 0)
    {
        int bytes = size > 0 ? size : (int)Math.Max(count, 1) * SizeOf(type);
        var buffer = Marshal.AllocHGlobal(bytes);
        new Span<byte>((void*)buffer, bytes).Clear();
        initial?.CopyTo(new Span<byte>((void*)buffer, bytes));
        var utf8 = System.Text.Encoding.UTF8.GetBytes(address + "\0");
        var text = Marshal.AllocHGlobal(utf8.Length);
        utf8.CopyTo(new Span<byte>((void*)text, utf8.Length));
        allocations.Add(buffer);
        allocations.Add(text);

        items.Add(new NativeItem
        {
            Address = text,
            Buffer = buffer,
            BufferSize = (nuint)bytes,
            Count = count,
            Type = (byte)type,
            Prefix = (byte)prefix,
            Kind = (byte)kind,
        });
        return this;
    }

    internal NativeItem this[int index]
    {
        get => items[index];
        set => items[index] = value;
    }

    internal T[] Values<T>(int index) where T : unmanaged
    {
        var item = items[index];
        int length = (int)item.BufferSize / sizeof(T);
        return new ReadOnlySpan<T>((void*)item.Buffer, length).ToArray();
    }

    internal NativeItem[] ToArray() => items.ToArray();

    private static int SizeOf(McpxType type) => type switch
    {
        McpxType.Bool or McpxType.I8 or McpxType.U8 => 1,
        McpxType.I16 or McpxType.U16 => 2,
        McpxType.I32 or McpxType.U32 or McpxType.F32 => 4,
        _ => 8,
    };

    public void Dispose()
    {
        foreach (var p in allocations)
        {
            Marshal.FreeHGlobal(p);
        }
    }
}
