using System.Net.Sockets;
using McpXLib.Exceptions;

namespace McpXNative;

/// <summary>
/// 状態コードの返却と、エラー詳細（mcpx_error）への書き込み。
/// </summary>
/// <remarks>
/// 例外をネイティブ側へ漏らすとプロセスが終了するため、各 export は例外をここで状態コードに変換して返す。
/// </remarks>
internal static unsafe class Errors
{
    internal static int Ok(NativeError* err)
    {
        Write(err, McpxStatus.Ok, string.Empty, 0, 0);
        return (int)McpxStatus.Ok;
    }

    internal static int Fail(NativeError* err, McpxStatus status, string message)
    {
        Write(err, status, message, 0, 0);
        return (int)status;
    }

    internal static int InvalidArgument(NativeError* err, string message)
    {
        return Fail(err, McpxStatus.InvalidArgument, message);
    }

    internal static int InvalidHandle(NativeError* err)
    {
        return Fail(err, McpxStatus.InvalidHandle, "The client handle is invalid or has already been closed.");
    }

    /// <summary>
    /// 例外を状態コードに変換します。呼び出し中にクライアントが close された場合は <see cref="McpxStatus.Closed"/> を返します。
    /// </summary>
    internal static int FromException(NativeError* err, Exception ex, ClientEntry? entry)
    {
        var (status, endCode, socketError) = Classify(ex);
        if (entry is { Closed: true })
        {
            status = McpxStatus.Closed;
        }

        Write(err, status, $"{ex.GetType().Name}: {ex.Message}", endCode, socketError);
        return (int)status;
    }

    internal static (McpxStatus status, ushort endCode, int socketError) Classify(Exception ex)
    {
        return ex switch
        {
            McProtocolException pe => (McpxStatus.Protocol, pe.ErrorCode, 0),
            RecivePacketException => (McpxStatus.ReceivePacket, 0, 0),
            DeviceAddressException or OverflowException => (McpxStatus.InvalidDeviceAddress, 0, 0),
            TimeoutException te => (McpxStatus.Timeout, 0, te.InnerException is SocketException inner ? (int)inner.SocketErrorCode : 0),
            SocketException { SocketErrorCode: SocketError.ConnectionRefused } se => (McpxStatus.ConnectionRefused, 0, (int)se.SocketErrorCode),
            SocketException se => (McpxStatus.Network, 0, (int)se.SocketErrorCode),
            // ObjectDisposedException は InvalidOperationException の派生のため、先に判定する
            IOException or ObjectDisposedException => (McpxStatus.Disconnected, 0, 0),
            InvalidOperationException => (McpxStatus.InvalidOperation, 0, 0),
            ArgumentException => (McpxStatus.InvalidArgument, 0, 0),
            NotSupportedException or NotImplementedException => (McpxStatus.Unsupported, 0, 0),
            _ => (McpxStatus.Internal, 0, 0),
        };
    }

    private static void Write(NativeError* err, McpxStatus status, string message, ushort endCode, int socketError)
    {
        if (err == null || err->StructSize < NativeError.HeaderSize)
        {
            return;
        }

        try
        {
            err->Status = (int)status;
            err->EndCode = endCode;
            err->Reserved = 0;
            err->SocketError = socketError;

            if (err->StructSize >= (uint)sizeof(NativeError))
            {
                Utf8.WriteTruncated(message, err->Message, NativeError.MessageCapacity);
            }
        }
        catch
        {
            // エラーの書き込みで例外をネイティブ側へ漏らさない
        }
    }
}
