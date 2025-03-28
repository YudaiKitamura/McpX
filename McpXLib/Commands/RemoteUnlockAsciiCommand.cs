using McpXLib.Abstructs;
using McpXLib.Interfaces;
using McpXLib.Helpers;

namespace McpXLib.Commands;

public sealed class RemoteUnlockAsciiCommand : BaseAsciiCommand, IPlcCommand<bool>
{
    public RemoteUnlockAsciiCommand(string password) : base()
    {
        content = new ContentAsciiPacketHelper(
            command: "1630",
            subCommand: "0000",
            commandContent: $"{ password.Length.ToString("X").PadLeft(4, '0') }{ password }",
            monitoringTimer: "0000"
        );
    }

    public async Task<bool> ExecuteAsync(IPlc plc)
    {
        Route = plc.Route;
        return new ResponseAsciiPacketHelper(
            await plc.RequestAsync(ToBytes())
        ).errCode == 0;
    }

    public bool Execute(IPlc plc)
    {
        Route = plc.Route;
        return new ResponseAsciiPacketHelper(
            plc.Request(ToBytes())
        ).errCode == 0;
    }
}
