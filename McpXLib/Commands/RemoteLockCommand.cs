using McpXLib.Interfaces;
using McpXLib.Builders;
using McpXLib.Utils;

namespace McpXLib.Commands;

internal sealed class RemoteLockCommand : IPlcCommand<bool>
{
    private readonly CommandPacketBuilder commandPacketBuilder;
    
    internal RemoteLockCommand(string password, ushort monitoringTimer)
    {
        commandPacketBuilder = new CommandPacketBuilder(
            command: [0x31, 0x16],
            subCommand: [0x00, 0x00],
            payloadBuilder: new AsciiPayloadBuilder(password),
            monitoringTimer: monitoringTimer
        );
    }

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
