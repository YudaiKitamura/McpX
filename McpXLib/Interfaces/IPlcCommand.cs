namespace McpXLib.Interfaces;


internal interface IPlcCommand<T> : IPacketBuilder
{
    Task<T> ExecuteAsync(IPlc plc, CancellationToken cancellationToken);
    T Execute(IPlc plc);
}
