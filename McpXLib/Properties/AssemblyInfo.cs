using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("McpXLib.TextMcpX")]
[assembly: InternalsVisibleTo("TestMcpXInterop")]
// テストの Moq が internal のインターフェースをモックするため
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
