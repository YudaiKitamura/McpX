using McpXLib.Commands;
using McpXLib.Enums;
using McpXLib.Exceptions;

namespace McpXLib;

// リモート操作（コマンド: 1001 / 1002 / 1003 / 1005 / 1006）
public partial class McpX
{
    // リセット後の再接続の最大待ち時間（ミリ秒）と、試行の間隔（ミリ秒）
    private static readonly TimeSpan DefaultReconnectTimeout = TimeSpan.FromSeconds(30);
    private const int RECONNECT_INTERVAL = 1000;

    /// <summary>
    /// リモートRUN
    /// </summary>
    /// <remarks>
    /// 接続先のCPUユニットをRUN状態にします（コマンド: 1001）。<br/>
    /// CPUユニットのスイッチがRUNの場合に実行できます。スイッチがSTOPの場合、コマンドは正常完了しますがRUN状態になりません。<br/>
    /// リモート操作後に電源OFF→ONまたはリセットした場合は、CPUユニットのスイッチの状態に戻ります。
    /// </remarks>
    /// <param name="force">他の外部機器からリモートSTOP/PAUSE中でも実行する場合に<c>true</c>を指定します。（デフォルトは、<c>false</c>です。）</param>
    /// <param name="clearMode">RUN時にクリアするデバイスメモリの範囲を指定します。（デフォルトは、クリアしない:<c>RemoteRunClearMode.None</c>です。）</param>
    /// <exception cref="ReceivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <exception cref="TimeoutException">通信がタイムアウト時間内に完了しなかった場合に例外をスローします。</exception>
    /// <exception cref="System.IO.IOException">接続が切断された場合、または以前のエラーで接続が閉じられている場合に例外をスローします。</exception>
    /// <exception cref="ObjectDisposedException">インスタンスを破棄した後に呼び出した場合に例外をスローします。</exception>
    public void RemoteRun(bool force = false, RemoteRunClearMode clearMode = RemoteRunClearMode.None)
        => RemoteOperation(timer => RemoteOperationCommand.Run(force, clearMode, timer));

    /// <summary>
    /// リモートRUN（非同期）
    /// </summary>
    /// <remarks>
    /// 詳細は <see cref="RemoteRun(bool, RemoteRunClearMode)"/> を参照してください。
    /// </remarks>
    /// <param name="force">他の外部機器からリモートSTOP/PAUSE中でも実行する場合に<c>true</c>を指定します。（デフォルトは、<c>false</c>です。）</param>
    /// <param name="clearMode">RUN時にクリアするデバイスメモリの範囲を指定します。（デフォルトは、クリアしない:<c>RemoteRunClearMode.None</c>です。）</param>
    /// <param name="cancellationToken">要求をキャンセルするトークンを指定します。キャンセルされると <see cref="OperationCanceledException"/> をスローします。通信中にキャンセルした場合、TCP 交信では接続を閉じます（以降の呼び出しは <see cref="System.IO.IOException"/> になります）。</param>
    /// <exception cref="ReceivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <exception cref="TimeoutException">通信がタイムアウト時間内に完了しなかった場合に例外をスローします。</exception>
    /// <exception cref="System.IO.IOException">接続が切断された場合、または以前のエラーで接続が閉じられている場合に例外をスローします。</exception>
    /// <exception cref="ObjectDisposedException">インスタンスを破棄した後に呼び出した場合に例外をスローします。</exception>
    public Task RemoteRunAsync(bool force = false, RemoteRunClearMode clearMode = RemoteRunClearMode.None, CancellationToken cancellationToken = default)
        => RemoteOperationAsync(timer => RemoteOperationCommand.Run(force, clearMode, timer), cancellationToken);

    /// <summary>
    /// リモートSTOP
    /// </summary>
    /// <remarks>
    /// 接続先のCPUユニットをSTOP状態にします（コマンド: 1002）。
    /// </remarks>
    /// <exception cref="ReceivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <exception cref="TimeoutException">通信がタイムアウト時間内に完了しなかった場合に例外をスローします。</exception>
    /// <exception cref="System.IO.IOException">接続が切断された場合、または以前のエラーで接続が閉じられている場合に例外をスローします。</exception>
    /// <exception cref="ObjectDisposedException">インスタンスを破棄した後に呼び出した場合に例外をスローします。</exception>
    public void RemoteStop()
        => RemoteOperation(timer => RemoteOperationCommand.Stop(timer));

    /// <summary>
    /// リモートSTOP（非同期）
    /// </summary>
    /// <remarks>
    /// 接続先のCPUユニットをSTOP状態にします（コマンド: 1002）。
    /// </remarks>
    /// <param name="cancellationToken">要求をキャンセルするトークンを指定します。キャンセルされると <see cref="OperationCanceledException"/> をスローします。通信中にキャンセルした場合、TCP 交信では接続を閉じます（以降の呼び出しは <see cref="System.IO.IOException"/> になります）。</param>
    /// <exception cref="ReceivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <exception cref="TimeoutException">通信がタイムアウト時間内に完了しなかった場合に例外をスローします。</exception>
    /// <exception cref="System.IO.IOException">接続が切断された場合、または以前のエラーで接続が閉じられている場合に例外をスローします。</exception>
    /// <exception cref="ObjectDisposedException">インスタンスを破棄した後に呼び出した場合に例外をスローします。</exception>
    public Task RemoteStopAsync(CancellationToken cancellationToken = default)
        => RemoteOperationAsync(timer => RemoteOperationCommand.Stop(timer), cancellationToken);

    /// <summary>
    /// リモートPAUSE
    /// </summary>
    /// <remarks>
    /// 接続先のCPUユニットをPAUSE状態にします（コマンド: 1003）。<br/>
    /// CPUユニットのスイッチがRUNの場合に実行できます。スイッチがSTOPの場合、コマンドは正常完了しますがPAUSE状態になりません。
    /// </remarks>
    /// <param name="force">他の外部機器からリモートSTOP/PAUSE中でも実行する場合に<c>true</c>を指定します。（デフォルトは、<c>false</c>です。）</param>
    /// <exception cref="ReceivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <exception cref="TimeoutException">通信がタイムアウト時間内に完了しなかった場合に例外をスローします。</exception>
    /// <exception cref="System.IO.IOException">接続が切断された場合、または以前のエラーで接続が閉じられている場合に例外をスローします。</exception>
    /// <exception cref="ObjectDisposedException">インスタンスを破棄した後に呼び出した場合に例外をスローします。</exception>
    public void RemotePause(bool force = false)
        => RemoteOperation(timer => RemoteOperationCommand.Pause(force, timer));

    /// <summary>
    /// リモートPAUSE（非同期）
    /// </summary>
    /// <remarks>
    /// 詳細は <see cref="RemotePause(bool)"/> を参照してください。
    /// </remarks>
    /// <param name="force">他の外部機器からリモートSTOP/PAUSE中でも実行する場合に<c>true</c>を指定します。（デフォルトは、<c>false</c>です。）</param>
    /// <param name="cancellationToken">要求をキャンセルするトークンを指定します。キャンセルされると <see cref="OperationCanceledException"/> をスローします。通信中にキャンセルした場合、TCP 交信では接続を閉じます（以降の呼び出しは <see cref="System.IO.IOException"/> になります）。</param>
    /// <exception cref="ReceivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <exception cref="TimeoutException">通信がタイムアウト時間内に完了しなかった場合に例外をスローします。</exception>
    /// <exception cref="System.IO.IOException">接続が切断された場合、または以前のエラーで接続が閉じられている場合に例外をスローします。</exception>
    /// <exception cref="ObjectDisposedException">インスタンスを破棄した後に呼び出した場合に例外をスローします。</exception>
    public Task RemotePauseAsync(bool force = false, CancellationToken cancellationToken = default)
        => RemoteOperationAsync(timer => RemoteOperationCommand.Pause(force, timer), cancellationToken);

    /// <summary>
    /// リモートラッチクリア
    /// </summary>
    /// <remarks>
    /// 接続先のCPUユニットに対してラッチクリアを実行します（コマンド: 1005）。ラッチ範囲のデバイスもクリアされます。<br/>
    /// CPUユニットをSTOP状態にしてから実行してください。他の外部機器からリモートSTOP/PAUSE中の場合は、エラーになります。
    /// </remarks>
    /// <exception cref="ReceivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <exception cref="TimeoutException">通信がタイムアウト時間内に完了しなかった場合に例外をスローします。</exception>
    /// <exception cref="System.IO.IOException">接続が切断された場合、または以前のエラーで接続が閉じられている場合に例外をスローします。</exception>
    /// <exception cref="ObjectDisposedException">インスタンスを破棄した後に呼び出した場合に例外をスローします。</exception>
    public void RemoteLatchClear()
        => RemoteOperation(timer => RemoteOperationCommand.LatchClear(timer));

    /// <summary>
    /// リモートラッチクリア（非同期）
    /// </summary>
    /// <remarks>
    /// 詳細は <see cref="RemoteLatchClear()"/> を参照してください。
    /// </remarks>
    /// <param name="cancellationToken">要求をキャンセルするトークンを指定します。キャンセルされると <see cref="OperationCanceledException"/> をスローします。通信中にキャンセルした場合、TCP 交信では接続を閉じます（以降の呼び出しは <see cref="System.IO.IOException"/> になります）。</param>
    /// <exception cref="ReceivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <exception cref="TimeoutException">通信がタイムアウト時間内に完了しなかった場合に例外をスローします。</exception>
    /// <exception cref="System.IO.IOException">接続が切断された場合、または以前のエラーで接続が閉じられている場合に例外をスローします。</exception>
    /// <exception cref="ObjectDisposedException">インスタンスを破棄した後に呼び出した場合に例外をスローします。</exception>
    public Task RemoteLatchClearAsync(CancellationToken cancellationToken = default)
        => RemoteOperationAsync(timer => RemoteOperationCommand.LatchClear(timer), cancellationToken);

    /// <summary>
    /// リモートRESET
    /// </summary>
    /// <remarks>
    /// 接続先のCPUユニットをリセットします（コマンド: 1006）。<br/>
    /// CPUユニットをSTOP状態にしてから実行してください（エラーで停止している場合はRUNでも実行できます）。
    /// パラメータでリモートRESETを許可する設定が必要です。<br/>
    /// リセットにより応答が返らない場合や、接続が切断される場合があるため、通信の切断・タイムアウトは例外にしません。<br/>
    /// リセット後は、接続し直して SM400（常時ON）を読み出せるようになるまで待機します（リモートパスワードを指定している場合は、アンロックもやり直します）。
    /// そのため、同じインスタンスで続けて通信できます。
    /// </remarks>
    /// <param name="reconnectTimeout">
    /// リセット後に接続し直すまでの最大待ち時間を指定します。<see cref="TimeSpan.Zero"/> を指定した場合は、接続し直しません。（省略した場合は、30秒です。）
    /// </param>
    /// <exception cref="ReceivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合（リモートRESETが許可されていない場合など）に例外をスローします。</exception>
    /// <exception cref="TimeoutException">待ち時間内に接続し直せなかった場合に例外をスローします。</exception>
    /// <exception cref="ObjectDisposedException">インスタンスを破棄した後に呼び出した場合に例外をスローします。</exception>
    public void RemoteReset(TimeSpan? reconnectTimeout = null)
    {
        try
        {
            RemoteOperation(timer => RemoteOperationCommand.Reset(timer));
        }
        catch (Exception ex) when (IsDisconnectedByReset(ex))
        {
        }

        var timeout = reconnectTimeout ?? DefaultReconnectTimeout;
        if (timeout <= TimeSpan.Zero || !CanReconnect)
        {
            return;
        }

        var deadline = DateTime.UtcNow + timeout;
        Exception? lastError = null;
        do
        {
            Thread.Sleep(RECONNECT_INTERVAL);
            try
            {
                Reconnect();
                ProbeAfterReconnect();
                return;
            }
            catch (Exception ex) when (IsReconnectRetryable(ex))
            {
                lastError = ex;
            }
        }
        while (DateTime.UtcNow < deadline);

        throw new TimeoutException("Reconnection after remote RESET timed out.", lastError);
    }

    /// <summary>
    /// リモートRESET（非同期）
    /// </summary>
    /// <remarks>
    /// 詳細は <see cref="RemoteReset(TimeSpan?)"/> を参照してください。
    /// </remarks>
    /// <param name="reconnectTimeout">
    /// リセット後に接続し直すまでの最大待ち時間を指定します。<see cref="TimeSpan.Zero"/> を指定した場合は、接続し直しません。（省略した場合は、30秒です。）
    /// </param>
    /// <param name="cancellationToken">要求をキャンセルするトークンを指定します。キャンセルされると <see cref="OperationCanceledException"/> をスローします。通信中にキャンセルした場合、TCP 交信では接続を閉じます（以降の呼び出しは <see cref="System.IO.IOException"/> になります）。</param>
    /// <exception cref="ReceivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合（リモートRESETが許可されていない場合など）に例外をスローします。</exception>
    /// <exception cref="TimeoutException">待ち時間内に接続し直せなかった場合に例外をスローします。</exception>
    /// <exception cref="ObjectDisposedException">インスタンスを破棄した後に呼び出した場合に例外をスローします。</exception>
    public async Task RemoteResetAsync(TimeSpan? reconnectTimeout = null, CancellationToken cancellationToken = default)
    {
        try
        {
            await RemoteOperationAsync(timer => RemoteOperationCommand.Reset(timer), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (IsDisconnectedByReset(ex))
        {
        }

        var timeout = reconnectTimeout ?? DefaultReconnectTimeout;
        if (timeout <= TimeSpan.Zero || !CanReconnect)
        {
            return;
        }

        var deadline = DateTime.UtcNow + timeout;
        Exception? lastError = null;
        do
        {
            await Task.Delay(RECONNECT_INTERVAL, cancellationToken).ConfigureAwait(false);
            try
            {
                await ReconnectAsync(cancellationToken).ConfigureAwait(false);
                await ProbeAfterReconnectAsync(cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (Exception ex) when (IsReconnectRetryable(ex))
            {
                lastError = ex;
            }
        }
        while (DateTime.UtcNow < deadline);

        throw new TimeoutException("Reconnection after remote RESET timed out.", lastError);
    }

    // リセットによる応答なし・切断（タイムアウト: TimeoutException、切断・エラー後の要求: IOException、接続拒否など: SocketException）
    private static bool IsDisconnectedByReset(Exception ex)
        => ex is IOException or TimeoutException or System.Net.Sockets.SocketException;

    // リセット中は、接続できない・応答がない・エラーコードが返る場合があるため、待ち時間内は試し直す
    private static bool IsReconnectRetryable(Exception ex)
        => IsDisconnectedByReset(ex) || ex is McProtocolException;

    // 接続し直した後、アンロック（リモートパスワード指定時）と SM400（常時ON）の読み出しができることを確認する
    private void ProbeAfterReconnect()
    {
        if (password != null)
        {
            RemoteUnlock(password);
        }

        Read<bool>(Prefix.SM, "400");
    }

    private async Task ProbeAfterReconnectAsync(CancellationToken cancellationToken)
    {
        if (password != null)
        {
            await RemoteUnlockAsync(password, cancellationToken).ConfigureAwait(false);
        }

        await ReadAsync<bool>(Prefix.SM, "400", cancellationToken).ConfigureAwait(false);
    }
}
