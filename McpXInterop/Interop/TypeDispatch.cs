using System.Runtime.CompilerServices;
using McpXLib;
using McpXLib.Enums;

namespace McpXNative;

/// <summary>
/// C ABI の型コード（MCPX_TYPE_*）から McpX のジェネリックメソッドを呼び分けます。
/// </summary>
/// <remarks>
/// Native AOT で確実にコードを生成させるため、型ごとに明示的に呼び出す（リフレクションや MakeGenericMethod は使わない）。
/// 呼び出し側のバッファは整列されている保証がないため、非整列アクセスで読み書きする。
/// </remarks>
internal static unsafe class TypeDispatch
{
    internal static int SizeOf(McpxType type)
    {
        return type switch
        {
            McpxType.Bool or McpxType.I8 or McpxType.U8 => 1,
            McpxType.I16 or McpxType.U16 => 2,
            McpxType.I32 or McpxType.U32 or McpxType.F32 => 4,
            McpxType.I64 or McpxType.U64 or McpxType.F64 => 8,
            _ => 0,
        };
    }

    internal static void Read(McpX client, Prefix prefix, string address, McpxType type, void* output)
    {
        switch (type)
        {
            case McpxType.Bool: Unsafe.WriteUnaligned(output, client.Read<bool>(prefix, address) ? (byte)1 : (byte)0); break;
            case McpxType.I8: Unsafe.WriteUnaligned(output, client.Read<sbyte>(prefix, address)); break;
            case McpxType.U8: Unsafe.WriteUnaligned(output, client.Read<byte>(prefix, address)); break;
            case McpxType.I16: Unsafe.WriteUnaligned(output, client.Read<short>(prefix, address)); break;
            case McpxType.U16: Unsafe.WriteUnaligned(output, client.Read<ushort>(prefix, address)); break;
            case McpxType.I32: Unsafe.WriteUnaligned(output, client.Read<int>(prefix, address)); break;
            case McpxType.U32: Unsafe.WriteUnaligned(output, client.Read<uint>(prefix, address)); break;
            case McpxType.I64: Unsafe.WriteUnaligned(output, client.Read<long>(prefix, address)); break;
            case McpxType.U64: Unsafe.WriteUnaligned(output, client.Read<ulong>(prefix, address)); break;
            case McpxType.F32: Unsafe.WriteUnaligned(output, client.Read<float>(prefix, address)); break;
            case McpxType.F64: Unsafe.WriteUnaligned(output, client.Read<double>(prefix, address)); break;
            default: throw new NotSupportedException($"Type {type} is not supported.");
        }
    }

    internal static void Write(McpX client, Prefix prefix, string address, McpxType type, void* value)
    {
        switch (type)
        {
            case McpxType.Bool: client.Write(prefix, address, Unsafe.ReadUnaligned<byte>(value) != 0); break;
            case McpxType.I8: client.Write(prefix, address, Unsafe.ReadUnaligned<sbyte>(value)); break;
            case McpxType.U8: client.Write(prefix, address, Unsafe.ReadUnaligned<byte>(value)); break;
            case McpxType.I16: client.Write(prefix, address, Unsafe.ReadUnaligned<short>(value)); break;
            case McpxType.U16: client.Write(prefix, address, Unsafe.ReadUnaligned<ushort>(value)); break;
            case McpxType.I32: client.Write(prefix, address, Unsafe.ReadUnaligned<int>(value)); break;
            case McpxType.U32: client.Write(prefix, address, Unsafe.ReadUnaligned<uint>(value)); break;
            case McpxType.I64: client.Write(prefix, address, Unsafe.ReadUnaligned<long>(value)); break;
            case McpxType.U64: client.Write(prefix, address, Unsafe.ReadUnaligned<ulong>(value)); break;
            case McpxType.F32: client.Write(prefix, address, Unsafe.ReadUnaligned<float>(value)); break;
            case McpxType.F64: client.Write(prefix, address, Unsafe.ReadUnaligned<double>(value)); break;
            default: throw new NotSupportedException($"Type {type} is not supported.");
        }
    }

    internal static void BatchRead(McpX client, Prefix prefix, string address, McpxType type, ushort count, void* output)
    {
        switch (type)
        {
            case McpxType.Bool:
                var bits = client.BatchRead<bool>(prefix, address, count);
                for (int i = 0; i < count; i++)
                {
                    ((byte*)output)[i] = bits[i] ? (byte)1 : (byte)0;
                }
                break;
            case McpxType.I8: CopyOut(client.BatchRead<sbyte>(prefix, address, count), output); break;
            case McpxType.U8: CopyOut(client.BatchRead<byte>(prefix, address, count), output); break;
            case McpxType.I16: CopyOut(client.BatchRead<short>(prefix, address, count), output); break;
            case McpxType.U16: CopyOut(client.BatchRead<ushort>(prefix, address, count), output); break;
            case McpxType.I32: CopyOut(client.BatchRead<int>(prefix, address, count), output); break;
            case McpxType.U32: CopyOut(client.BatchRead<uint>(prefix, address, count), output); break;
            case McpxType.I64: CopyOut(client.BatchRead<long>(prefix, address, count), output); break;
            case McpxType.U64: CopyOut(client.BatchRead<ulong>(prefix, address, count), output); break;
            case McpxType.F32: CopyOut(client.BatchRead<float>(prefix, address, count), output); break;
            case McpxType.F64: CopyOut(client.BatchRead<double>(prefix, address, count), output); break;
            default: throw new NotSupportedException($"Type {type} is not supported.");
        }
    }

    internal static void BatchWrite(McpX client, Prefix prefix, string address, McpxType type, ushort count, void* values)
    {
        switch (type)
        {
            case McpxType.Bool:
                var bits = new bool[count];
                for (int i = 0; i < count; i++)
                {
                    bits[i] = ((byte*)values)[i] != 0;
                }
                client.BatchWrite(prefix, address, bits);
                break;
            case McpxType.I8: client.BatchWrite(prefix, address, CopyIn<sbyte>(values, count)); break;
            case McpxType.U8: client.BatchWrite(prefix, address, CopyIn<byte>(values, count)); break;
            case McpxType.I16: client.BatchWrite(prefix, address, CopyIn<short>(values, count)); break;
            case McpxType.U16: client.BatchWrite(prefix, address, CopyIn<ushort>(values, count)); break;
            case McpxType.I32: client.BatchWrite(prefix, address, CopyIn<int>(values, count)); break;
            case McpxType.U32: client.BatchWrite(prefix, address, CopyIn<uint>(values, count)); break;
            case McpxType.I64: client.BatchWrite(prefix, address, CopyIn<long>(values, count)); break;
            case McpxType.U64: client.BatchWrite(prefix, address, CopyIn<ulong>(values, count)); break;
            case McpxType.F32: client.BatchWrite(prefix, address, CopyIn<float>(values, count)); break;
            case McpxType.F64: client.BatchWrite(prefix, address, CopyIn<double>(values, count)); break;
            default: throw new NotSupportedException($"Type {type} is not supported.");
        }
    }

    private static void CopyOut<T>(T[] source, void* destination) where T : unmanaged
    {
        // 非整列のバッファにも書けるよう、バイト列としてコピーする
        var bytes = System.Runtime.InteropServices.MemoryMarshal.AsBytes(source.AsSpan());
        bytes.CopyTo(new Span<byte>(destination, bytes.Length));
    }

    private static T[] CopyIn<T>(void* source, int count) where T : unmanaged
    {
        var values = new T[count];
        new ReadOnlySpan<byte>(source, count * sizeof(T)).CopyTo(System.Runtime.InteropServices.MemoryMarshal.AsBytes(values.AsSpan()));
        return values;
    }
}
