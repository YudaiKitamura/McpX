using McpXLib.Interfaces;

namespace McpXLib.Commands;

public class PlcCommandHandler<T>
{
    public async Task<T> ExecuteAsync(IPlcCommand<T> command, Mcp plc)
    {
        return await command.ExecuteAsync(plc);
    }

    public T Execute(IPlcCommand<T> command, Mcp plc)
    {
        return command.Execute(plc);
    }
}
