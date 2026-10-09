## [0.13.0] - 2026-10-10
### Added
- Added a new native library with a stable C ABI (`mcpx.dll` / `mcpx.so` / `mcpx.dylib`, header `McpXInterop/include/mcpx.h`): `mcpx_connect` / `mcpx_connect_simulator` / `mcpx_close`, `mcpx_read` / `mcpx_write` / `mcpx_batch_read` / `mcpx_batch_write` for all value types, combined / random access (`mcpx_read_items` / `mcpx_write_items`), multiple block access (`mcpx_block_read` / `mcpx_block_write`), Shift_JIS strings (`mcpx_read_string` / `mcpx_write_string`), monitor sessions (`mcpx_monitor_register` / `mcpx_monitor_read` / `mcpx_session_free`), remote operations (`mcpx_remote_run` / `stop` / `pause` / `latch_clear` / `reset`) and library information. Every function returns a status code and reports details (message, PLC end code, socket error) through `mcpx_error`.
- `ReadString` / `WriteString` are now available in Native AOT builds as well.
- Added Python bindings (`bindings/python`, package `mcpx`) with the same feel as the C# McpX: single / batch / combined / random / block access, strings, monitor sessions, remote operations, async methods, and exceptions mapped to standard Python exceptions.
- Added `scripts/gxsim-portforward.ps1`, which makes GX Simulator3 (listening only on 127.0.0.1) reachable from other PCs by setting up `netsh portproxy` and a Windows Firewall rule.

### Removed
- **Breaking:** Removed the old native exports (`plc_connect`, `plc_close`, `batch_read_*`, `batch_write_*`). Use the new `mcpx_*` functions. The problems of the old exports (the process terminating on an exception, closed connections being kept, and all connections being serialized by a single lock) do not occur with the new functions.

### Fixed
- Fixed a new connection being left open when the instance was disposed during the reconnection of `RemoteReset`.

## [0.12.0] - 2026-09-28
### Fixed
- Fixed async TCP requests never timing out when the PLC does not respond (`NetworkStream.ReadAsync` ignores `ReceiveTimeout`).
- Fixed concurrent requests on the same instance (async TCP and UDP) interleaving their packets, which could swap responses between requests. Requests are now processed one at a time.
- Fixed the next request returning a stale (late) response after a timeout or communication error. TCP now closes the connection, and UDP recreates the socket on sync timeouts as well.
- Fixed a refused TCP connection throwing `AggregateException` and leaking the `TcpClient`. It now disposes the client and throws the underlying `SocketException`.
- Fixed `timeoutMilliseconds = 0` always failing to connect over TCP. `0` now means no timeout, as for requests.
- Fixed every command failing with `ArgumentOutOfRangeException` when `timeoutMilliseconds` was 1–249. The monitoring timer is now derived from the timeout (250 ms shorter, in 250 ms units; 0 below 500 ms) so that the PLC's error response arrives before the client times out.
- Fixed async UDP requests always timing out when `timeoutMilliseconds = 0`. `0` now means no timeout, as for TCP and sync UDP requests.
- Fixed the connection being left open when the remote password unlock failed in the constructor.
- Fixed `Dispose` leaving the socket open when the remote lock failed (e.g. after a disconnect), and throwing when called twice.
- Fixed an old `MonitorSession` silently passing values of another registration to its callbacks after a new `MonitorRegist` (the PLC keeps only the latest registration).

### Changed
- The timeout (`timeoutMilliseconds`) now applies to the whole request, from sending to receiving the complete response, for both sync and async TCP requests (previously per read call).
- After a TCP timeout or communication error, the connection is closed and subsequent requests throw `IOException`. Create a new instance to reconnect.
- **Breaking:** A refused TCP connection now throws `SocketException` instead of `AggregateException`.
- **Breaking:** All transport timeouts now throw `TimeoutException` (with the `SocketException` of `SocketError.TimedOut` as the inner exception). Previously TCP requests threw `IOException` and UDP requests threw `SocketException`.
- `Dispose` no longer throws when the remote lock fails; the connection is always released.
- `MonitorSession.Read` / `ReadAsync` now throw `InvalidOperationException` when the monitor registration has been replaced by another `MonitorRegist` call, or the connection has been re-established (e.g. by `RemoteReset`).

### Removed
- **Breaking:** Removed the obsolete single-argument `Request(byte[])` / `RequestAsync(byte[])` from `IPlc`, `IPlcTransport` and `BasePlc` (deprecated since 0.5.1). Use the overloads that take an `IReceiveLengthParser`.

## [0.11.0] - 2026-09-28
### Added
- Added multiple block batch read / write (commands 0406 / 1406): `BlockRead(Action<BlockReadBuilder>)` / `BlockWrite(Action<BlockWriteBuilder>)` and their async versions. Requests exceeding the limits (120 blocks, or 60 for `ProcessorSeries.iQR`; 960 points) are split automatically.
- Added `McpX.UseMultiBlockAccess` (and the `useMultiBlockAccess` constructor parameter of `McpX` / `McpXSimulator`). When enabled, `Read(Action<ReadBuilder>)` / `Write(Action<WriteBuilder>)` send multiple ranges in one request with multiple block access. Disabled by default, because some targets (e.g. a CPU built-in Ethernet port) do not support it and return error C059. `bool` ranges on bit devices whose length is not a multiple of 16 are still written per range, so that no other bits are overwritten.
- Added remote operations: `RemoteRun` / `RemoteStop` / `RemotePause` / `RemoteLatchClear` / `RemoteReset` and their async versions (commands 1001 / 1002 / 1003 / 1005 / 1006), and `RemoteRunClearMode`. `RemoteReset` does not throw when the connection is closed or times out due to the reset, and reconnects (and unlocks the remote password) so that the same instance can be used afterwards.
- Added `McProtocolException.ErrorCode` to get the error code returned by the PLC.

## [0.10.0] - 2026-09-27
### Added
- Added `McpXSimulator` for connecting to GX Simulator3 by system No. and CPU No. (port `5500 + system No. × 10 + CPU No.`, TCP / binary, `ProcessorSeries.iQR` by default), and `McpXSimulator.GetPort`. Multiple simulators can be connected at the same time by creating an instance for each.

## [0.9.2] - 2026-09-27
### Fixed
- Fixed bit random write (`RandomWriteBit`, and bit devices in `RandomWrite` / `Write(Action<WriteBuilder>)`) failing with error C061 when `ProcessorSeries.iQR` is specified. The set/reset value is now sent as 2 bytes (4 digits in ASCII) for the MELSEC iQ-R subcommand (0003).

## [0.9.1] - 2026-09-25
### Fixed
- Fixed `BatchRead` / `BatchWrite` with word-sized types on bit devices (e.g. `BatchRead<ushort>(Prefix.M, ...)`): requests split beyond 960 words started at the wrong device number (advanced by words instead of 16 points per word).
- Fixed `BatchRead` / `BatchWrite` silently truncating data when the total exceeded 65535 words.
- Fixed `Read<T>` reading more words than necessary for multi-word types (e.g. 4 words for `int`, 16 words for `long`).
- Fixed `sbyte` APIs (`ReadSByte`, `WriteSByte`, etc.) always throwing `NotSupportedException`.
- Fixed `byte` writes sending malformed packets. `byte` / `sbyte` now map one element to one word (low byte). **Breaking:** `BatchRead<byte>(n)` / `BatchReadByte(n)` now returns `n` elements instead of `2n` raw bytes.

## [0.9.0] - 2026-09-25
### Added
- Added a combined builder API: `Read(Action<ReadBuilder>)` / `Write(Action<WriteBuilder>)` (and their async versions). Devices added with a point count (read) or an array (write) use batch access; single devices use random access.

## [0.8.1] - 2026-09-25
### Fixed
- Fixed an unobserved faulted task being left behind when a TCP connection attempt timed out, which surfaced later via `TaskScheduler.UnobservedTaskException` (#41).
- Fixed UDP async requests after a receive timeout: the abandoned pending receive could consume the next request's response, causing cascading timeouts. The UDP socket is now recreated on timeout.

## [0.8.0] - 2026-07-15
### Added
- Added a builder-based random access API: `RandomRead` / `RandomWrite(Action<builder>)` (and their async versions) with automatic bit/word/double-word routing, including bit-device support.
- Added a builder-based monitor API: `MonitorRegist(Action<MonitorBuilder>)` returning a `MonitorSession` for repeated reads (register once, read many).

### Changed
- Made the point-count limits of random read/write and monitor register series-aware for the MELSEC iQ-R series.

### Deprecated
- Deprecated the generic `RandomRead` / `RandomWrite<T1, T2>` and the type-specific random overloads (Compat) in favor of the new builder API.

### Fixed
- Fixed ASCII access-point count encoding for 16 or more points in random read/write and monitor register.
- Fixed random read/write with bit (`bool`) devices, which previously returned an incorrect element count or produced malformed packets.

## [0.7.0] - 2026-06-30
### Added
- Added support for the MELSEC iQ-R series (device extension specification) via the `ProcessorSeries` option.

## [0.6.0] - 2026-02-01
### Added
- Made timeout configurable at instance creation.

## [0.5.5] - 2025-11-17
### Fixed
- Fixed In the NativeAOT-compatible McpxInterrop class, corrected BatchWriteBool to call BatchWrite with bool instead of short.

## [0.5.4] - 2025-09-03
### Fixed
- Fixed an issue in the BatchRead method where specifying types larger than 32 bits resulted in an incorrect read count.

## [0.5.3] - 2025-08-14
### Added
- Added VB-compatible, type-specific overloads

## [0.5.2] - 2025-07-16
### Fixed
- Fix bug in hex address conversion.

## [0.5.1] - 2024-04-17
### Changed
- Optimized TCP transport performance.

## [0.5.0] - 2024-04-14
### Added
- Added documentation comments

## [0.4.2] - 2024-04-13
### Changed
- Changed unnecessary `public` modifiers to `internal`

## [0.4.1] - 2024-04-12
### Fixed
- Fixed a bug where an error would occur in the random read or monitor command if the number of specified word devices was not exactly two.

## [0.4.0] - 2024-04-12
### Added
- Support for 4E frames (binary and ASCII)

### Changed
- Improved packet generation process
- Improved packet parsing process

## [0.3.0] - 2024-04-07
### Added
- Added string read functionality
- Added string write functionality

## [0.2.0] - 2024-04-03
### Added
- Support for UDP

## [0.1.0] - 2024-03-29
### Added
- Support for 3E frames (ASCII)
