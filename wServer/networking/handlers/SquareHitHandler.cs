using wServer.networking.packets;
using wServer.networking.packets.incoming;

namespace wServer.networking.handlers;

class SquareHitHandler : PacketHandlerBase<SquareHit>
{
    public override C2SPacketId C2SId => C2SPacketId.SquareHit;

    protected override void HandlePacket(Client client, SquareHit packet)
    {
    }
}