namespace McpXLib.Interfaces;


public interface IContent : IByteConvertible
{
    public byte[] MonitoringTimer { get; }
    public byte[] Command { get; }
    public byte[] SubCommand { get; }
    public byte[] CommandContent { get; }
}
