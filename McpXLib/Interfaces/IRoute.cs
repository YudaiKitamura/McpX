namespace McpXLib.Interfaces;


public interface IRoute : IByteConvertible
{
    public byte NetworkNumber { get; }
    public byte PCNumber { get; }
    public byte UnitNumber { get; }
    public byte IONumber { get; }
    public byte StationNumber { get; }
}
