namespace McpXLib.Interfaces;


public interface I3EResponseFrame
{
    public byte[] SubHeader { get; }
    public IRoute Route { get; }
    public byte[] Content { get; }
    public string ErrCode { get; }
}
