using NLog;
using wServer.networking.packets;
using wServer.networking.packets.incoming;
using wServer.realm.worlds.logic;

namespace wServer.networking;

interface IPacketHandler
{
    C2SPacketId C2SId { get; }
    void Handle(Client client, IncomingMessage packet);
}

abstract class PacketHandlerBase<T> : IPacketHandler where T : IncomingMessage
{
    protected static readonly Logger Log = LogManager.GetCurrentClassLogger();

    protected abstract void HandlePacket(Client client, T packet);

    public virtual C2SPacketId C2SId => C2SPacketId.Unknown;

    public void Handle(Client client, IncomingMessage packet)
    {
        HandlePacket(client, (T)packet);
    }

    protected bool IsTest(Client cli)
    {
        return cli?.Player?.Owner is Test;
    }
}

class PacketHandlers
{
    public static readonly Dictionary<C2SPacketId, IPacketHandler> C2SHandlers = new();
    
    static PacketHandlers()
    {
        foreach (var i in typeof(Packet).Assembly.GetTypes())
            if (typeof(IPacketHandler).IsAssignableFrom(i) &&
                !i.IsAbstract && !i.IsInterface)
            {
                IPacketHandler pkt = (IPacketHandler)Activator.CreateInstance(i);
                if (pkt == null) continue;
                if (pkt.C2SId != C2SPacketId.Unknown)
                    C2SHandlers.Add(pkt.C2SId, pkt);
            }
    }
}