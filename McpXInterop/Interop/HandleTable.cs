using System.Collections.Concurrent;
using McpXLib;

namespace McpXNative;

/// <summary>
/// C ABI に渡すハンドルと McpX インスタンスの対応表。
/// </summary>
/// <remarks>
/// ハンドルは採番のみで再利用しないため、close 済みのハンドルは常に無効として扱える。
/// 表の参照はロックを取らない。通信の直列化は McpX（トランスポート）側に任せ、別のハンドル同士は並列に動作する。
/// </remarks>
internal static class HandleTable
{
    private static long lastId;
    private static readonly ConcurrentDictionary<ulong, ClientEntry> clients = new();

    internal static ulong Add(McpX client)
    {
        var id = (ulong)Interlocked.Increment(ref lastId);
        clients[id] = new ClientEntry(client);
        return id;
    }

    internal static ClientEntry? Get(ulong id)
    {
        return clients.TryGetValue(id, out var entry) ? entry : null;
    }

    internal static ClientEntry? Remove(ulong id)
    {
        return clients.TryRemove(id, out var entry) ? entry : null;
    }
}

internal sealed class ClientEntry
{
    internal ClientEntry(McpX client)
    {
        Client = client;
    }

    internal McpX Client { get; }

    // close 済みか。close したスレッドで設定し、通信中の別スレッドが失敗理由の判定に使うため volatile にする。
    private volatile bool closed;

    internal bool Closed
    {
        get => closed;
        set => closed = value;
    }
}
