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
    private static readonly ConcurrentDictionary<ulong, SessionEntry> sessions = new();

    // クライアントとモニタセッションは同じカウンタから採番する（取り違えても無効なハンドルになる）
    private static ulong NextId() => (ulong)Interlocked.Increment(ref lastId);

    internal static ulong Add(McpX client)
    {
        var id = NextId();
        clients[id] = new ClientEntry(client);
        return id;
    }

    internal static ulong AddSession(SessionEntry session)
    {
        var id = NextId();
        sessions[id] = session;
        return id;
    }

    internal static SessionEntry? GetSession(ulong id)
    {
        return sessions.TryGetValue(id, out var session) ? session : null;
    }

    internal static SessionEntry? RemoveSession(ulong id)
    {
        return sessions.TryRemove(id, out var session) ? session : null;
    }

    /// <summary>
    /// クライアントが所有するモニタセッションをすべて解放します（close 時）。
    /// </summary>
    internal static void RemoveSessionsOf(ulong clientId)
    {
        foreach (var pair in sessions)
        {
            if (pair.Value.ClientId == clientId)
            {
                sessions.TryRemove(pair.Key, out _);
            }
        }
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

internal sealed class SessionEntry(ulong clientId, ClientEntry owner, MonitorSession session, McpxType[] types, byte[] scratch)
{
    // 1項目あたりの作業領域のバイト数（モニタできる型は最大 4 バイト）
    internal const int SlotSize = 8;

    internal ulong ClientId { get; } = clientId;

    internal ClientEntry Owner { get; } = owner;

    internal MonitorSession Session { get; } = session;

    internal McpxType[] Types { get; } = types;

    // モニタのコールバックが値を書き込む作業領域（項目 i は i * SlotSize から）
    internal byte[] Scratch { get; } = scratch;

    // 同じセッションの読み出しを1つずつ行い、作業領域の内容を保護する
    internal object Gate { get; } = new();
}
