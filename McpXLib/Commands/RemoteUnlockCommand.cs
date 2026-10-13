using McpXLib.Builders;
using McpXLib.Utils;

namespace McpXLib.Commands;

internal sealed class RemoteUnlockCommand : PlcCommandBase<bool>
{
    
    internal RemoteUnlockCommand(string password, ushort monitoringTimer = 0)
    {
        commandPacketBuilder = new CommandPacketBuilder(
            command: [0x30, 0x16],
            subCommand: [0x00, 0x00],
            payloadBuilder: new AsciiPayloadBuilder(password),
            monitoringTimer: monitoringTimer
        );
    }

    protected override bool Parse(byte[] responseContent)
        => true;

    }
