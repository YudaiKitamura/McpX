using System.Text;
using McpXLib.Helpers;
using McpXLib.Interfaces;

namespace McpXLib.Abstructs;

public abstract class BaseAsciiCommand : I3ERequestFrame
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

    public BaseAsciiCommand()
    {
        subHeader = Encoding.ASCII.GetBytes("5000");
        route = new RouteAsciiPacketHelper();
        content = new ContentAsciiPacketHelper("", "", "", "");
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
