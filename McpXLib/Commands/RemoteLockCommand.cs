using System.Text;
using McpXLib.Abstructs;
using McpXLib.Interfaces;
using McpXLib.Helpers;

namespace McpXLib.Commands;

public sealed class RemoteLockCommand : BaseCommand, IPlcCommand<bool>
{
    public RemoteLockCommand(string password) : base()
    {
        var commandContent = new List<byte>();
        commandContent.AddRange(BitConverter.GetBytes((ushort)password.Length));
        commandContent.AddRange(Encoding.ASCII.GetBytes(password));

        content = new ContentPacketHelper(
            command: [0x31, 0x16],
            subCommand: [0x00, 0x00],
            commandContent: commandContent.ToArray(),
            monitoringTimer: [0x00, 0x00]
        );
    }

    public void ValidatePramater()
    {
    }

    public async Task<bool> ExecuteAsync(Interfaces.IPlc plc)
    {
        Route = plc.Route;
        return new ResponsePacketHelper(
            await plc.RequestAsync(ToBytes())
        ).errCode == 0;
    }

    public bool Execute(Interfaces.IPlc plc)
    {
        Route = plc.Route;
        return new ResponsePacketHelper(
            plc.Request(ToBytes())
        ).errCode == 0;
    }
}
