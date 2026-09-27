<img src="docfx/images/mcpx_ogp.png" alt="logo" />
<br>
<p>
  <img alt="Downloads" src="https://img.shields.io/nuget/dt/McpX" />
  <img alt="Version" src="https://img.shields.io/badge/version-0.11.0-blue" />
  <img alt=".NET 7.0+" src="https://img.shields.io/badge/.NET-7.0+-blueviolet" />
  <img alt=".NET 8.0+" src="https://img.shields.io/badge/.NET-8.0+-purple" />
  <img alt=".NET 9.0+" src="https://img.shields.io/badge/.NET-9.0+-indigo" />
  <img alt=".NET Core 2.0+" src="https://img.shields.io/badge/.NET_Core-2.0+-darkgreen" />
  <img alt=".NET Framework 4.6.1+" src="https://img.shields.io/badge/.NET_Framework-4.6.1+-teal?logo=windows" />
  <img alt="License" src="https://img.shields.io/badge/license-MIT-brightgreen.svg" />
  <a href="https://flatt.tech/oss/gmo/trampoline" target="_blank"><img src="https://flatt.tech/assets/images/badges/gmo-oss.svg" height="24px"/></a>
</p>

<p>
  <a href="README_JA.md">日本語</a> | <a href="README.md">English</a>
</p>

**McpX is a library for communicating with Mitsubishi Electric PLCs using the MC protocol.**  
It features a simple and easy-to-use API, allowing you to communicate without worrying about MC protocol details.  
It runs on various platforms, including Linux, Windows, and macOS.

## Installation
### .NET CLI
```sh
dotnet add package McpX
```
### Package Manager(Visual Studio)
```sh
PM> NuGet\Install-Package McpX
```

## Example Usage 
```csharp
using McpXLib;
using McpXLib.Enums;

// Connect to PLC by specifying IP and port
using (var mcpx = new McpX("192.168.12.88", 10000))
{
    // Read 7000 points starting from M0
    bool[] mArr = mcpx.BatchRead<bool>(Prefix.M, "0", 7000);
    
    // Read 7000 words starting from D1000
    short[] dArr = mcpx.BatchRead<short>(Prefix.D, "1000", 7000);

    // Write 1234 to D0 and 5678 to D1 as signed 32-bit integers
    mcpx.BatchWrite<int>(Prefix.D, "0", [1234, 5678]);

    // Read a range and single devices together (ranges: batch access, single: random access)
    short[] block = [];
    int total = 0;
    mcpx.Read(b => b
        .Add<short>(Prefix.D, "400", 100, v => block = v)
        .Add<int>(Prefix.D, "2000", v => total = v));
}
```
[C# and Visual Basic samples are available here.](https://github.com/YudaiKitamura/McpX/tree/main/Example)

### Connecting to GX Simulator3
`McpXSimulator` connects to GX Simulator3 (the simulation function of GX Works3) by system No. and CPU No.
The port is `5500 + system No. × 10 + CPU No.` (e.g. system 1 / CPU 1 = 5511, system 2 / CPU 1 = 5521).
Create one instance per simulator to connect to several simulators at the same time. The read/write API is the same as `McpX`.
```csharp
using var sim1 = new McpXSimulator();                                  // 127.0.0.1:5511 (system 1 / CPU 1)
using var sim2 = new McpXSimulator(systemNo: 2, ip: "192.168.12.90");  // 192.168.12.90:5521
using var cpu2 = new McpXSimulator(systemNo: 1, cpuNo: 2);             // 127.0.0.1:5512 (CPU No.2 of a multiple CPU system)

sim1.Write(Prefix.D, "100", (short)123);
short d0 = sim2.Read<short>(Prefix.D, "0");
int port = McpXSimulator.GetPort(systemNo: 2);                        // 5521
```
GX Simulator3 listens on 127.0.0.1 only. To connect from another PC, forward each port to 127.0.0.1 on the simulator PC (e.g. `netsh interface portproxy`).
Communication is TCP / binary code only (GX Simulator3 does not respond to ASCII code).

## Supported Commands

| Name                         | Description                                                            | Synchronous Method                                      | Asynchronous Method                                              |
|------------------------------|------------------------------------------------------------------------|---------------------------------------------------------|------------------------------------------------------------------|
| **Single Read**              | Reads a single value from the specified device.                        | `Read<T>(Prefix prefix, string address)`                | `ReadAsync<T>(Prefix prefix, string address)`                   |
| **Single Write**             | Writes a single value to the specified device.                         | `Write<T>(Prefix prefix, string address, T value)`     | `WriteAsync<T>(Prefix prefix, string address, T value)`         |
| **Batch Read**               | Reads multiple consecutive values starting from the specified address. | `BatchRead<T>(Prefix prefix, string address, ushort length)` | `BatchReadAsync<T>(Prefix prefix, string address, ushort length)` |
| **Batch Write**              | Writes an array of values to consecutive device addresses.             | `BatchWrite<T>(Prefix prefix, string address, T[] values)`   | `BatchWriteAsync<T>(Prefix prefix, string address, T[] values)` |
| **Random Read**              | Reads values from non-consecutive devices (bit / word / double-word). | `RandomRead(Action<RandomReadBuilder> build)`          | `RandomReadAsync(Action<RandomReadBuilder> build)`              |
| **Random Write**             | Writes values to non-consecutive devices (bit / word / double-word).  | `RandomWrite(Action<RandomWriteBuilder> build)`        | `RandomWriteAsync(Action<RandomWriteBuilder> build)`            |
| **Monitor Registration**     | Registers devices to monitor and returns a `MonitorSession`.           | `MonitorRegist(Action<MonitorBuilder> build)`          | `MonitorRegistAsync(Action<MonitorBuilder> build)`             |
| **Monitor Read**             | Reads the latest values of registered devices via the returned session. | `MonitorSession.Read()`                              | `MonitorSession.ReadAsync()`                                   |
| **Combined Read**            | Reads consecutive ranges (with a point count) and single devices together; ranges use batch access (multiple block read when there are several ranges), single devices use random access. | `Read(Action<ReadBuilder> build)`   | `ReadAsync(Action<ReadBuilder> build)`   |
| **Combined Write**           | Writes consecutive ranges (arrays) and single devices together; arrays use batch access (multiple block write when there are several ranges), single values use random access. | `Write(Action<WriteBuilder> build)` | `WriteAsync(Action<WriteBuilder> build)` |
| **Multiple Block Read**      | Reads multiple ranges (blocks) of consecutive devices in one request (command 0406). | `BlockRead(Action<BlockReadBuilder> build)`   | `BlockReadAsync(Action<BlockReadBuilder> build)`   |
| **Multiple Block Write**     | Writes multiple ranges (blocks) of consecutive devices in one request (command 1406). | `BlockWrite(Action<BlockWriteBuilder> build)` | `BlockWriteAsync(Action<BlockWriteBuilder> build)` |
| **Remote RUN / STOP / PAUSE** | Changes the operating status of the CPU (commands 1001 / 1002 / 1003). | `RemoteRun(bool force, RemoteRunClearMode clearMode)` / `RemoteStop()` / `RemotePause(bool force)` | `RemoteRunAsync(...)` / `RemoteStopAsync()` / `RemotePauseAsync(...)` |
| **Remote Latch Clear / RESET** | Executes latch clear or reset (commands 1005 / 1006). Execute while the CPU is stopped. | `RemoteLatchClear()` / `RemoteReset()` | `RemoteLatchClearAsync()` / `RemoteResetAsync()` |
| **Remote Password Lock/Unlock** | Automatically locks the PLC with the specified remote password when the instance is created and unlocks it when disposed. | `McpX(string ip, int port, string? password = null)`   | –                                                                |

## Supported Protocols
- TCP
- UDP
- 3E frame (binary code)
- 3E frame (ASCII code)
- 4E frame (binary code)
- 4E frame (ASCII code)

## Roadmap
- [x] ~~3E frame (ASCII code) support~~
- [x] ~~4E frame (binary code) support~~
- [x] ~~4E frame (ASCII code) support~~
- [x] ~~UDP support~~
- [x] ~~GX Simulator support~~

## Changelog
- [CHANGELOG.md](./CHANGELOG.md)

## Related
- [mcpx-mcp-server](https://github.com/YudaiKitamura/mcpx-mcp-server) An MCP(Model Context Protocol) server that enables real-time access to Mitsubishi Electric PLC devices from generative AI.
