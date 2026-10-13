using McpXLib.Builders;
using McpXLib.Utils;

namespace McpXLib.Commands;

internal sealed class RemoteLockCommand : PlcCommandBase<bool>
{
    
    internal RemoteLockCommand(string password, ushort monitoringTimer = 0)
    {
        commandPacketBuilder = new CommandPacketBuilder(
            command: [0x31, 0x16],
            subCommand: [0x00, 0x00],
            payloadBuilder: new AsciiPayloadBuilder(password),
            monitoringTimer: monitoringTimer
        );
    }

    protected override bool Parse(byte[] responseContent)
        => true;
}
