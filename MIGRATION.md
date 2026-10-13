# Migrating from 0.x to 1.0

1.0 fixes the public API, so it removes and renames the APIs that were kept for compatibility. This guide lists the changes and how to rewrite your code. See [CHANGELOG.md](CHANGELOG.md) for all changes.

[日本語](MIGRATION_JA.md)

## Connecting

The constructor was replaced by the `McpX.Connect` / `McpX.ConnectAsync` factories and `McpXOptions`.

```csharp
// 0.x
using var mcpx = new McpX("192.168.12.88", 10000, password: "1234", isAscii: true, isUdp: false,
    requestFrame: RequestFrame.E4, timeoutMilliseconds: 3000, processorSeries: ProcessorSeries.iQR);

// 1.0
using var mcpx = McpX.Connect(new McpXOptions
{
    Host = "192.168.12.88",
    Port = 10000,
    Password = "1234",
    IsAscii = true,
    IsUdp = false,
    RequestFrame = RequestFrame.E4,
    Timeout = TimeSpan.FromMilliseconds(3000),   // Timeout.InfiniteTimeSpan for no timeout (was 0)
    ProcessorSeries = ProcessorSeries.iQR,
});

// With the default settings
using var mcpx = McpX.Connect("192.168.12.88", 10000);

// Asynchronously (does not block a thread)
await using var mcpx = await McpX.ConnectAsync(options, cancellationToken);
```

`IsAscii`, `RequestFrame` and `ProcessorSeries` on `McpX` are read-only. `McpX` is sealed.

## GX Simulator3

`McpXSimulator` is now a static class.

```csharp
// 0.x
using var sim = new McpXSimulator(systemNo: 2, cpuNo: 1, ip: "192.168.12.90", timeoutMilliseconds: 10000);

// 1.0
using var sim = McpXSimulator.Connect(systemNo: 2, cpuNo: 1, host: "192.168.12.90");

// To change other settings
var options = McpXSimulator.CreateOptions(systemNo: 2, cpuNo: 1, host: "192.168.12.90");
options.Timeout = TimeSpan.FromSeconds(10);
using var sim = McpX.Connect(options);
```

The `SystemNo` / `CpuNo` properties were removed. Keep the values you passed.

## Typed methods

The typed wrappers (`ReadInt16`, `BatchReadSingleAsync`, `WriteDouble`, ...) were removed. Use the generic methods.

| 0.x | 1.0 |
|---|---|
| `ReadInt16(prefix, address)` | `Read<short>(prefix, address)` |
| `BatchReadSingle(prefix, address, length)` | `BatchRead<float>(prefix, address, length)` |
| `WriteDouble(prefix, address, value)` | `Write<double>(prefix, address, value)` |
| `BatchWriteUInt32Async(prefix, address, values)` | `BatchWriteAsync<uint>(prefix, address, values)` |

## Random access and monitoring

The tuple-based methods were removed. Use the builders.

```csharp
// 0.x
var (words, doubleWords) = mcpx.RandomRead<short, int>(
    [(Prefix.D, "0"), (Prefix.D, "10")],
    [(Prefix.D, "20")]);

// 1.0
short d0 = 0, d10 = 0;
int d20 = 0;
mcpx.RandomRead(b => b
    .Add<short>(Prefix.D, "0", v => d0 = v)
    .Add<short>(Prefix.D, "10", v => d10 = v)
    .Add<int>(Prefix.D, "20", v => d20 = v));
```

```csharp
// 0.x
mcpx.RandomWrite<short, int>([(Prefix.D, "0", (short)1)], [(Prefix.D, "20", 2)]);
mcpx.RandomWriteBit([(Prefix.M, "0", true)]);

// 1.0 (bit, word and double word can be mixed)
mcpx.RandomWrite(b => b
    .Add<short>(Prefix.D, "0", 1)
    .Add<int>(Prefix.D, "20", 2)
    .Add(Prefix.M, "0", true));
```

```csharp
// 0.x
mcpx.MonitorRegist([(Prefix.D, "0")], [(Prefix.D, "20")]);
var (words, doubleWords) = mcpx.Monitor<short, int>([(Prefix.D, "0")], [(Prefix.D, "20")]);

// 1.0
var session = mcpx.MonitorRegister(b => b
    .Add<short>(Prefix.D, "0", v => d0 = v)
    .Add<int>(Prefix.D, "20", v => d20 = v));
session.Read();   // repeat as needed
```

`MonitorRegist` / `MonitorRegistAsync` (builder overloads) were renamed to `MonitorRegister` / `MonitorRegisterAsync`.

## Other changes

| Change | What to do |
|---|---|
| `BatchWrite` / `BatchWriteAsync` return nothing | Use the array you passed instead of the return value |
| Lengths are `int` (`BatchRead`, `ReadString`, `ReadBuilder.Add`, `BlockReadBuilder.Add`) | No change for literals. Remove `(ushort)` casts if you like |
| `ReadString(prefix, address, length)` parameter renamed to `wordLength` | Rename named arguments |
| `RemoteReset(int reconnectTimeoutMilliseconds)` takes `TimeSpan? reconnectTimeout` | `RemoteReset(TimeSpan.FromSeconds(30))`, `TimeSpan.Zero` not to reconnect |
| Every async method takes an optional `CancellationToken` | Pass one to cancel. Cancelling a TCP request in progress closes the connection |
| `RecivePacketException` renamed to `ReceivePacketException` | Rename `catch` clauses. All library exceptions derive from `McpXException` |
| Namespace `McpXLib.Abstructs` renamed to `McpXLib.Abstracts` | The types in it are no longer public; remove the `using` |
| `Mcp`, `BasePlc`, `Request` / `RequestAsync`, `Route` and the interfaces in `McpXLib.Interfaces` are no longer public | Use the `McpX` methods. For another station, use `McpXOptions.Route` |
| Builders (`ReadBuilder`, ...) cannot be created with `new` | Use the builder passed to the `McpX` method |
| `PlcDuplicationException` removed | Remove `catch` clauses |
| Calling a method after `Dispose` throws `ObjectDisposedException` (was `IOException` over TCP) | Catch `ObjectDisposedException` if you relied on it |
| `bool` on a word device throws `ArgumentException` before sending (was `McProtocolException` C05C, or bit 0 of the word for random read / monitor) | Use a word type such as `short`, or a bit device. To read a bit of a word, read `ushort` and test the bit |
| Q/L addresses above FFFFFF throw `DeviceAddressException` | Use `ProcessorSeries.iQR` for such addresses |
| .NET Framework 4.6.2 or later is required for the netstandard2.0 build | Update from 4.6.1 |
