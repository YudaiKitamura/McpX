namespace McpXLib.Interfaces;


public interface IPlcCommand<T>
{
    public Task<T> ExecuteAsync(IPlc plc);
    public T Execute(IPlc plc);
}
