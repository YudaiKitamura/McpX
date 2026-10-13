using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TestMcpX")]
[assembly: InternalsVisibleTo("TestMcpXInterop")]
// テストの Moq が internal のインターフェースをモックするため
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
