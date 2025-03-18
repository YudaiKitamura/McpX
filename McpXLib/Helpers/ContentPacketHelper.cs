using System.Text;
using McpXLib.Interfaces;

namespace McpXLib.Helpers;

public class ContentPacketHelper : IContent
{
    public byte[] MonitoringTimer => monitoringTimer;
    public byte[] Command => command;
    public byte[] SubCommand => subCommand;
    public byte[] CommandContent => commandContent;

    private readonly byte[] monitoringTimer;
    private readonly byte[] command;
    private readonly byte[] subCommand;
    private readonly byte[] commandContent;

    public ContentPacketHelper(byte[] command, byte[] subCommand, byte[] commandContent , byte[] monitoringTimer)
    {
        this.command = command;
        this.subCommand = subCommand;
        this.commandContent = commandContent;
        this.monitoringTimer = monitoringTimer;
    }

    public ContentPacketHelper(string command, string subCommand, string commandContent , string monitoringTimer)
    {
        this.command = Encoding.ASCII.GetBytes(command);
        this.subCommand = Encoding.ASCII.GetBytes(subCommand);
        this.commandContent = Encoding.ASCII.GetBytes(commandContent);
        this.monitoringTimer = Encoding.ASCII.GetBytes(monitoringTimer);
    }

    public byte[] ToBytes()
    {
        var packets = new List<byte>();
        packets.AddRange(MonitoringTimer);
        packets.AddRange(Command);
        packets.AddRange(SubCommand);
        packets.AddRange(CommandContent);

        packets.InsertRange(0, BitConverter.GetBytes((ushort)packets.Count));

        return packets.ToArray();
    }
}
