using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using McpXLib;

namespace McpXNative;

/// <summary>
/// ライブラリ情報とオプションの初期化（mcpx_abi_version / mcpx_version / mcpx_struct_size / *_options_init）。
/// </summary>
internal static unsafe class LibraryExports
{
    internal const uint AbiVersion = (1u << 16) | 0u;

    internal const ushort DefaultTimeoutMs = 5000;

    // mcpx_version が返す静的な UTF-8 文字列（プロセス終了まで解放しない）
    private static readonly byte* version = CreateVersion();

    private static byte* CreateVersion()
    {
        var text = typeof(McpX).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
        var bytes = Encoding.UTF8.GetBytes(text);
        var buffer = (byte*)NativeMemory.Alloc((nuint)bytes.Length + 1);
        bytes.AsSpan().CopyTo(new Span<byte>(buffer, bytes.Length));
        buffer[bytes.Length] = 0;
        return buffer;
    }

    [UnmanagedCallersOnly(EntryPoint = "mcpx_abi_version", CallConvs = [typeof(CallConvCdecl)])]
    public static uint GetAbiVersion() => AbiVersion;

    [UnmanagedCallersOnly(EntryPoint = "mcpx_version", CallConvs = [typeof(CallConvCdecl)])]
    public static byte* GetVersion() => version;

    [UnmanagedCallersOnly(EntryPoint = "mcpx_struct_size", CallConvs = [typeof(CallConvCdecl)])]
    public static uint GetStructSize(uint which) => StructSize(which);

    internal static uint StructSize(uint which)
    {
        return (McpxStruct)which switch
        {
            McpxStruct.Error => (uint)sizeof(NativeError),
            McpxStruct.ConnectOptions => (uint)sizeof(NativeConnectOptions),
            McpxStruct.SimulatorOptions => (uint)sizeof(NativeSimulatorOptions),
            McpxStruct.Item => (uint)sizeof(NativeItem),
            _ => 0,
        };
    }

    [UnmanagedCallersOnly(EntryPoint = "mcpx_connect_options_init", CallConvs = [typeof(CallConvCdecl)])]
    public static void ConnectOptionsInit(NativeConnectOptions* options)
    {
        if (options == null)
        {
            return;
        }

        *options = default;
        options->StructSize = (uint)sizeof(NativeConnectOptions);
        options->TimeoutMs = DefaultTimeoutMs;
    }

    [UnmanagedCallersOnly(EntryPoint = "mcpx_simulator_options_init", CallConvs = [typeof(CallConvCdecl)])]
    public static void SimulatorOptionsInit(NativeSimulatorOptions* options)
    {
        if (options == null)
        {
            return;
        }

        *options = default;
        options->StructSize = (uint)sizeof(NativeSimulatorOptions);
        options->SystemNo = 1;
        options->CpuNo = 1;
        options->TimeoutMs = DefaultTimeoutMs;
        options->Series = 1; // MCPX_SERIES_IQR（McpXSimulator の既定）
    }
}
