namespace McpXLib.Interfaces;

public interface IPlc 
{
    IRoute Route { get; set; }
    Task<byte[]> RequestAsync(byte[] packet); 
    byte[] Request(byte[] packet);
}
