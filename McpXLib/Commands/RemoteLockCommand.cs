using McpXLib.Interfaces;
using McpXLib.Helpers;
using McpXLib.Builders;

namespace McpXLib.Commands;

public sealed class RemoteLockCommand : IPlcCommand<bool>
{
    private readonly CommandPacketBuilder commandPacketBuilder;
    
    public RemoteLockCommand(string password)
    {
        commandPacketBuilder = new CommandPacketBuilder(
            command: [0x31, 0x16],
            subCommand: [0x00, 0x00],
            payloadBuilder: new AsciiPayloadBuilder(password),
            monitoringTimer: [0x00, 0x00]
        );
    }

    public void ValidatePramater()
    {
    }

    public RequestPacketBuilder GetPacketBuilder()
    {
        return new RequestPacketBuilder(
            subHeaderPacketBuilder: new SubHeaderPacketBuilder(),
            routePacketBuilder: new RoutePacketBuilder(),
            commandPacketBuilder: commandPacketBuilder
        );
    }

    public async Task<bool> ExecuteAsync(IPlc plc)
    {
        if (plc.IsAscii) 
        {
            return new ResponseAsciiPacketHelper(
                await plc.RequestAsync(GetPacketBuilder().ToAsciiBytes())
            ).errCode == 0;
        }
        else 
        {
            return new ResponsePacketHelper(
                await plc.RequestAsync(GetPacketBuilder().ToBinaryBytes())
            ).errCode == 0;
        }
    }

    public bool Execute(IPlc plc)
    {
        if (plc.IsAscii) 
        {
            return new ResponseAsciiPacketHelper(
                plc.Request(GetPacketBuilder().ToAsciiBytes())
            ).errCode == 0;
        }
        else 
        {
            return new ResponsePacketHelper(
                plc.Request(GetPacketBuilder().ToBinaryBytes())
            ).errCode == 0;
        }
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
