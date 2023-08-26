using wServer.networking.packets;
using wServer.networking.packets.incoming;
using wServer.realm.worlds;

namespace wServer.networking.handlers;

class EscapeHandler : PacketHandlerBase<Escape>
{
    public override C2SPacketId C2SId => C2SPacketId.Escape;

    protected override void HandlePacket(Client client, Escape packet)
    {
        //client.Manager.Logic.AddPendingAction(t => Handle(client, packet));
        Handle(client);
    }

    private void Handle(Client client)
    {
        if (client.Player == null || client.Player.Owner == null)
            return;

        var map = client.Player.Owner;
        if (map.Id == World.Nexus)
        {
            client.Player.SendInfo("Already in Nexus!");
            return;
        }

        client.Reconnect("Nexus", World.Nexus);
    }
}