using System.Diagnostics;
using System.Net.Sockets;
using McpXLib.Interfaces;
using McpXLib.Utils;

namespace McpXLib.Transports;

internal class TcpPlcTransport : IPlcTransport
{
    private readonly TcpClient client;
    private readonly NetworkStream stream;
    // 同期・非同期の要求を1つずつ処理する（送受信の交錯を防ぐ）。
    private readonly SemaphoreSlim gate = new(1, 1);
    // 1要求（送信開始〜応答受信完了）の期限（ミリ秒）。0 は無期限。
    private readonly ushort timeout;
    // タイムアウト・通信エラーで接続を閉じたか。閉じた後は応答がずれるため再利用しない。
    // 要求中の読み書きは gate の内側で行う（gate の取得・解放で最新の値が見えるため volatile は不要）。
    private bool broken;

    internal TcpPlcTransport(string ip, int port, ushort timeout)
    {
        this.timeout = timeout;
        client = new TcpClient();
        client.SendTimeout = timeout;
        client.ReceiveTimeout = timeout;

        var task = client.ConnectAsync(ip, port);
        if (!task.Wait(timeout))
        {
            task.ObserveException();
            client.Close();
            throw new TimeoutException("Connection Timeout");
        }
        stream = client.GetStream();
    }

    public void Dispose()
    {
        Break();
    }

    public byte[] Request(byte[] packet, IReceiveLengthParser contentLength)
    {
        gate.Wait();
        try
        {
            ThrowIfBroken();
            try
            {
                var elapsed = Stopwatch.StartNew();

                stream.WriteTimeout = GetRemaining(elapsed);
                stream.Write(packet, 0, packet.Length);

                var headerBytes = GetReceivePacket(contentLength.GetHeaderLength(), elapsed);

                var length = contentLength.ParseContentLength(headerBytes);

                return headerBytes.Concat(GetReceivePacket(length, elapsed)).ToArray();
            }
            catch
            {
                // 応答の途中で失敗すると残りがストリームに残り、次の要求が古い応答を読むため接続を閉じる。
                Break();
                throw;
            }
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<byte[]> RequestAsync(byte[] packet, IReceiveLengthParser contentLength)
    {
        await gate.WaitAsync();
        try
        {
            ThrowIfBroken();
            try
            {
                var elapsed = Stopwatch.StartNew();

                await WithDeadlineAsync(stream.WriteAsync(packet, 0, packet.Length), elapsed);

                var headerBytes = await GetReceivePacketAsync(contentLength.GetHeaderLength(), elapsed);

                var length = contentLength.ParseContentLength(headerBytes);

                return headerBytes.Concat(await GetReceivePacketAsync(length, elapsed)).ToArray();
            }
            catch
            {
                // 打ち切った受信が後から応答を読み込むこともあるため、接続を閉じる。
                Break();
                throw;
            }
        }
        finally
        {
            gate.Release();
        }
    }

    private void ThrowIfBroken()
    {
        if (broken)
        {
            throw new IOException("The connection was closed due to a previous error. Create a new instance to reconnect.");
        }
    }

    private void Break()
    {
        broken = true;
        stream.Dispose();
        client.Dispose();
    }

    private byte[] GetReceivePacket(int expectedLength, Stopwatch elapsed)
    {
        var memoryStream = new MemoryStream();
        var buffer = new byte[1024];
        int totalRead = 0;

        while (totalRead < expectedLength)
        {
            // 1回の Read ごとではなく、要求全体の残り時間で待つ。
            stream.ReadTimeout = GetRemaining(elapsed);
            int bytesRead = stream.Read(buffer, 0, Math.Min(buffer.Length, expectedLength - totalRead));
            if (bytesRead == 0)
            {
                throw new IOException("Connection closed unexpectedly");
            }

            memoryStream.Write(buffer, 0, bytesRead);
            totalRead += bytesRead;
        }

        return memoryStream.ToArray();
    }

    private async Task<byte[]> GetReceivePacketAsync(int expectedLength, Stopwatch elapsed)
    {
        var memoryStream = new MemoryStream();
        var buffer = new byte[1024];
        int totalRead = 0;

        while (totalRead < expectedLength)
        {
            int bytesRead = await WithDeadlineAsync(
                stream.ReadAsync(buffer, 0, Math.Min(buffer.Length, expectedLength - totalRead)),
                elapsed
            );
            if (bytesRead == 0)
            {
                throw new IOException("Connection closed unexpectedly");
            }

            memoryStream.Write(buffer, 0, bytesRead);
            totalRead += bytesRead;
        }

        return memoryStream.ToArray();
    }

    /// <summary>
    /// 要求全体の残り時間（ミリ秒）を返します。無期限の場合は <see cref="Timeout.Infinite"/>。
    /// </summary>
    private int GetRemaining(Stopwatch elapsed)
    {
        if (timeout == 0)
        {
            return Timeout.Infinite;
        }

        long remaining = timeout - elapsed.ElapsedMilliseconds;
        if (remaining <= 0)
        {
            throw CreateTimeoutException();
        }

        return (int)remaining;
    }

    /// <summary>
    /// 非同期の送受信を要求全体の期限内で待ちます。
    /// </summary>
    /// <remarks>
    /// <see cref="NetworkStream"/> の非同期 I/O は ReceiveTimeout / SendTimeout を見ないため、期限切れを WhenAny で判定する。
    /// （netstandard2.0 でも同じ挙動にするため、キャンセルトークンには頼らない）
    /// </remarks>
    private async Task WithDeadlineAsync(Task task, Stopwatch elapsed)
    {
        int remaining = GetRemaining(elapsed);
        if (remaining == Timeout.Infinite)
        {
            await task;
            return;
        }

        using var cts = new CancellationTokenSource();
        var delay = Task.Delay(remaining, cts.Token);

        if (await Task.WhenAny(task, delay) != task)
        {
            // 打ち切った送受信が後で失敗しても UnobservedTaskException にならないようにする
            task.ObserveException();
            throw CreateTimeoutException();
        }

        cts.Cancel();
        await task;
    }

    private async Task<T> WithDeadlineAsync<T>(Task<T> task, Stopwatch elapsed)
    {
        await WithDeadlineAsync((Task)task, elapsed);
        return await task;
    }

    private static IOException CreateTimeoutException()
    {
        // 同期版で OS がスローする例外と型・InnerException を揃える（メッセージは OS 非依存の固定文言）。
        return new IOException(
            "Unable to read data from the transport connection: Connection timed out.",
            new SocketException((int)SocketError.TimedOut)
        );
    }
}
