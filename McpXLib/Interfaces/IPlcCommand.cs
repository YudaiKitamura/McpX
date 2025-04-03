namespace McpXLib.Interfaces;


public interface IPlcCommand<T> : IPacketBuilder
{
    public Task<T> ExecuteAsync(IPlc plc);
    public T Execute(IPlc plc);
}
