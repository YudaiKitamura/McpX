using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace McpXNative;

/// <summary>
/// 複数の項目をまとめて読み書き（mcpx_read_items / mcpx_write_items / mcpx_block_read / mcpx_block_write）。
/// </summary>
internal static unsafe class ItemExports
{
    [UnmanagedCallersOnly(EntryPoint = "mcpx_read_items", CallConvs = [typeof(CallConvCdecl)])]
    public static int ReadItems(ulong client, NativeItem* items, nuint count, NativeError* err)
    {
        ClientEntry? entry = null;
        try
        {
            entry = HandleTable.Get(client);
            if (entry == null)
            {
                return Errors.InvalidHandle(err);
            }

            if (!Items.TryParse(items, count, requireAddress: true, allowSingle: true, allowRange: true, err, out var parsed, out var status))
            {
                return status;
            }

            entry.Client.Read(builder =>
            {
                foreach (var item in parsed)
                {
                    BuilderDispatch.AddRead(builder, item);
                }
            });
            return Errors.Ok(err);
        }
        catch (Exception ex)
        {
            return Errors.FromException(err, ex, entry);
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "mcpx_write_items", CallConvs = [typeof(CallConvCdecl)])]
    public static int WriteItems(ulong client, NativeItem* items, nuint count, NativeError* err)
    {
        ClientEntry? entry = null;
        try
        {
            entry = HandleTable.Get(client);
            if (entry == null)
            {
                return Errors.InvalidHandle(err);
            }

            if (!Items.TryParse(items, count, requireAddress: true, allowSingle: true, allowRange: true, err, out var parsed, out var status))
            {
                return status;
            }

            entry.Client.Write(builder =>
            {
                foreach (var item in parsed)
                {
                    BuilderDispatch.AddWrite(builder, item);
                }
            });
            return Errors.Ok(err);
        }
        catch (Exception ex)
        {
            return Errors.FromException(err, ex, entry);
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "mcpx_block_read", CallConvs = [typeof(CallConvCdecl)])]
    public static int BlockRead(ulong client, NativeItem* items, nuint count, NativeError* err)
    {
        ClientEntry? entry = null;
        try
        {
            entry = HandleTable.Get(client);
            if (entry == null)
            {
                return Errors.InvalidHandle(err);
            }

            if (!Items.TryParse(items, count, requireAddress: true, allowSingle: false, allowRange: true, err, out var parsed, out var status))
            {
                return status;
            }

            entry.Client.BlockRead(builder =>
            {
                foreach (var item in parsed)
                {
                    BuilderDispatch.AddBlockRead(builder, item);
                }
            });
            return Errors.Ok(err);
        }
        catch (Exception ex)
        {
            return Errors.FromException(err, ex, entry);
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "mcpx_block_write", CallConvs = [typeof(CallConvCdecl)])]
    public static int BlockWrite(ulong client, NativeItem* items, nuint count, NativeError* err)
    {
        ClientEntry? entry = null;
        try
        {
            entry = HandleTable.Get(client);
            if (entry == null)
            {
                return Errors.InvalidHandle(err);
            }

            if (!Items.TryParse(items, count, requireAddress: true, allowSingle: false, allowRange: true, err, out var parsed, out var status))
            {
                return status;
            }

            entry.Client.BlockWrite(builder =>
            {
                foreach (var item in parsed)
                {
                    BuilderDispatch.AddBlockWrite(builder, item);
                }
            });
            return Errors.Ok(err);
        }
        catch (Exception ex)
        {
            return Errors.FromException(err, ex, entry);
        }
    }
}
