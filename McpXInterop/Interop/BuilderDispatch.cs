using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using McpXLib;

namespace McpXNative;

/// <summary>
/// 検証済みの項目を、McpX のビルダー（統合読み書き・複数ブロック・モニタ）へ型ごとに登録します。
/// </summary>
/// <remarks>
/// 読み込みの結果は、ビルダーのコールバックで呼び出し側のバッファへ直接書き込む（ビルダーやコールバックは ABI に持ち込まない）。
/// AOT で確実にコードを生成させるため、ジェネリックメソッドは型ごとに明示的に呼び出す。
/// </remarks>
internal static unsafe class BuilderDispatch
{
    // ---- 統合読み込み（Read(Action<ReadBuilder>)） ----

    internal static void AddRead(ReadBuilder builder, ParsedItem item)
    {
        switch (item.Type)
        {
            case McpxType.Bool:
                nint buffer = item.Buffer;
                if (item.IsRange)
                {
                    builder.Add<bool>(item.Prefix, item.Address, item.Count, values => WriteBools(values, buffer));
                }
                else
                {
                    builder.Add<bool>(item.Prefix, item.Address, value => *(byte*)buffer = value ? (byte)1 : (byte)0);
                }
                break;
            case McpxType.I8: AddRead<sbyte>(builder, item); break;
            case McpxType.U8: AddRead<byte>(builder, item); break;
            case McpxType.I16: AddRead<short>(builder, item); break;
            case McpxType.U16: AddRead<ushort>(builder, item); break;
            case McpxType.I32: AddRead<int>(builder, item); break;
            case McpxType.U32: AddRead<uint>(builder, item); break;
            case McpxType.I64: AddRead<long>(builder, item); break;
            case McpxType.U64: AddRead<ulong>(builder, item); break;
            case McpxType.F32: AddRead<float>(builder, item); break;
            case McpxType.F64: AddRead<double>(builder, item); break;
            default: throw new NotSupportedException($"Type {item.Type} is not supported.");
        }
    }

    private static void AddRead<T>(ReadBuilder builder, ParsedItem item) where T : unmanaged
    {
        nint buffer = item.Buffer;
        if (item.IsRange)
        {
            builder.Add<T>(item.Prefix, item.Address, item.Count, values => CopyOut(values, buffer));
        }
        else
        {
            builder.Add<T>(item.Prefix, item.Address, value => Unsafe.WriteUnaligned((void*)buffer, value));
        }
    }

    // ---- 統合書き込み（Write(Action<WriteBuilder>)） ----

    internal static void AddWrite(WriteBuilder builder, ParsedItem item)
    {
        switch (item.Type)
        {
            case McpxType.Bool:
                if (item.IsRange)
                {
                    builder.Add(item.Prefix, item.Address, ReadBools(item.Buffer, item.Count));
                }
                else
                {
                    builder.Add(item.Prefix, item.Address, *(byte*)item.Buffer != 0);
                }
                break;
            case McpxType.I8: AddWrite<sbyte>(builder, item); break;
            case McpxType.U8: AddWrite<byte>(builder, item); break;
            case McpxType.I16: AddWrite<short>(builder, item); break;
            case McpxType.U16: AddWrite<ushort>(builder, item); break;
            case McpxType.I32: AddWrite<int>(builder, item); break;
            case McpxType.U32: AddWrite<uint>(builder, item); break;
            case McpxType.I64: AddWrite<long>(builder, item); break;
            case McpxType.U64: AddWrite<ulong>(builder, item); break;
            case McpxType.F32: AddWrite<float>(builder, item); break;
            case McpxType.F64: AddWrite<double>(builder, item); break;
            default: throw new NotSupportedException($"Type {item.Type} is not supported.");
        }
    }

    private static void AddWrite<T>(WriteBuilder builder, ParsedItem item) where T : unmanaged
    {
        if (item.IsRange)
        {
            builder.Add(item.Prefix, item.Address, CopyIn<T>(item.Buffer, item.Count));
        }
        else
        {
            builder.Add(item.Prefix, item.Address, Unsafe.ReadUnaligned<T>((void*)item.Buffer));
        }
    }

    // ---- 複数ブロック一括読み書き（BlockRead / BlockWrite）。RANGE のみ ----

    internal static void AddBlockRead(BlockReadBuilder builder, ParsedItem item)
    {
        switch (item.Type)
        {
            case McpxType.Bool:
                nint buffer = item.Buffer;
                builder.Add<bool>(item.Prefix, item.Address, item.Count, values => WriteBools(values, buffer));
                break;
            case McpxType.I8: AddBlockRead<sbyte>(builder, item); break;
            case McpxType.U8: AddBlockRead<byte>(builder, item); break;
            case McpxType.I16: AddBlockRead<short>(builder, item); break;
            case McpxType.U16: AddBlockRead<ushort>(builder, item); break;
            case McpxType.I32: AddBlockRead<int>(builder, item); break;
            case McpxType.U32: AddBlockRead<uint>(builder, item); break;
            case McpxType.I64: AddBlockRead<long>(builder, item); break;
            case McpxType.U64: AddBlockRead<ulong>(builder, item); break;
            case McpxType.F32: AddBlockRead<float>(builder, item); break;
            case McpxType.F64: AddBlockRead<double>(builder, item); break;
            default: throw new NotSupportedException($"Type {item.Type} is not supported.");
        }
    }

    private static void AddBlockRead<T>(BlockReadBuilder builder, ParsedItem item) where T : unmanaged
    {
        nint buffer = item.Buffer;
        builder.Add<T>(item.Prefix, item.Address, item.Count, values => CopyOut(values, buffer));
    }

    internal static void AddBlockWrite(BlockWriteBuilder builder, ParsedItem item)
    {
        switch (item.Type)
        {
            case McpxType.Bool: builder.Add(item.Prefix, item.Address, ReadBools(item.Buffer, item.Count)); break;
            case McpxType.I8: builder.Add(item.Prefix, item.Address, CopyIn<sbyte>(item.Buffer, item.Count)); break;
            case McpxType.U8: builder.Add(item.Prefix, item.Address, CopyIn<byte>(item.Buffer, item.Count)); break;
            case McpxType.I16: builder.Add(item.Prefix, item.Address, CopyIn<short>(item.Buffer, item.Count)); break;
            case McpxType.U16: builder.Add(item.Prefix, item.Address, CopyIn<ushort>(item.Buffer, item.Count)); break;
            case McpxType.I32: builder.Add(item.Prefix, item.Address, CopyIn<int>(item.Buffer, item.Count)); break;
            case McpxType.U32: builder.Add(item.Prefix, item.Address, CopyIn<uint>(item.Buffer, item.Count)); break;
            case McpxType.I64: builder.Add(item.Prefix, item.Address, CopyIn<long>(item.Buffer, item.Count)); break;
            case McpxType.U64: builder.Add(item.Prefix, item.Address, CopyIn<ulong>(item.Buffer, item.Count)); break;
            case McpxType.F32: builder.Add(item.Prefix, item.Address, CopyIn<float>(item.Buffer, item.Count)); break;
            case McpxType.F64: builder.Add(item.Prefix, item.Address, CopyIn<double>(item.Buffer, item.Count)); break;
            default: throw new NotSupportedException($"Type {item.Type} is not supported.");
        }
    }

    // ---- モニタ登録（MonitorRegist）。SINGLE のみ ----

    /// <summary>
    /// モニタ登録します。読み出した値はセッションの作業領域（scratch の offset から型のサイズ分）に書き込まれます。
    /// </summary>
    internal static void AddMonitor(MonitorBuilder builder, ParsedItem item, byte[] scratch, int offset)
    {
        switch (item.Type)
        {
            case McpxType.Bool: builder.Add<bool>(item.Prefix, item.Address, value => scratch[offset] = value ? (byte)1 : (byte)0); break;
            case McpxType.I16: AddMonitor<short>(builder, item, scratch, offset); break;
            case McpxType.U16: AddMonitor<ushort>(builder, item, scratch, offset); break;
            case McpxType.I32: AddMonitor<int>(builder, item, scratch, offset); break;
            case McpxType.U32: AddMonitor<uint>(builder, item, scratch, offset); break;
            case McpxType.F32: AddMonitor<float>(builder, item, scratch, offset); break;
            default: throw new NotSupportedException($"Type {item.Type} is not supported for monitoring. Supported types are bool, i16, u16, i32, u32 and f32.");
        }
    }

    private static void AddMonitor<T>(MonitorBuilder builder, ParsedItem item, byte[] scratch, int offset) where T : unmanaged
    {
        builder.Add<T>(item.Prefix, item.Address, value => MemoryMarshal.Write(scratch.AsSpan(offset), in value));
    }

    // ---- バッファとの変換 ----

    private static void WriteBools(bool[] values, nint buffer)
    {
        var destination = (byte*)buffer;
        for (int i = 0; i < values.Length; i++)
        {
            destination[i] = values[i] ? (byte)1 : (byte)0;
        }
    }

    private static bool[] ReadBools(nint buffer, int count)
    {
        var source = (byte*)buffer;
        var values = new bool[count];
        for (int i = 0; i < count; i++)
        {
            values[i] = source[i] != 0;
        }
        return values;
    }

    private static void CopyOut<T>(T[] values, nint buffer) where T : unmanaged
    {
        var bytes = MemoryMarshal.AsBytes(values.AsSpan());
        bytes.CopyTo(new Span<byte>((void*)buffer, bytes.Length));
    }

    private static T[] CopyIn<T>(nint buffer, int count) where T : unmanaged
    {
        var values = new T[count];
        new ReadOnlySpan<byte>((void*)buffer, count * sizeof(T)).CopyTo(MemoryMarshal.AsBytes(values.AsSpan()));
        return values;
    }
}
