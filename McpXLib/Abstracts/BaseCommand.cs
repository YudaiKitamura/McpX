using McpXLib.Helpers;
using McpXLib.Interfaces;

namespace McpXLib.Abstructs;

public abstract class BaseCommand : I3ERequestFrame
{
    public byte[] SubHeader => subHeader;
    public IRoute Route 
    {
        get 
        {
            return route;
        }
        set
        {
            route = value;
        }
    }

    public IContent Content => content;

    internal readonly byte[] subHeader;
    private IRoute route;
    internal IContent content;

    public BaseCommand()
    {
        subHeader = [0x50, 0x00];
        route = new RoutePacketHelper();
        content = new ContentPacketHelper([], [], [], []);
    }

    public virtual byte[] ToBytes()
    {
        var packets = new List<byte>();
        packets.AddRange(SubHeader);
        packets.AddRange(Route.ToBytes());
        packets.AddRange(Content.ToBytes());

        return packets.ToArray();
    }

    public virtual void Validation()
    {
        throw new NotImplementedException();
    }
}
