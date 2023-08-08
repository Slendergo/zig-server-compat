using wServer.networking.packets;
using wServer.networking.packets.incoming;

namespace wServer.networking.handlers;

class ShootAckHandler : PacketHandlerBase<ShootAck>
{
    public override C2SPacketId C2SId => C2SPacketId.ShootAck;

    protected override void HandlePacket(Client client, ShootAck packet)
    {
        //client.Player.ShootAckReceived();
    }
}