using wServer.networking.packets;
using wServer.networking.packets.incoming;

namespace wServer.networking.handlers;

class OtherHitHandler : PacketHandlerBase<OtherHit>
{
    public override C2SPacketId C2SId => C2SPacketId.OtherHit;

    protected override void HandlePacket(Client client, OtherHit packet)
    {
    }
}