namespace McpXLib.Interfaces;


internal interface IPlcCommand<T> : IPacketBuilder
{
    Task<T> ExecuteAsync(IPlc plc);
    T Execute(IPlc plc);
}
