namespace McpXLib.Interfaces;

public interface IPlc 
{
    bool IsAscii { get; set; }
    IRoute Route { get; set; }
    Task<byte[]> RequestAsync(byte[] packet); 
    byte[] Request(byte[] packet);
}
