using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace McpXNative;

/// <summary>
/// モニタ（mcpx_monitor_register / mcpx_monitor_read / mcpx_session_free）。
/// </summary>
internal static unsafe class MonitorExports
{
    [UnmanagedCallersOnly(EntryPoint = "mcpx_monitor_register", CallConvs = [typeof(CallConvCdecl)])]
    public static int Register(ulong client, NativeItem* items, nuint count, ulong* output, NativeError* err)
    {
        ClientEntry? entry = null;
        try
        {
            if (output == null)
            {
                return Errors.InvalidArgument(err, "out must not be null.");
            }

            *output = 0;

            entry = HandleTable.Get(client);
            if (entry == null)
            {
                return Errors.InvalidHandle(err);
            }

            if (!Items.TryParse(items, count, requireAddress: true, allowSingle: true, allowRange: false, err, out var parsed, out var status, requireBuffer: false))
            {
                return status;
            }

            var scratch = new byte[parsed.Length * SessionEntry.SlotSize];
            var session = entry.Client.MonitorRegist(builder =>
            {
                for (int i = 0; i < parsed.Length; i++)
                {
                    BuilderDispatch.AddMonitor(builder, parsed[i], scratch, i * SessionEntry.SlotSize);
                }
            });

            var id = HandleTable.AddSession(new SessionEntry(client, entry, session, parsed.Select(p => p.Type).ToArray(), scratch));

            // 登録中に close された場合は、セッションを残さない
            if (entry.Closed)
            {
                HandleTable.RemoveSession(id);
                return Errors.Fail(err, McpxStatus.Closed, "The client was closed during the call.");
            }

            *output = id;
            return Errors.Ok(err);
        }
        catch (Exception ex)
        {
            return Errors.FromException(err, ex, entry);
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "mcpx_monitor_read", CallConvs = [typeof(CallConvCdecl)])]
    public static int Read(ulong session, NativeItem* items, nuint count, NativeError* err)
    {
        SessionEntry? entry = null;
        try
        {
            entry = HandleTable.GetSession(session);
            if (entry == null)
            {
                return Errors.Fail(err, McpxStatus.InvalidHandle, "The session handle is invalid or has already been freed.");
            }

            if (items == null || count != (nuint)entry.Types.Length)
            {
                return Errors.InvalidArgument(err, $"items must not be null and count must be {entry.Types.Length} (the number of registered items).");
            }

            for (int i = 0; i < entry.Types.Length; i++)
            {
                if (items[i].Type != (byte)entry.Types[i])
                {
                    return Errors.InvalidArgument(err, $"items[{i}]: type must be the same as the registered type {(byte)entry.Types[i]}.");
                }

                if (items[i].Buffer == IntPtr.Zero || items[i].BufferSize < (nuint)TypeDispatch.SizeOf(entry.Types[i]))
                {
                    return Errors.InvalidArgument(err, $"items[{i}]: buffer must not be null and must be at least {TypeDispatch.SizeOf(entry.Types[i])} bytes.");
                }
            }

            lock (entry.Gate)
            {
                entry.Session.Read();

                for (int i = 0; i < entry.Types.Length; i++)
                {
                    int size = TypeDispatch.SizeOf(entry.Types[i]);
                    entry.Scratch.AsSpan(i * SessionEntry.SlotSize, size).CopyTo(new Span<byte>((void*)items[i].Buffer, size));
                }
            }

            return Errors.Ok(err);
        }
        catch (Exception ex)
        {
            return Errors.FromException(err, ex, entry?.Owner);
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "mcpx_session_free", CallConvs = [typeof(CallConvCdecl)])]
    public static int Free(ulong session, NativeError* err)
    {
        try
        {
            if (session == 0)
            {
                return Errors.Ok(err);
            }

            return HandleTable.RemoveSession(session) == null
                ? Errors.Fail(err, McpxStatus.InvalidHandle, "The session handle is invalid or has already been freed.")
                : Errors.Ok(err);
        }
        catch (Exception ex)
        {
            return Errors.FromException(err, ex, null);
        }
    }
}
