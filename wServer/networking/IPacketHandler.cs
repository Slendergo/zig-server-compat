using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wServer.networking.packets;
using wServer.networking.packets.incoming;
using wServer.realm.worlds.logic;

namespace wServer.networking
{
    interface IPacketHandler
    {
        PacketId ID { get; }
        void Handle(Client client, IncomingMessage packet);
    }

    abstract class PacketHandlerBase<T> : IPacketHandler where T : IncomingMessage
    {
        protected static readonly Logger Log = LogManager.GetCurrentClassLogger();

        protected abstract void HandlePacket(Client client, T packet);

        public abstract PacketId ID { get; }

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
        public static Dictionary<PacketId, IPacketHandler> Handlers = new Dictionary<PacketId, IPacketHandler>();
        static PacketHandlers()
        {
            foreach (var i in typeof(Packet).Assembly.GetTypes())
                if (typeof(IPacketHandler).IsAssignableFrom(i) &&
                    !i.IsAbstract && !i.IsInterface)
                {
                    IPacketHandler pkt = (IPacketHandler)Activator.CreateInstance(i);
                    Handlers.Add(pkt.ID, pkt);
                }
        }
    }
}
