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
    private readonly int timeout;
    // タイムアウト・通信エラー・キャンセルで接続を閉じたか。閉じた後は応答がずれるため再利用しない。
    // 要求中の読み書きは gate の内側で行う（gate の取得・解放で最新の値が見えるため volatile は不要）。
    private bool broken;

    private TcpPlcTransport(TcpClient client, int timeout)
    {
        this.client = client;
        this.timeout = timeout;
        stream = client.GetStream();
    }

    /// <summary>
    /// 接続します。<paramref name="timeout"/>（ミリ秒、0 は無期限）以内に接続できない場合は <see cref="TimeoutException"/> をスローします。
    /// </summary>
    internal static TcpPlcTransport Connect(string host, int port, int timeout)
    {
        var client = CreateClient(timeout);
        var task = client.ConnectAsync(host, port);

        // Task.Wait は接続失敗時に AggregateException をスローして後始末を飛ばすため、WaitAny で完了だけを待つ。
        if (Task.WaitAny([task], timeout == 0 ? Timeout.Infinite : timeout) != 0)
        {
            task.ObserveException();
            client.Dispose();
            throw new TimeoutException("Connection Timeout");
        }

        if (task.IsFaulted)
        {
            client.Dispose();
            // AggregateException を外し、SocketException などをそのままスローする
            task.GetAwaiter().GetResult();
        }

        return new TcpPlcTransport(client, timeout);
    }

    /// <summary>
    /// 非同期で接続します。期限切れは <see cref="TimeoutException"/>、キャンセルは <see cref="OperationCanceledException"/> になります。
    /// </summary>
    internal static async Task<TcpPlcTransport> ConnectAsync(string host, int port, int timeout, CancellationToken cancellationToken)
    {
        var client = CreateClient(timeout);
        try
        {
            await WaitAsync(client.ConnectAsync(host, port), timeout, Stopwatch.StartNew(), cancellationToken, "Connection Timeout").ConfigureAwait(false);
        }
        catch
        {
            client.Dispose();
            throw;
        }

        return new TcpPlcTransport(client, timeout);
    }

    private static TcpClient CreateClient(int timeout)
    {
        return new TcpClient
        {
            SendTimeout = timeout,
            ReceiveTimeout = timeout,
        };
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

                stream.WriteTimeout = GetRemaining(timeout, elapsed);
                stream.Write(packet, 0, packet.Length);

                int headerLength = contentLength.GetHeaderLength();
                var header = new byte[headerLength];
                ReadExactly(header, 0, headerLength, elapsed);

                var response = new byte[headerLength + contentLength.ParseContentLength(header)];
                Array.Copy(header, response, headerLength);
                ReadExactly(response, headerLength, response.Length - headerLength, elapsed);

                return response;
            }
            catch (IOException ex) when (ex.InnerException is SocketException { SocketErrorCode: SocketError.TimedOut } inner)
            {
                // OS の受信・送信タイムアウトも、非同期と同じ TimeoutException として扱う。
                Break();
                throw new TimeoutException(TimeoutMessage, inner);
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

    public async Task<byte[]> RequestAsync(byte[] packet, IReceiveLengthParser contentLength, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfBroken();
            try
            {
                var elapsed = Stopwatch.StartNew();

                await WaitAsync(stream.WriteAsync(packet, 0, packet.Length), timeout, elapsed, cancellationToken).ConfigureAwait(false);

                int headerLength = contentLength.GetHeaderLength();
                var header = new byte[headerLength];
                await ReadExactlyAsync(header, 0, headerLength, elapsed, cancellationToken).ConfigureAwait(false);

                var response = new byte[headerLength + contentLength.ParseContentLength(header)];
                Array.Copy(header, response, headerLength);
                await ReadExactlyAsync(response, headerLength, response.Length - headerLength, elapsed, cancellationToken).ConfigureAwait(false);

                return response;
            }
            catch
            {
                // 打ち切った受信が後から応答を読み込むこともあるため、接続を閉じる（キャンセルした場合も同じ）。
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

    private void ReadExactly(byte[] buffer, int offset, int count, Stopwatch elapsed)
    {
        while (count > 0)
        {
            // 1回の Read ごとではなく、要求全体の残り時間で待つ。
            stream.ReadTimeout = GetRemaining(timeout, elapsed);
            int bytesRead = stream.Read(buffer, offset, count);
            if (bytesRead == 0)
            {
                throw new IOException("Connection closed unexpectedly");
            }

            offset += bytesRead;
            count -= bytesRead;
        }
    }

    private async Task ReadExactlyAsync(byte[] buffer, int offset, int count, Stopwatch elapsed, CancellationToken cancellationToken)
    {
        while (count > 0)
        {
            int bytesRead = await WaitAsync(stream.ReadAsync(buffer, offset, count), timeout, elapsed, cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                throw new IOException("Connection closed unexpectedly");
            }

            offset += bytesRead;
            count -= bytesRead;
        }
    }

    /// <summary>
    /// 期限（<paramref name="elapsed"/> の開始から <paramref name="timeout"/> ミリ秒）までの残り時間を返します。無期限の場合は <see cref="Timeout.Infinite"/>。
    /// </summary>
    private static int GetRemaining(int timeout, Stopwatch elapsed)
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
    /// 非同期の接続・送受信を、期限内かつキャンセルされるまで待ちます。
    /// </summary>
    /// <remarks>
    /// <see cref="NetworkStream"/> の非同期 I/O は ReceiveTimeout / SendTimeout を見ないため、期限切れとキャンセルを WhenAny で判定する。
    /// （netstandard2.0 でも同じ挙動にするため、I/O へのキャンセルトークンには頼らない）
    /// </remarks>
    private static async Task WaitAsync(Task task, int timeout, Stopwatch elapsed, CancellationToken cancellationToken, string message = TimeoutMessage)
    {
        int remaining = GetRemaining(timeout, elapsed);
        if (remaining == Timeout.Infinite && !cancellationToken.CanBeCanceled)
        {
            await task.ConfigureAwait(false);
            return;
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var delay = Task.Delay(remaining, cts.Token);

        if (await Task.WhenAny(task, delay).ConfigureAwait(false) != task)
        {
            // 打ち切った送受信が後で失敗しても UnobservedTaskException にならないようにする
            task.ObserveException();
            cancellationToken.ThrowIfCancellationRequested();
            throw new TimeoutException(message, new SocketException((int)SocketError.TimedOut));
        }

        cts.Cancel();
        await task.ConfigureAwait(false);
    }

    private static async Task<T> WaitAsync<T>(Task<T> task, int timeout, Stopwatch elapsed, CancellationToken cancellationToken)
    {
        await WaitAsync((Task)task, timeout, elapsed, cancellationToken).ConfigureAwait(false);
        return await task.ConfigureAwait(false);
    }

    private const string TimeoutMessage = "The request timed out.";

    private static TimeoutException CreateTimeoutException()
    {
        return new TimeoutException(TimeoutMessage, new SocketException((int)SocketError.TimedOut));
    }
}
