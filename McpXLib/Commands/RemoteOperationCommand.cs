using McpXLib.Builders;
using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Utils;

namespace McpXLib.Commands;

/// <summary>
/// リモート操作（コマンド: 1001 / 1002 / 1003 / 1005 / 1006）
/// </summary>
/// <remarks>
/// いずれもサブコマンド 0000、応答データなし。
/// </remarks>
internal sealed class RemoteOperationCommand : IPlcCommand<bool>
{
    // モード: 他の外部機器からリモートSTOP/PAUSE中の場合に、強制実行しない / 強制実行する
    private const uint MODE_NOT_FORCE = 0x0001;
    private const uint MODE_FORCE = 0x0003;

    private readonly CommandPacketBuilder commandPacketBuilder;

    private RemoteOperationCommand(byte command, NumberPayloadBuilder payloadBuilder, ushort monitoringTimer)
    {
        commandPacketBuilder = new CommandPacketBuilder(
            command: [command, 0x10],
            subCommand: [0x00, 0x00],
            payloadBuilder: payloadBuilder,
            monitoringTimer: monitoringTimer
        );
    }

    // リモートRUN（1001）: モード(2) + クリアモード(1) + 固定値 0(1)
    internal static RemoteOperationCommand Run(bool force, RemoteRunClearMode clearMode, ushort monitoringTimer = 0)
        => new(0x01, new NumberPayloadBuilder((force ? MODE_FORCE : MODE_NOT_FORCE, 2), ((uint)clearMode, 1), (0, 1)), monitoringTimer);

    // リモートSTOP（1002）: 固定値 0001H
    internal static RemoteOperationCommand Stop(ushort monitoringTimer = 0)
        => new(0x02, new NumberPayloadBuilder((0x0001, 2)), monitoringTimer);

    // リモートPAUSE（1003）: モード(2)
    internal static RemoteOperationCommand Pause(bool force, ushort monitoringTimer = 0)
        => new(0x03, new NumberPayloadBuilder((force ? MODE_FORCE : MODE_NOT_FORCE, 2)), monitoringTimer);

    // リモートラッチクリア（1005）: 固定値 0001H
    internal static RemoteOperationCommand LatchClear(ushort monitoringTimer = 0)
        => new(0x05, new NumberPayloadBuilder((0x0001, 2)), monitoringTimer);

    // リモートRESET（1006）: 固定値 0001H
    internal static RemoteOperationCommand Reset(ushort monitoringTimer = 0)
        => new(0x06, new NumberPayloadBuilder((0x0001, 2)), monitoringTimer);

    public async Task<bool> ExecuteAsync(IPlc plc)
    {
        var requestFrameSelector = new RequestFrameSelector(plc, commandPacketBuilder);
        var responseFrameSelector = new ResponseFrameSelector(
            plc,
            requestFrameSelector.GetSerialNumber()
        );

        responseFrameSelector.ParsePacket(
            await plc.RequestAsync(requestFrameSelector.GetRequestPacket(), responseFrameSelector)
        );

        return true;
    }

    public bool Execute(IPlc plc)
    {
        var requestFrameSelector = new RequestFrameSelector(plc, commandPacketBuilder);
        var responseFrameSelector = new ResponseFrameSelector(
            plc,
            requestFrameSelector.GetSerialNumber()
        );

        responseFrameSelector.ParsePacket(
            plc.Request(requestFrameSelector.GetRequestPacket(), responseFrameSelector)
        );

        return true;
    }

    public byte[] ToBinaryBytes()
    {
        return commandPacketBuilder.ToBinaryBytes();
    }

    public byte[] ToAsciiBytes()
    {
        return commandPacketBuilder.ToAsciiBytes();
    }
}
