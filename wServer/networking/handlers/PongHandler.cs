using wServer.networking.packets;
using wServer.networking.packets.incoming;
using wServer.realm;

namespace wServer.networking.handlers;

class PongHandler : PacketHandlerBase<Pong>
{
    public override C2SPacketId C2SId => C2SPacketId.Pong;

    protected override void HandlePacket(Client client, Pong packet)
    {
        client.Manager.Logic.AddPendingAction(t => Handle(client, packet, t));
    }

    private void Handle(Client client, Pong packet, RealmTime t)
    {
        client.Player?.Pong(t, packet);
    }
}