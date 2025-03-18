namespace McpXLib.Interfaces;


public interface I3ERequestFrame : IByteConvertible
{
    public byte[] SubHeader { get; }
    public IRoute Route { get; set; }
    public IContent Content { get; }
}
