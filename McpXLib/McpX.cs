using McpXLib.Commands;
using McpXLib.Enums;
using McpXLib.Utils;
using McpXLib.Exceptions;
using McpXLib.Interfaces;

namespace McpXLib;

/// <summary>
/// MCプロトコル交信クラス
/// </summary>
/// <remarks>
/// MCプロトコルでPLCと交信するクラスです。<see cref="Connect(McpXOptions)"/> / <see cref="ConnectAsync(McpXOptions, CancellationToken)"/> で接続します。
/// デバイスアクセス点数の上限を超える要求は、自動的に分割して送信します。<br/>
/// <br/>
/// <b>型引数</b><br/>
/// 読み書きの型 <c>T</c> には、<c>bool</c>、<c>short</c>、<c>ushort</c>、<c>int</c>、<c>uint</c>、<c>float</c>、<c>long</c>、<c>ulong</c>、<c>double</c>、<c>byte</c>、<c>sbyte</c> を指定できます
/// （<c>byte</c> / <c>sbyte</c> は1ワードに1つ）。ランダムアクセス・モニタ（点数を指定しないビルダーの登録）で指定できるのは、
/// <c>bool</c>、<c>short</c>、<c>ushort</c>、<c>int</c>、<c>uint</c>、<c>float</c> です。それ以外の型は <see cref="NotSupportedException"/> をスローします。<br/>
/// <br/>
/// <b>例外</b><br/>
/// 通信が指定したタイムアウト時間内に完了しない場合は <see cref="TimeoutException"/>、PLCがエラーコードを返した場合は <see cref="Exceptions.McProtocolException"/>、
/// 応答が不正な場合は <see cref="Exceptions.ReceivePacketException"/> をスローします（独自の例外は <see cref="Exceptions.McpXException"/> の派生クラスです）。
/// TCP 交信でタイムアウト・通信エラー・キャンセルが発生すると接続を閉じ、以降の呼び出しは <see cref="System.IO.IOException"/> になります（接続し直してください）。<br/>
/// <br/>
/// <b>スレッドセーフ</b><br/>
/// 同じインスタンスへの要求は、複数のスレッド・タスクから同時に呼び出せます（1つずつ順に送信します）。
/// ただし、複数の要求に分かれる操作（点数の多い連続アクセスなど）の途中に、他の要求が割り込むことがあります。
/// </remarks> 
public sealed partial class McpX : IDisposable, IAsyncDisposable
{
    // 0: 未破棄、1: 破棄済み（Dispose / DisposeAsync を一度だけ実行する）
    private int disposed;

    /// <summary>
    /// 接続
    /// </summary>
    /// <remarks>
    /// 指定した設定でPLCに接続します。リモートパスワードを指定した場合は、接続後にリモートパスワードを解除します。<br/>
    /// 使用後は <see cref="Dispose"/> または <see cref="DisposeAsync"/> で接続を閉じてください。
    /// </remarks>
    /// <param name="options">接続設定を指定します。</param>
    /// <returns>接続済みのインスタンスを返します。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> が <c>null</c> の場合に例外をスローします。</exception>
    /// <exception cref="ArgumentException">接続設定が不正な場合に例外をスローします。</exception>
    /// <exception cref="TimeoutException">タイムアウト時間内に接続できなかった場合に例外をスローします。</exception>
    /// <exception cref="System.Net.Sockets.SocketException">接続が拒否された場合など、接続に失敗した場合に例外をスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合（リモートパスワードの解除に失敗した場合など）に例外をスローします。</exception>
    public static McpX Connect(McpXOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        int timeout = options.Validate();
        var factory = Transports.TransportFactory.For(options.Host, options.Port, timeout, options.IsUdp);
        var mcpx = new McpX(factory.Create(), factory, timeout, options.IsAscii, options.RequestFrame, options.ProcessorSeries, options.Password, options.Route)
        {
            UseMultiBlockAccess = options.UseMultiBlockAccess,
        };
        mcpx.UnlockOrRelease();
        return mcpx;
    }

    /// <summary>
    /// 接続
    /// </summary>
    /// <remarks>
    /// 指定したIPアドレス（またはホスト名）とポート番号のPLCに、既定の設定（TCP・バイナリコード・3Eフレーム・MELSEC-Q/Lシリーズ）で接続します。
    /// 設定を変える場合は <see cref="Connect(McpXOptions)"/> を使用してください。
    /// </remarks>
    /// <param name="host">PLCのIPアドレスまたはホスト名を指定します。</param>
    /// <param name="port">PLCのポート番号を指定します。</param>
    /// <returns>接続済みのインスタンスを返します。</returns>
    /// <exception cref="ArgumentException">接続先が不正な場合に例外をスローします。</exception>
    /// <exception cref="TimeoutException">タイムアウト時間内に接続できなかった場合に例外をスローします。</exception>
    /// <exception cref="System.Net.Sockets.SocketException">接続が拒否された場合など、接続に失敗した場合に例外をスローします。</exception>
    public static McpX Connect(string host, int port)
        => Connect(new McpXOptions { Host = host, Port = port });

    /// <summary>
    /// 接続（非同期）
    /// </summary>
    /// <remarks>
    /// 詳細は <see cref="Connect(McpXOptions)"/> を参照してください。
    /// </remarks>
    /// <param name="options">接続設定を指定します。</param>
    /// <param name="cancellationToken">接続をキャンセルするトークンを指定します。キャンセルされると <see cref="OperationCanceledException"/> をスローします。</param>
    /// <returns>接続済みのインスタンスを返します。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> が <c>null</c> の場合に例外をスローします。</exception>
    /// <exception cref="ArgumentException">接続設定が不正な場合に例外をスローします。</exception>
    /// <exception cref="TimeoutException">タイムアウト時間内に接続できなかった場合に例外をスローします。</exception>
    /// <exception cref="System.Net.Sockets.SocketException">接続が拒否された場合など、接続に失敗した場合に例外をスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合（リモートパスワードの解除に失敗した場合など）に例外をスローします。</exception>
    public static async Task<McpX> ConnectAsync(McpXOptions options, CancellationToken cancellationToken = default)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        int timeout = options.Validate();
        var factory = Transports.TransportFactory.For(options.Host, options.Port, timeout, options.IsUdp);
        var transport = await factory.CreateAsync(cancellationToken).ConfigureAwait(false);
        var mcpx = new McpX(transport, factory, timeout, options.IsAscii, options.RequestFrame, options.ProcessorSeries, options.Password, options.Route)
        {
            UseMultiBlockAccess = options.UseMultiBlockAccess,
        };
        await mcpx.UnlockOrReleaseAsync(cancellationToken).ConfigureAwait(false);
        return mcpx;
    }

    /// <summary>
    /// 接続（非同期）
    /// </summary>
    /// <remarks>
    /// 詳細は <see cref="Connect(string, int)"/> を参照してください。
    /// </remarks>
    /// <param name="host">PLCのIPアドレスまたはホスト名を指定します。</param>
    /// <param name="port">PLCのポート番号を指定します。</param>
    /// <param name="cancellationToken">接続をキャンセルするトークンを指定します。キャンセルされると <see cref="OperationCanceledException"/> をスローします。</param>
    /// <returns>接続済みのインスタンスを返します。</returns>
    /// <exception cref="ArgumentException">接続先が不正な場合に例外をスローします。</exception>
    /// <exception cref="TimeoutException">タイムアウト時間内に接続できなかった場合に例外をスローします。</exception>
    /// <exception cref="System.Net.Sockets.SocketException">接続が拒否された場合など、接続に失敗した場合に例外をスローします。</exception>
    public static Task<McpX> ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
        => ConnectAsync(new McpXOptions { Host = host, Port = port }, cancellationToken);

    // テスト用：再接続できるよう、トランスポートの生成処理を指定して生成する。
    internal McpX(
        Func<IPlcTransport> transportFactory,
        ProcessorSeries processorSeries = ProcessorSeries.Q
    ) : this(Transports.TransportFactory.From(transportFactory), processorSeries)
    {
    }

    private McpX(Transports.TransportFactory factory, ProcessorSeries processorSeries)
        : this(factory.Create(), factory, 5000, false, RequestFrame.E3, processorSeries, null)
    {
    }

    // テスト用：トランスポートを差し替えて生成する。
    internal McpX(
        IPlcTransport transport,
        ProcessorSeries processorSeries = ProcessorSeries.Q,
        ushort timeoutMilliseconds = 5000,
        string? password = null,
        McpXRoute? route = null,
        bool isAscii = false
    ) : this(transport, null, timeoutMilliseconds, isAscii, RequestFrame.E3, processorSeries, password, route)
    {
        UnlockOrRelease();
    }

    // リモートパスワードを解除する。失敗した場合はインスタンスが返らず Dispose できないため、ここで接続を閉じる。
    private void UnlockOrRelease()
    {
        if (password == null)
        {
            return;
        }

        try
        {
            RemoteUnlock(password);
        }
        catch
        {
            // this.Dispose() はリモートロックを送るため、トランスポートだけを閉じる
            DisposeTransport();
            throw;
        }
    }

    private async Task UnlockOrReleaseAsync(CancellationToken cancellationToken)
    {
        if (password == null)
        {
            return;
        }

        try
        {
            await RemoteUnlockAsync(password, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            DisposeTransport();
            throw;
        }
    }

    /// <summary>
    /// 単一デバイス読み込み
    /// </summary>
    /// <remarks>
    /// 単一デバイスの値をPLCから読み込みます。<br/>
    /// 指定された型<c>T</c>に応じて、内部的に読み込むデバイス点数は自動的に調整されます。
    /// </remarks>
    /// <typeparam name="T">
    /// 読み込むデータの型。bool, short, int などの値型を指定します。
    /// `unmanaged` 制約があるため、参照型は使用できません。
    /// </typeparam>
    /// <param name="prefix">読み込み対象の先頭デバイスコードを指定します。</param>
    /// <param name="address">読み込み対象の先頭アドレスを指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <exception cref="TimeoutException">通信がタイムアウト時間内に完了しなかった場合に例外をスローします。</exception>
    /// <exception cref="System.IO.IOException">接続が切断された場合、または以前のエラーで接続が閉じられている場合に例外をスローします。</exception>
    /// <exception cref="ObjectDisposedException">インスタンスを破棄した後に呼び出した場合に例外をスローします。</exception>
    /// <returns>PLCから読み込んだ値を指定された型<c>T</c>に変換して返します。</returns>
    public T Read<T>(Prefix prefix, string address) where T : unmanaged
    {
        return ReadChunk<T>(prefix, address, 1)[0];
    }

    /// <summary>
    /// 単一デバイス読み込み（非同期）
    /// </summary>
    /// <remarks>
    /// 指定したデバイスの値を非同期でPLCから読み込みます。<br/>
    /// 指定された型<c>T</c>に応じて、内部的に読み込むデバイス点数は自動的に調整されます。
    /// </remarks>
    /// <typeparam name="T">
    /// 読み込むデータの型。bool, short, int などの値型を指定します。
    /// `unmanaged` 制約があるため、参照型は使用できません。
    /// </typeparam>
    /// <param name="prefix">読み込み対象の先頭デバイスコードを指定します。</param>
    /// <param name="address">読み込み対象の先頭アドレス</param>
    /// <param name="cancellationToken">要求をキャンセルするトークンを指定します。キャンセルされると <see cref="OperationCanceledException"/> をスローします。通信中にキャンセルした場合、TCP 交信では接続を閉じます（以降の呼び出しは <see cref="System.IO.IOException"/> になります）。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <exception cref="TimeoutException">通信がタイムアウト時間内に完了しなかった場合に例外をスローします。</exception>
    /// <exception cref="System.IO.IOException">接続が切断された場合、または以前のエラーで接続が閉じられている場合に例外をスローします。</exception>
    /// <exception cref="ObjectDisposedException">インスタンスを破棄した後に呼び出した場合に例外をスローします。</exception>
    /// <returns>PLCから読み込んだ値を指定された型<c>T</c>に変換して返します。</returns>
    public async Task<T> ReadAsync<T>(Prefix prefix, string address, CancellationToken cancellationToken = default) where T : unmanaged
    {
        return (await ReadChunkAsync<T>(prefix, address, 1, cancellationToken).ConfigureAwait(false))[0];
    }

    /// <summary>
    /// 連続デバイス読み込み
    /// </summary>
    /// <remarks>
    /// 指定したデバイス範囲の値をPLCから読み込みます。<br/>
    /// </remarks>
    /// <typeparam name="T">
    /// 読み込むデータの型。bool, short, int などの値型を指定します。
    /// `unmanaged` 制約があるため、参照型は使用できません。
    /// </typeparam>
    /// <param name="prefix">読み込み対象の先頭デバイスコードを指定します。</param>
    /// <param name="address">読み込み対象の先頭アドレスを指定します。</param>
    /// <param name="length">
    /// 読み込み対象の要素数を指定します。<br/>
    /// 指定された型<c>T</c>に応じて、内部的に読み込むデバイス点数は自動的に調整されます。<br/>
    /// そのため、この引数には「最終的に取得する配列の要素数（<c>T</c>型の要素数）」を指定してください。
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> が負の場合に例外をスローします。</exception>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <exception cref="TimeoutException">通信がタイムアウト時間内に完了しなかった場合に例外をスローします。</exception>
    /// <exception cref="System.IO.IOException">接続が切断された場合、または以前のエラーで接続が閉じられている場合に例外をスローします。</exception>
    /// <exception cref="ObjectDisposedException">インスタンスを破棄した後に呼び出した場合に例外をスローします。</exception>
    /// <returns>PLCから読み込んだ値を指定された型<c>T</c>に変換して返します。</returns>
    public T[] BatchRead<T>(Prefix prefix, string address, int length) where T : unmanaged
    {
        ThrowIfNegative(length, nameof(length));
        var values = new T[length];
        foreach (var (chunkAddress, offset, count) in SplitBatch<T>(prefix, address, length))
        {
            ReadChunk<T>(prefix, chunkAddress, count).CopyTo(values, offset);
        }

        return values;
    }

    /// <summary>
    /// 連続デバイス読み込み（非同期）
    /// </summary>
    /// <remarks>
    /// 指定したデバイス範囲の値を非同期でPLCから読み込みます。<br/>
    /// </remarks>
    /// <typeparam name="T">
    /// 読み込むデータの型。bool, short, int などの値型を指定します。
    /// `unmanaged` 制約があるため、参照型は使用できません。
    /// </typeparam>
    /// <param name="prefix">読み込み対象の先頭デバイスコードを指定します。</param>
    /// <param name="address">読み込み対象の先頭アドレスを指定します。</param>
    /// <param name="length">
    /// 読み込み対象の要素数を指定します。<br/>
    /// 指定された型<c>T</c>に応じて、内部的に読み込むデバイス点数は自動的に調整されます。<br/>
    /// そのため、この引数には「最終的に取得する配列の要素数（<c>T</c>型の要素数）」を指定してください。
    /// </param>
    /// <param name="cancellationToken">要求をキャンセルするトークンを指定します。キャンセルされると <see cref="OperationCanceledException"/> をスローします。通信中にキャンセルした場合、TCP 交信では接続を閉じます（以降の呼び出しは <see cref="System.IO.IOException"/> になります）。</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> が負の場合に例外をスローします。</exception>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <exception cref="TimeoutException">通信がタイムアウト時間内に完了しなかった場合に例外をスローします。</exception>
    /// <exception cref="System.IO.IOException">接続が切断された場合、または以前のエラーで接続が閉じられている場合に例外をスローします。</exception>
    /// <exception cref="ObjectDisposedException">インスタンスを破棄した後に呼び出した場合に例外をスローします。</exception>
    /// <returns>PLCから読み込んだ値を指定された型<c>T</c>に変換して返します。</returns>
    public async Task<T[]> BatchReadAsync<T>(Prefix prefix, string address, int length, CancellationToken cancellationToken = default) where T : unmanaged
    {
        ThrowIfNegative(length, nameof(length));
        var values = new T[length];
        foreach (var (chunkAddress, offset, count) in SplitBatch<T>(prefix, address, length))
        {
            (await ReadChunkAsync<T>(prefix, chunkAddress, count, cancellationToken).ConfigureAwait(false)).CopyTo(values, offset);
        }

        return values;
    }

    /// <summary>
    /// 単一デバイス書き込み
    /// </summary>
    /// <remarks>
    /// 単一デバイスの値をPLCに書き込みます。<br/>
    /// 指定された型<c>T</c>に応じて、内部的に書き込むデバイス点数は自動的に調整されます。
    /// </remarks>
    /// <typeparam name="T">
    /// 書き込むデータの型。bool, short, int などの値型を指定します。
    /// `unmanaged` 制約があるため、参照型は使用できません。
    /// </typeparam>
    /// <param name="prefix">書き込み対象の先頭デバイスコードを指定します。</param>
    /// <param name="address">書き込み対象の先頭アドレスを指定します。</param>
    /// <param name="value">書き込みする値を指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <exception cref="TimeoutException">通信がタイムアウト時間内に完了しなかった場合に例外をスローします。</exception>
    /// <exception cref="System.IO.IOException">接続が切断された場合、または以前のエラーで接続が閉じられている場合に例外をスローします。</exception>
    /// <exception cref="ObjectDisposedException">インスタンスを破棄した後に呼び出した場合に例外をスローします。</exception>
    public void Write<T>(Prefix prefix, string address, T value) where T : unmanaged
    {
        WriteChunk(prefix, address, new[] { value });
    }
    
    /// <summary>
    /// 単一デバイス書き込み（非同期）
    /// </summary>
    /// <remarks>
    /// 指定したデバイスに値を非同期でPLCへ書き込みます。<br/>
    /// 指定された型<c>T</c>に応じて、内部的に書き込むデバイス点数は自動的に調整されます。
    /// </remarks>
    /// <typeparam name="T">
    /// 書き込むデータの型。bool, short, int などの値型を指定します。
    /// `unmanaged` 制約があるため、参照型は使用できません。
    /// </typeparam>
    /// <param name="prefix">書き込み対象の先頭デバイスコードを指定します。</param>
    /// <param name="address">書き込み対象の先頭アドレスを指定します。</param>
    /// <param name="value">書き込みする値を指定します。</param>
    /// <param name="cancellationToken">要求をキャンセルするトークンを指定します。キャンセルされると <see cref="OperationCanceledException"/> をスローします。通信中にキャンセルした場合、TCP 交信では接続を閉じます（以降の呼び出しは <see cref="System.IO.IOException"/> になります）。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <exception cref="TimeoutException">通信がタイムアウト時間内に完了しなかった場合に例外をスローします。</exception>
    /// <exception cref="System.IO.IOException">接続が切断された場合、または以前のエラーで接続が閉じられている場合に例外をスローします。</exception>
    /// <exception cref="ObjectDisposedException">インスタンスを破棄した後に呼び出した場合に例外をスローします。</exception>
    public async Task WriteAsync<T>(Prefix prefix, string address, T value, CancellationToken cancellationToken = default) where T : unmanaged
    {
        await WriteChunkAsync(prefix, address, new[] { value }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 連続デバイス書き込み
    /// </summary>
    /// <remarks>
    /// 指定したデバイス範囲に値をPLCへ書き込みます。<br/>
    /// 指定された型<c>T</c>に応じて、内部的に書き込むデバイス点数は自動的に調整されます。
    /// </remarks>
    /// <typeparam name="T">
    /// 書き込むデータの型。bool, short, int などの値型を指定します。
    /// `unmanaged` 制約があるため、参照型は使用できません。
    /// </typeparam>
    /// <param name="prefix">書き込み対象の先頭デバイスコードを指定します。</param>
    /// <param name="address">書き込み対象の先頭アドレスを指定します。</param>
    /// <param name="values">書き込みする値を配列で指定します。</param>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> が <c>null</c> の場合に例外をスローします。</exception>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <exception cref="TimeoutException">通信がタイムアウト時間内に完了しなかった場合に例外をスローします。</exception>
    /// <exception cref="System.IO.IOException">接続が切断された場合、または以前のエラーで接続が閉じられている場合に例外をスローします。</exception>
    /// <exception cref="ObjectDisposedException">インスタンスを破棄した後に呼び出した場合に例外をスローします。</exception>
    public void BatchWrite<T>(Prefix prefix, string address, T[] values) where T : unmanaged
    {
        ThrowIfNull(values);
        foreach (var (chunkAddress, offset, count) in SplitBatch<T>(prefix, address, values.Length))
        {
            WriteChunk(prefix, chunkAddress, Slice(values, offset, count));
        }
    }

    /// <summary>
    /// 連続デバイス書き込み（非同期）
    /// </summary>
    /// <remarks>
    /// 指定したデバイス範囲に値を非同期でPLCへ書き込みます。<br/>
    /// 指定された型<c>T</c>に応じて、内部的に書き込むデバイス点数は自動的に調整されます。
    /// </remarks>
    /// <typeparam name="T">
    /// 書き込むデータの型。bool, short, int などの値型を指定します。
    /// `unmanaged` 制約があるため、参照型は使用できません。
    /// </typeparam>
    /// <param name="prefix">書き込み対象の先頭デバイスコードを指定します。</param>
    /// <param name="address">書き込み対象の先頭アドレスを指定します。</param>
    /// <param name="values">書き込みする値を配列で指定します。</param>
    /// <param name="cancellationToken">要求をキャンセルするトークンを指定します。キャンセルされると <see cref="OperationCanceledException"/> をスローします。通信中にキャンセルした場合、TCP 交信では接続を閉じます（以降の呼び出しは <see cref="System.IO.IOException"/> になります）。</param>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> が <c>null</c> の場合に例外をスローします。</exception>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <exception cref="TimeoutException">通信がタイムアウト時間内に完了しなかった場合に例外をスローします。</exception>
    /// <exception cref="System.IO.IOException">接続が切断された場合、または以前のエラーで接続が閉じられている場合に例外をスローします。</exception>
    /// <exception cref="ObjectDisposedException">インスタンスを破棄した後に呼び出した場合に例外をスローします。</exception>
    public async Task BatchWriteAsync<T>(Prefix prefix, string address, T[] values, CancellationToken cancellationToken = default) where T : unmanaged
    {
        ThrowIfNull(values);
        foreach (var (chunkAddress, offset, count) in SplitBatch<T>(prefix, address, values.Length))
        {
            await WriteChunkAsync(prefix, chunkAddress, Slice(values, offset, count), cancellationToken).ConfigureAwait(false);
        }
    }

    // ---------------------------------------------------------------
    // 連続アクセスの分割送信
    // ---------------------------------------------------------------

    /// <summary>
    /// 連続アクセスを、1回の要求に収まる範囲に分割します。
    /// </summary>
    /// <returns>各要求の先頭アドレスと、<typeparamref name="T"/> 型の要素単位の位置・個数。</returns>
    private List<(string address, int offset, int count)> SplitBatch<T>(Prefix prefix, string address, int length) where T : unmanaged
    {
        DeviceConverter.ThrowIfBitOnWordDevice<T>(prefix);

        int maxCount;
        int pointsPerElement;
        if (typeof(T) == typeof(bool))
        {
            maxCount = IsAscii ? BitBatchReadCommand.MAX_ASCII_BIT_LENGTH : BitBatchReadCommand.MAX_BIT_LENGTH;
            pointsPerElement = 1;
        }
        else
        {
            int wordSize = DeviceConverter.GetWordLength<T>();
            maxCount = WordBatchReadCommand<T>.MAX_WORD_LENGTH / wordSize;
            // ビットデバイスをワード単位でアクセスする場合、1ワード=16点のため先頭アドレスを16倍進める。
            pointsPerElement = wordSize * (DeviceConverter.IsBitDevice(prefix) ? 16 : 1);
        }

        var chunks = new List<(string, int, int)>();
        for (int offset = 0; offset < length; offset += maxCount)
        {
            chunks.Add((
                DeviceConverter.GetOffsetAddress(prefix, address, offset * pointsPerElement),
                offset,
                Math.Min(maxCount, length - offset)
            ));
        }

        return chunks;
    }

    private static void ThrowIfNegative(int length, string paramName)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, length, "Length must not be negative.");
        }
    }

    private static void ThrowIfNull<T>(T[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException(nameof(values));
        }
    }

    private static T[] Slice<T>(T[] values, int offset, int count)
    {
        var slice = new T[count];
        Array.Copy(values, offset, slice, 0, count);
        return slice;
    }

    // 1回の要求（ビット単位またはワード単位の一括読出し）で読み込む。
    private T[] ReadChunk<T>(Prefix prefix, string address, int count) where T : unmanaged
    {
        DeviceConverter.ThrowIfBitOnWordDevice<T>(prefix);
        return typeof(T) == typeof(bool)
            ? (T[])(object)BitBatchRead(prefix, address, (ushort)count)
            : WordBatchRead<T>(prefix, address, (ushort)count);
    }

    private async Task<T[]> ReadChunkAsync<T>(Prefix prefix, string address, int count, CancellationToken cancellationToken) where T : unmanaged
    {
        DeviceConverter.ThrowIfBitOnWordDevice<T>(prefix);
        return typeof(T) == typeof(bool)
            ? (T[])(object)await BitBatchReadAsync(prefix, address, (ushort)count, cancellationToken).ConfigureAwait(false)
            : await WordBatchReadAsync<T>(prefix, address, (ushort)count, cancellationToken).ConfigureAwait(false);
    }

    // 1回の要求（ビット単位またはワード単位の一括書込み）で書き込む。
    private void WriteChunk<T>(Prefix prefix, string address, T[] values) where T : unmanaged
    {
        DeviceConverter.ThrowIfBitOnWordDevice<T>(prefix);
        if (typeof(T) == typeof(bool))
        {
            BitBatchWrite(prefix, address, (bool[])(object)values);
        }
        else
        {
            WordBatchWrite(prefix, address, values);
        }
    }

    private async Task WriteChunkAsync<T>(Prefix prefix, string address, T[] values, CancellationToken cancellationToken) where T : unmanaged
    {
        DeviceConverter.ThrowIfBitOnWordDevice<T>(prefix);
        if (typeof(T) == typeof(bool))
        {
            await BitBatchWriteAsync(prefix, address, (bool[])(object)values, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await WordBatchWriteAsync(prefix, address, values, cancellationToken).ConfigureAwait(false);
        }
    }

    // ---------------------------------------------------------------
    // ランダムアクセスの分割送信（ビルダーAPIの内部実装）
    // ---------------------------------------------------------------

    /// <summary>
    /// ワード／ダブルワードのランダム読み出しを、点数上限ごとに分割して実行します。
    /// </summary>
    private (ushort[] words, uint[] doubleWords) RandomReadWords((Prefix prefix, string address)[] wordAddresses, (Prefix prefix, string address)[] doubleWordAddresses)
    {
        var allWords = new List<ushort>();
        var allDoubleWords = new List<uint>();

        int maxWordLength = WordRandomReadCommand<ushort, uint>.GetMaxWordLength(ProcessorSeries);
        int wordIndex = 0, doubleIndex = 0;
        while (wordIndex < wordAddresses.Length || doubleIndex < doubleWordAddresses.Length)
        {
            var wordBatch = wordAddresses.Skip(wordIndex).Take(maxWordLength).ToArray();
            var doubleBatch = doubleWordAddresses.Skip(doubleIndex).Take(maxWordLength - wordBatch.Length).ToArray();

            var (words, doubleWords) = WordRandomRead<ushort, uint>(wordBatch, doubleBatch);

            allWords.AddRange(words);
            allDoubleWords.AddRange(doubleWords);

            wordIndex += wordBatch.Length;
            doubleIndex += doubleBatch.Length;
        }

        return (allWords.ToArray(), allDoubleWords.ToArray());
    }

    private async Task<(ushort[] words, uint[] doubleWords)> RandomReadWordsAsync((Prefix prefix, string address)[] wordAddresses, (Prefix prefix, string address)[] doubleWordAddresses, CancellationToken cancellationToken)
    {
        var allWords = new List<ushort>();
        var allDoubleWords = new List<uint>();

        int maxWordLength = WordRandomReadCommand<ushort, uint>.GetMaxWordLength(ProcessorSeries);
        int wordIndex = 0, doubleIndex = 0;
        while (wordIndex < wordAddresses.Length || doubleIndex < doubleWordAddresses.Length)
        {
            var wordBatch = wordAddresses.Skip(wordIndex).Take(maxWordLength).ToArray();
            var doubleBatch = doubleWordAddresses.Skip(doubleIndex).Take(maxWordLength - wordBatch.Length).ToArray();

            var (words, doubleWords) = await WordRandomReadAsync<ushort, uint>(wordBatch, doubleBatch, cancellationToken).ConfigureAwait(false);

            allWords.AddRange(words);
            allDoubleWords.AddRange(doubleWords);

            wordIndex += wordBatch.Length;
            doubleIndex += doubleBatch.Length;
        }

        return (allWords.ToArray(), allDoubleWords.ToArray());
    }

    /// <summary>
    /// ワード／ダブルワードのランダム書き込みを、点数上限ごとに分割して実行します。
    /// </summary>
    private void RandomWriteWords((Prefix prefix, string address, ushort value)[] wordDevices, (Prefix prefix, string address, uint value)[] doubleWordDevices)
    {
        foreach (var (wordBatch, doubleWordBatch) in SplitRandomWrite(wordDevices, doubleWordDevices))
        {
            WordRandomWrite(wordBatch, doubleWordBatch);
        }
    }

    private async Task RandomWriteWordsAsync((Prefix prefix, string address, ushort value)[] wordDevices, (Prefix prefix, string address, uint value)[] doubleWordDevices, CancellationToken cancellationToken)
    {
        foreach (var (wordBatch, doubleWordBatch) in SplitRandomWrite(wordDevices, doubleWordDevices))
        {
            await WordRandomWriteAsync(wordBatch, doubleWordBatch, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// ランダム書き込みの対象を、1回の要求に収まる点数ごとに分割します（ワードを先に詰め、残りにダブルワードを詰める）。
    /// </summary>
    private List<((Prefix prefix, string address, ushort value)[] words, (Prefix prefix, string address, uint value)[] doubleWords)> SplitRandomWrite(
        (Prefix prefix, string address, ushort value)[] wordDevices,
        (Prefix prefix, string address, uint value)[] doubleWordDevices)
    {
        int maxSize = WordRandomWriteCommand<ushort, uint>.GetMaxWordLength(ProcessorSeries);
        int wordSize = WordRandomWriteCommand<ushort, uint>.WORD_SIZE;
        int doubleWordSize = WordRandomWriteCommand<ushort, uint>.DOUBLE_WORD_SIZE;

        var batches = new List<((Prefix, string, ushort)[], (Prefix, string, uint)[])>();
        int wordIndex = 0, doubleIndex = 0;
        while (wordIndex < wordDevices.Length || doubleIndex < doubleWordDevices.Length)
        {
            int wordCount = Math.Min(wordDevices.Length - wordIndex, maxSize / wordSize);
            int doubleCount = Math.Min(doubleWordDevices.Length - doubleIndex, (maxSize - wordCount * wordSize) / doubleWordSize);

            batches.Add((
                wordDevices.Skip(wordIndex).Take(wordCount).ToArray(),
                doubleWordDevices.Skip(doubleIndex).Take(doubleCount).ToArray()
            ));

            wordIndex += wordCount;
            doubleIndex += doubleCount;
        }

        return batches;
    }

    /// <summary>
    /// ビット単位のランダム書き込み（コマンド:1402 / サブコマンド:0001）を、点数上限ごとに分割して実行します。
    /// </summary>
    private void RandomWriteBits((Prefix prefix, string address, bool value)[] bitDevices)
    {
        int maxBitLength = BitRandomWriteCommand.GetMaxBitLength(ProcessorSeries);
        for (int i = 0; i < bitDevices.Length; i += maxBitLength)
        {
            BitRandomWrite(bitDevices.Skip(i).Take(maxBitLength).ToArray());
        }
    }

    private async Task RandomWriteBitsAsync((Prefix prefix, string address, bool value)[] bitDevices, CancellationToken cancellationToken)
    {
        int maxBitLength = BitRandomWriteCommand.GetMaxBitLength(ProcessorSeries);
        for (int i = 0; i < bitDevices.Length; i += maxBitLength)
        {
            await BitRandomWriteAsync(bitDevices.Skip(i).Take(maxBitLength).ToArray(), cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// ランダムデバイス読み込み（ビルダー）
    /// </summary>
    /// <remarks>
    /// 非連続のデバイスを、型ごとにアクセス単位（ビット／ワード／ダブルワード）を意識せずに一括で読み込みます。<br/>
    /// <paramref name="build"/> 内で <see cref="RandomReadBuilder.Add{T}(Prefix, string, Action{T})"/> により
    /// デバイスと読み取り後のコールバックを並べます。読み取り完了後、各コールバックに変換済みの値が渡されます。<br/>
    /// アクセス単位・点数上限・パケット分割は内部で自動処理されます。
    /// </remarks>
    /// <param name="build">読み込むデバイスとコールバックを登録するビルダー操作を指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <exception cref="TimeoutException">通信がタイムアウト時間内に完了しなかった場合に例外をスローします。</exception>
    /// <exception cref="System.IO.IOException">接続が切断された場合、または以前のエラーで接続が閉じられている場合に例外をスローします。</exception>
    /// <exception cref="ObjectDisposedException">インスタンスを破棄した後に呼び出した場合に例外をスローします。</exception>
    public void RandomRead(Action<RandomReadBuilder> build)
    {
        var builder = new RandomReadBuilder();
        (build ?? throw new ArgumentNullException(nameof(build)))(builder);
        ExecuteRandomRead(builder);
    }

    private void ExecuteRandomRead(RandomReadBuilder builder)
    {
        var wordAddresses = builder.wordEntries.Select(e => (e.prefix, e.address)).ToArray();
        var doubleWordAddresses = builder.doubleWordEntries.Select(e => (e.prefix, e.address)).ToArray();

        if (wordAddresses.Length == 0 && doubleWordAddresses.Length == 0)
        {
            return;
        }

        var (rawWords, rawDoubleWords) = RandomReadWords(wordAddresses, doubleWordAddresses);

        for (int i = 0; i < builder.wordEntries.Count; i++)
        {
            builder.wordEntries[i].apply(rawWords[i]);
        }

        for (int j = 0; j < builder.doubleWordEntries.Count; j++)
        {
            builder.doubleWordEntries[j].apply(rawDoubleWords[j]);
        }
    }

    /// <summary>
    /// ランダムデバイス読み込み（ビルダー・非同期）
    /// </summary>
    /// <remarks>
    /// 非連続のデバイスを、型ごとにアクセス単位（ビット／ワード／ダブルワード）を意識せずに非同期で一括読み込みします。<br/>
    /// <paramref name="build"/> 内で <see cref="RandomReadBuilder.Add{T}(Prefix, string, Action{T})"/> により
    /// デバイスと読み取り後のコールバックを並べます。読み取り完了後、各コールバックに変換済みの値が渡されます。
    /// </remarks>
    /// <param name="build">読み込むデバイスとコールバックを登録するビルダー操作を指定します。</param>
    /// <param name="cancellationToken">要求をキャンセルするトークンを指定します。キャンセルされると <see cref="OperationCanceledException"/> をスローします。通信中にキャンセルした場合、TCP 交信では接続を閉じます（以降の呼び出しは <see cref="System.IO.IOException"/> になります）。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <exception cref="TimeoutException">通信がタイムアウト時間内に完了しなかった場合に例外をスローします。</exception>
    /// <exception cref="System.IO.IOException">接続が切断された場合、または以前のエラーで接続が閉じられている場合に例外をスローします。</exception>
    /// <exception cref="ObjectDisposedException">インスタンスを破棄した後に呼び出した場合に例外をスローします。</exception>
    public async Task RandomReadAsync(Action<RandomReadBuilder> build, CancellationToken cancellationToken = default)
    {
        var builder = new RandomReadBuilder();
        (build ?? throw new ArgumentNullException(nameof(build)))(builder);
        await ExecuteRandomReadAsync(builder, cancellationToken).ConfigureAwait(false);
    }

    private async Task ExecuteRandomReadAsync(RandomReadBuilder builder, CancellationToken cancellationToken)
    {
        var wordAddresses = builder.wordEntries.Select(e => (e.prefix, e.address)).ToArray();
        var doubleWordAddresses = builder.doubleWordEntries.Select(e => (e.prefix, e.address)).ToArray();

        if (wordAddresses.Length == 0 && doubleWordAddresses.Length == 0)
        {
            return;
        }

        var (rawWords, rawDoubleWords) = await RandomReadWordsAsync(wordAddresses, doubleWordAddresses, cancellationToken).ConfigureAwait(false);

        for (int i = 0; i < builder.wordEntries.Count; i++)
        {
            builder.wordEntries[i].apply(rawWords[i]);
        }

        for (int j = 0; j < builder.doubleWordEntries.Count; j++)
        {
            builder.doubleWordEntries[j].apply(rawDoubleWords[j]);
        }
    }

    /// <summary>
    /// ランダムデバイス書き込み（ビルダー）
    /// </summary>
    /// <remarks>
    /// 非連続のデバイスに対し、型ごとにアクセス単位（ビット／ワード／ダブルワード）を意識せずに一括で書き込みます。<br/>
    /// <paramref name="build"/> 内で <see cref="RandomWriteBuilder.Add{T}(Prefix, string, T)"/> により
    /// デバイスと値を並べます。アクセス単位・点数上限・パケット分割は内部で自動処理されます。<br/>
    /// ワード／ダブルワードとビットはMCプロトコル上サブコマンドが異なるため、別パケットで送信されます。
    /// </remarks>
    /// <param name="build">書き込むデバイスと値を登録するビルダー操作を指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <exception cref="TimeoutException">通信がタイムアウト時間内に完了しなかった場合に例外をスローします。</exception>
    /// <exception cref="System.IO.IOException">接続が切断された場合、または以前のエラーで接続が閉じられている場合に例外をスローします。</exception>
    /// <exception cref="ObjectDisposedException">インスタンスを破棄した後に呼び出した場合に例外をスローします。</exception>
    public void RandomWrite(Action<RandomWriteBuilder> build)
    {
        var builder = new RandomWriteBuilder();
        (build ?? throw new ArgumentNullException(nameof(build)))(builder);
        ExecuteRandomWrite(builder);
    }

    private void ExecuteRandomWrite(RandomWriteBuilder builder)
    {
        if (builder.wordDevices.Count > 0 || builder.doubleWordDevices.Count > 0)
        {
            RandomWriteWords(builder.wordDevices.ToArray(), builder.doubleWordDevices.ToArray());
        }

        if (builder.bitDevices.Count > 0)
        {
            RandomWriteBits(builder.bitDevices.ToArray());
        }
    }

    /// <summary>
    /// ランダムデバイス書き込み（ビルダー・非同期）
    /// </summary>
    /// <remarks>
    /// 非連続のデバイスに対し、型ごとにアクセス単位（ビット／ワード／ダブルワード）を意識せずに非同期で一括書き込みします。<br/>
    /// <paramref name="build"/> 内で <see cref="RandomWriteBuilder.Add{T}(Prefix, string, T)"/> により
    /// デバイスと値を並べます。ワード／ダブルワードとビットは別パケットで送信されます。
    /// </remarks>
    /// <param name="build">書き込むデバイスと値を登録するビルダー操作を指定します。</param>
    /// <param name="cancellationToken">要求をキャンセルするトークンを指定します。キャンセルされると <see cref="OperationCanceledException"/> をスローします。通信中にキャンセルした場合、TCP 交信では接続を閉じます（以降の呼び出しは <see cref="System.IO.IOException"/> になります）。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <exception cref="TimeoutException">通信がタイムアウト時間内に完了しなかった場合に例外をスローします。</exception>
    /// <exception cref="System.IO.IOException">接続が切断された場合、または以前のエラーで接続が閉じられている場合に例外をスローします。</exception>
    /// <exception cref="ObjectDisposedException">インスタンスを破棄した後に呼び出した場合に例外をスローします。</exception>
    public async Task RandomWriteAsync(Action<RandomWriteBuilder> build, CancellationToken cancellationToken = default)
    {
        var builder = new RandomWriteBuilder();
        (build ?? throw new ArgumentNullException(nameof(build)))(builder);
        await ExecuteRandomWriteAsync(builder, cancellationToken).ConfigureAwait(false);
    }

    private async Task ExecuteRandomWriteAsync(RandomWriteBuilder builder, CancellationToken cancellationToken)
    {
        if (builder.wordDevices.Count > 0 || builder.doubleWordDevices.Count > 0)
        {
            await RandomWriteWordsAsync(builder.wordDevices.ToArray(), builder.doubleWordDevices.ToArray(), cancellationToken).ConfigureAwait(false);
        }

        if (builder.bitDevices.Count > 0)
        {
            await RandomWriteBitsAsync(builder.bitDevices.ToArray(), cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// モニタデバイス登録（ビルダー）
    /// </summary>
    /// <remarks>
    /// モニタ対象の非連続デバイスを、型ごとにアクセス単位（ビット／ワード／ダブルワード）を意識せずに登録(コマンド: 0801)します。<br/>
    /// <paramref name="build"/> 内で <see cref="MonitorBuilder.Add{T}(Prefix, string, Action{T})"/> により
    /// デバイスと読み取り後のコールバックを並べます。<br/>
    /// 戻り値の <see cref="MonitorSession"/> で <see cref="MonitorSession.Read"/> を繰り返し実行できます。<br/>
    /// モニタ登録はパケット分割ができず（コマンドの特性上、再登録は全置換）、点数上限は
    /// ワード＋ダブルワードの合計で 192点（Q/L）／96点（iQ-R）です。超過時は例外をスローします。
    /// </remarks>
    /// <param name="build">モニタ対象のデバイスとコールバックを登録するビルダー操作を指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="ArgumentException">モニタ登録の点数上限を超過した場合に例外をスローします（分割不可）。</exception>
    /// <exception cref="ReceivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <exception cref="TimeoutException">通信がタイムアウト時間内に完了しなかった場合に例外をスローします。</exception>
    /// <exception cref="System.IO.IOException">接続が切断された場合、または以前のエラーで接続が閉じられている場合に例外をスローします。</exception>
    /// <exception cref="ObjectDisposedException">インスタンスを破棄した後に呼び出した場合に例外をスローします。</exception>
    /// <returns>登録済みデバイスを繰り返し読み出すための <see cref="MonitorSession"/>。</returns>
    public MonitorSession MonitorRegister(Action<MonitorBuilder> build)
    {
        var builder = new MonitorBuilder();
        (build ?? throw new ArgumentNullException(nameof(build)))(builder);

        var wordAddresses = builder.wordEntries.Select(e => (e.prefix, e.address)).ToArray();
        var doubleWordAddresses = builder.doubleWordEntries.Select(e => (e.prefix, e.address)).ToArray();

        // モニタ登録は分割不可。点数上限を超えると MonitorRegisterCommand が例外をスローする。
        int generation = RegisterMonitor(wordAddresses, doubleWordAddresses);

        return new MonitorSession(this, builder, generation);
    }

    /// <summary>
    /// モニタデバイス登録（ビルダー・非同期）
    /// </summary>
    /// <remarks>
    /// モニタ対象の非連続デバイスを、型ごとにアクセス単位（ビット／ワード／ダブルワード）を意識せずに非同期で登録(コマンド: 0801)します。<br/>
    /// 戻り値の <see cref="MonitorSession"/> で <see cref="MonitorSession.ReadAsync"/> を繰り返し実行できます。<br/>
    /// モニタ登録はパケット分割ができず、点数上限は 192点（Q/L）／96点（iQ-R）です。超過時は例外をスローします。
    /// </remarks>
    /// <param name="build">モニタ対象のデバイスとコールバックを登録するビルダー操作を指定します。</param>
    /// <param name="cancellationToken">要求をキャンセルするトークンを指定します。キャンセルされると <see cref="OperationCanceledException"/> をスローします。通信中にキャンセルした場合、TCP 交信では接続を閉じます（以降の呼び出しは <see cref="System.IO.IOException"/> になります）。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="ArgumentException">モニタ登録の点数上限を超過した場合に例外をスローします（分割不可）。</exception>
    /// <exception cref="ReceivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <exception cref="TimeoutException">通信がタイムアウト時間内に完了しなかった場合に例外をスローします。</exception>
    /// <exception cref="System.IO.IOException">接続が切断された場合、または以前のエラーで接続が閉じられている場合に例外をスローします。</exception>
    /// <exception cref="ObjectDisposedException">インスタンスを破棄した後に呼び出した場合に例外をスローします。</exception>
    /// <returns>登録済みデバイスを繰り返し読み出すための <see cref="MonitorSession"/>。</returns>
    public async Task<MonitorSession> MonitorRegisterAsync(Action<MonitorBuilder> build, CancellationToken cancellationToken = default)
    {
        var builder = new MonitorBuilder();
        (build ?? throw new ArgumentNullException(nameof(build)))(builder);

        var wordAddresses = builder.wordEntries.Select(e => (e.prefix, e.address)).ToArray();
        var doubleWordAddresses = builder.doubleWordEntries.Select(e => (e.prefix, e.address)).ToArray();

        int generation = await RegisterMonitorAsync(wordAddresses, doubleWordAddresses, cancellationToken).ConfigureAwait(false);

        return new MonitorSession(this, builder, generation);
    }

    /// <summary>
    /// 連続／ランダム統合デバイス読み込み（ビルダー）
    /// </summary>
    /// <remarks>
    /// 連続デバイスと非連続デバイスを、1つのビルダーでまとめて読み込みます。<br/>
    /// <paramref name="build"/> 内で、点数を指定した <see cref="ReadBuilder.Add{T}(Prefix, string, int, Action{T[]})"/> は
    /// 連続アクセス（<see cref="BatchRead{T}(Prefix, string, int)"/>）、点数を指定しない
    /// <see cref="ReadBuilder.Add{T}(Prefix, string, Action{T})"/> はランダムアクセス
    /// （<see cref="RandomRead(Action{RandomReadBuilder})"/>）で読み込まれます。<br/>
    /// <see cref="UseMultiBlockAccess"/> が <c>true</c> で連続アクセスの範囲が2つ以上ある場合は、複数ブロック一括読出し（コマンド: 0406）でまとめて読み込みます。<br/>
    /// 連続アクセスを登録順に実行した後、ランダムアクセスをまとめて実行します。
    /// 複数のリクエストに分かれるため、全デバイスを同一スキャンで取得することは保証されません。
    /// </remarks>
    /// <param name="build">読み込むデバイスとコールバックを登録するビルダー操作を指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <exception cref="TimeoutException">通信がタイムアウト時間内に完了しなかった場合に例外をスローします。</exception>
    /// <exception cref="System.IO.IOException">接続が切断された場合、または以前のエラーで接続が閉じられている場合に例外をスローします。</exception>
    /// <exception cref="ObjectDisposedException">インスタンスを破棄した後に呼び出した場合に例外をスローします。</exception>
    public void Read(Action<ReadBuilder> build)
    {
        var builder = new ReadBuilder();
        (build ?? throw new ArgumentNullException(nameof(build)))(builder);

        var (blocks, remaining) = SplitForMultiBlock(builder.batchEntries, e => e.block);
        if (blocks.Count > 0)
        {
            ExecuteBlockRead(blocks);
        }

        foreach (var entry in remaining)
        {
            entry.read(this);
        }

        ExecuteRandomRead(builder.random);
    }

    /// <summary>
    /// 連続／ランダム統合デバイス読み込み（ビルダー・非同期）
    /// </summary>
    /// <remarks>
    /// 連続デバイスと非連続デバイスを、1つのビルダーでまとめて非同期で読み込みます。<br/>
    /// 点数を指定したデバイスは連続アクセス、点数を指定しないデバイスはランダムアクセスで読み込まれます。<br/>
    /// <see cref="UseMultiBlockAccess"/> が <c>true</c> で連続アクセスの範囲が2つ以上ある場合は、複数ブロック一括読出し（コマンド: 0406）でまとめて読み込みます。<br/>
    /// 複数のリクエストに分かれるため、全デバイスを同一スキャンで取得することは保証されません。
    /// </remarks>
    /// <param name="build">読み込むデバイスとコールバックを登録するビルダー操作を指定します。</param>
    /// <param name="cancellationToken">要求をキャンセルするトークンを指定します。キャンセルされると <see cref="OperationCanceledException"/> をスローします。通信中にキャンセルした場合、TCP 交信では接続を閉じます（以降の呼び出しは <see cref="System.IO.IOException"/> になります）。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <exception cref="TimeoutException">通信がタイムアウト時間内に完了しなかった場合に例外をスローします。</exception>
    /// <exception cref="System.IO.IOException">接続が切断された場合、または以前のエラーで接続が閉じられている場合に例外をスローします。</exception>
    /// <exception cref="ObjectDisposedException">インスタンスを破棄した後に呼び出した場合に例外をスローします。</exception>
    public async Task ReadAsync(Action<ReadBuilder> build, CancellationToken cancellationToken = default)
    {
        var builder = new ReadBuilder();
        (build ?? throw new ArgumentNullException(nameof(build)))(builder);

        var (blocks, remaining) = SplitForMultiBlock(builder.batchEntries, e => e.block);
        if (blocks.Count > 0)
        {
            await ExecuteBlockReadAsync(blocks, cancellationToken).ConfigureAwait(false);
        }

        foreach (var entry in remaining)
        {
            await entry.readAsync(this, cancellationToken).ConfigureAwait(false);
        }

        await ExecuteRandomReadAsync(builder.random, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 連続／ランダム統合デバイス書き込み（ビルダー）
    /// </summary>
    /// <remarks>
    /// 連続デバイスと非連続デバイスに、1つのビルダーでまとめて書き込みます。<br/>
    /// <paramref name="build"/> 内で、配列を指定した <see cref="WriteBuilder.Add{T}(Prefix, string, T[])"/> は
    /// 連続アクセス（<see cref="BatchWrite{T}(Prefix, string, T[])"/>）、単一の値を指定した
    /// <see cref="WriteBuilder.Add{T}(Prefix, string, T)"/> はランダムアクセス
    /// （<see cref="RandomWrite(Action{RandomWriteBuilder})"/>）で書き込まれます。<br/>
    /// <see cref="UseMultiBlockAccess"/> が <c>true</c> で連続アクセスの範囲が2つ以上ある場合は、複数ブロック一括書込み（コマンド: 1406）でまとめて書き込みます。（ビットデバイスへの <c>bool</c> で要素数が16の倍数でない範囲は、範囲ごとに書き込みます）<br/>
    /// 連続アクセスを登録順に実行した後、ランダムアクセスをまとめて実行します。
    /// </remarks>
    /// <param name="build">書き込むデバイスと値を登録するビルダー操作を指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <exception cref="TimeoutException">通信がタイムアウト時間内に完了しなかった場合に例外をスローします。</exception>
    /// <exception cref="System.IO.IOException">接続が切断された場合、または以前のエラーで接続が閉じられている場合に例外をスローします。</exception>
    /// <exception cref="ObjectDisposedException">インスタンスを破棄した後に呼び出した場合に例外をスローします。</exception>
    public void Write(Action<WriteBuilder> build)
    {
        var builder = new WriteBuilder();
        (build ?? throw new ArgumentNullException(nameof(build)))(builder);

        var (blocks, remaining) = SplitForMultiBlock(builder.batchEntries, e => e.block);
        if (blocks.Count > 0)
        {
            ExecuteBlockWrite(blocks);
        }

        foreach (var entry in remaining)
        {
            entry.write(this);
        }

        ExecuteRandomWrite(builder.random);
    }

    /// <summary>
    /// 連続／ランダム統合デバイス書き込み（ビルダー・非同期）
    /// </summary>
    /// <remarks>
    /// 連続デバイスと非連続デバイスに、1つのビルダーでまとめて非同期で書き込みます。<br/>
    /// 配列を指定したデバイスは連続アクセス、単一の値を指定したデバイスはランダムアクセスで書き込まれます。
    /// <see cref="UseMultiBlockAccess"/> が <c>true</c> で連続アクセスの範囲が2つ以上ある場合は、複数ブロック一括書込み（コマンド: 1406）でまとめて書き込みます。（ビットデバイスへの <c>bool</c> で要素数が16の倍数でない範囲は、範囲ごとに書き込みます）<br/>
    /// </remarks>
    /// <param name="build">書き込むデバイスと値を登録するビルダー操作を指定します。</param>
    /// <param name="cancellationToken">要求をキャンセルするトークンを指定します。キャンセルされると <see cref="OperationCanceledException"/> をスローします。通信中にキャンセルした場合、TCP 交信では接続を閉じます（以降の呼び出しは <see cref="System.IO.IOException"/> になります）。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <exception cref="TimeoutException">通信がタイムアウト時間内に完了しなかった場合に例外をスローします。</exception>
    /// <exception cref="System.IO.IOException">接続が切断された場合、または以前のエラーで接続が閉じられている場合に例外をスローします。</exception>
    /// <exception cref="ObjectDisposedException">インスタンスを破棄した後に呼び出した場合に例外をスローします。</exception>
    public async Task WriteAsync(Action<WriteBuilder> build, CancellationToken cancellationToken = default)
    {
        var builder = new WriteBuilder();
        (build ?? throw new ArgumentNullException(nameof(build)))(builder);

        var (blocks, remaining) = SplitForMultiBlock(builder.batchEntries, e => e.block);
        if (blocks.Count > 0)
        {
            await ExecuteBlockWriteAsync(blocks, cancellationToken).ConfigureAwait(false);
        }

        foreach (var entry in remaining)
        {
            await entry.writeAsync(this, cancellationToken).ConfigureAwait(false);
        }

        await ExecuteRandomWriteAsync(builder.random, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 文字列読み込み
    /// </summary>
    /// <remarks>
    /// 指定したデバイス範囲の値を文字列として、PLCから読み込みます。
    /// </remarks>
    /// <param name="prefix">読み込み対象の先頭デバイスコードを指定します。</param>
    /// <param name="address">読み込み対象の先頭アドレスを指定します。</param>
    /// <param name="wordLength">
    /// 読み込むワード数（デバイス点数）を指定します。1ワードに2バイト（半角2文字、全角1文字）が入ります。<br/>
    /// 読み込んだ範囲に 0x00 がある場合は、その手前までを文字列として返します。
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="wordLength"/> が負の場合に例外をスローします。</exception>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <exception cref="TimeoutException">通信がタイムアウト時間内に完了しなかった場合に例外をスローします。</exception>
    /// <exception cref="System.IO.IOException">接続が切断された場合、または以前のエラーで接続が閉じられている場合に例外をスローします。</exception>
    /// <exception cref="ObjectDisposedException">インスタンスを破棄した後に呼び出した場合に例外をスローします。</exception>
    /// <returns>PLCから読み込んだ値（Shift_JIS）を文字列に変換して返します。</returns>
    public string ReadString(Prefix prefix, string address, int wordLength)
    {
        ThrowIfNegative(wordLength, nameof(wordLength));
        return DeviceConverter.ConvertString(
            BatchRead<ushort>(prefix, address, wordLength).SelectMany(BitConverter.GetBytes).ToArray()
        );
    }

    /// <summary>
    /// 文字列読み込み（非同期）
    /// </summary>
    /// <remarks>
    /// 指定したデバイス範囲の値を文字列として、非同期でPLCから読み込みます。
    /// </remarks>
    /// <param name="prefix">読み込み対象の先頭デバイスコードを指定します。</param>
    /// <param name="address">読み込み対象の先頭アドレスを指定します。</param>
    /// <param name="wordLength">
    /// 読み込むワード数（デバイス点数）を指定します。1ワードに2バイト（半角2文字、全角1文字）が入ります。<br/>
    /// 読み込んだ範囲に 0x00 がある場合は、その手前までを文字列として返します。
    /// </param>
    /// <param name="cancellationToken">要求をキャンセルするトークンを指定します。キャンセルされると <see cref="OperationCanceledException"/> をスローします。通信中にキャンセルした場合、TCP 交信では接続を閉じます（以降の呼び出しは <see cref="System.IO.IOException"/> になります）。</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="wordLength"/> が負の場合に例外をスローします。</exception>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <exception cref="TimeoutException">通信がタイムアウト時間内に完了しなかった場合に例外をスローします。</exception>
    /// <exception cref="System.IO.IOException">接続が切断された場合、または以前のエラーで接続が閉じられている場合に例外をスローします。</exception>
    /// <exception cref="ObjectDisposedException">インスタンスを破棄した後に呼び出した場合に例外をスローします。</exception>
    /// <returns>PLCから読み込んだ値（Shift_JIS）を文字列に変換して返します。</returns>
    public async Task<string> ReadStringAsync(Prefix prefix, string address, int wordLength, CancellationToken cancellationToken = default)
    {
        ThrowIfNegative(wordLength, nameof(wordLength));
        return DeviceConverter.ConvertString(
            (await BatchReadAsync<ushort>(prefix, address, wordLength, cancellationToken).ConfigureAwait(false)).SelectMany(BitConverter.GetBytes).ToArray()
        );
    }

    /// <summary>
    /// 文字列書き込み
    /// </summary>
    /// <remarks>
    /// 指定したデバイスに対して、Shift_JISに変換した文字列データをPLCへ書き込みます。
    /// </remarks>
    /// <param name="prefix">書き込み対象の先頭デバイスコードを指定します。</param>
    /// <param name="address">書き込み対象の先頭アドレスを指定します。</param>
    /// <param name="value">書き込みする文字列を指定します。</param>
    /// <param name="includeNullTerminator">
    /// 文字列の後に終端の 0x00 を書き込む場合は<c>true</c>を指定します。（デフォルトは、<c>true</c>です。）<br/>
    /// 終端は、文字列のバイト数が偶数の場合は次のワード（1ワード分）、奇数の場合は最後のワードの上位バイトに書き込みます。
    /// <c>false</c>の場合、奇数バイトの最後のワードの上位バイトは 0x00 になります。
    /// </param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <exception cref="TimeoutException">通信がタイムアウト時間内に完了しなかった場合に例外をスローします。</exception>
    /// <exception cref="System.IO.IOException">接続が切断された場合、または以前のエラーで接続が閉じられている場合に例外をスローします。</exception>
    /// <exception cref="ObjectDisposedException">インスタンスを破棄した後に呼び出した場合に例外をスローします。</exception>
    public void WriteString(Prefix prefix, string address, string value, bool includeNullTerminator = true)
    {
        BatchWrite<ushort>(prefix, address, DeviceConverter.ConvertStringToUshorts(value, includeNullTerminator));
    }

    /// <summary>
    /// 文字列書き込み（非同期）
    /// </summary>
    /// <remarks>
    /// 指定したデバイスに対して、Shift_JISに変換した文字列データを非同期でPLCへ書き込みます。
    /// </remarks>
    /// <param name="prefix">書き込み対象の先頭デバイスコードを指定します。</param>
    /// <param name="address">書き込み対象の先頭アドレスを指定します。</param>
    /// <param name="value">書き込みする文字列を指定します。</param>
    /// <param name="includeNullTerminator">
    /// 文字列の後に終端の 0x00 を書き込む場合は<c>true</c>を指定します。（デフォルトは、<c>true</c>です。）<br/>
    /// 終端は、文字列のバイト数が偶数の場合は次のワード（1ワード分）、奇数の場合は最後のワードの上位バイトに書き込みます。
    /// <c>false</c>の場合、奇数バイトの最後のワードの上位バイトは 0x00 になります。
    /// </param>
    /// <param name="cancellationToken">要求をキャンセルするトークンを指定します。キャンセルされると <see cref="OperationCanceledException"/> をスローします。通信中にキャンセルした場合、TCP 交信では接続を閉じます（以降の呼び出しは <see cref="System.IO.IOException"/> になります）。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <exception cref="TimeoutException">通信がタイムアウト時間内に完了しなかった場合に例外をスローします。</exception>
    /// <exception cref="System.IO.IOException">接続が切断された場合、または以前のエラーで接続が閉じられている場合に例外をスローします。</exception>
    /// <exception cref="ObjectDisposedException">インスタンスを破棄した後に呼び出した場合に例外をスローします。</exception>
    public async Task WriteStringAsync(Prefix prefix, string address, string value, bool includeNullTerminator = true, CancellationToken cancellationToken = default)
    {
        await BatchWriteAsync<ushort>(prefix, address, DeviceConverter.ConvertStringToUshorts(value, includeNullTerminator), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// インスタンス破棄
    /// </summary>
    /// <remarks>
    /// 接続を閉じます。リモートパスワードを指定している場合は、閉じる前にPLCのリモートロックを実行します。<br/>
    /// 通信エラーなどでリモートロックに失敗しても例外はスローせず、接続は必ず閉じます。2回目以降の呼び出しは何もしません。<br/>
    /// 破棄した後に通信するメソッドを呼び出すと <see cref="ObjectDisposedException"/> をスローします。
    /// </remarks>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
        {
            return;
        }

        try
        {
            if (password != null)
            {
                RemoteLock(password);
            }
        }
        catch
        {
            // 接続が切れている場合などはロックを送れない。Dispose からは例外をスローしない（.NET の指針）。
        }
        finally
        {
            DisposeTransport();
        }
    }

    /// <summary>
    /// インスタンス破棄（非同期）
    /// </summary>
    /// <remarks>
    /// 詳細は <see cref="Dispose"/> を参照してください。リモートロックを非同期で送信します。
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
        {
            return;
        }

        try
        {
            if (password != null)
            {
                await RemoteLockAsync(password, CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch
        {
            // Dispose と同じく、ロックに失敗しても例外はスローしない。
        }
        finally
        {
            DisposeTransport();
        }
    }
}
