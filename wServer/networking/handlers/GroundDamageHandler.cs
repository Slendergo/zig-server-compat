using wServer.realm;
using wServer.realm.entities;
using wServer.networking.packets;
using wServer.networking.packets.incoming;

namespace wServer.networking.handlers;

class GroundDamageHandler : PacketHandlerBase<GroundDamage>
{
    public override C2SPacketId C2SId => C2SPacketId.GroundDamage;

    protected override void HandlePacket(Client client, GroundDamage packet)
    {
        client.Manager.Logic.AddPendingAction(t => Handle(client.Player,packet.Position, packet.Time));
    }

    void Handle(Player player, Position pos, long time)
    {
        if (player?.Owner == null)
            return;

        player.ForceGroundHit(pos, time);
    }
}