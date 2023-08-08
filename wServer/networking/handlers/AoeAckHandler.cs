using wServer.networking.packets;
using wServer.networking.packets.incoming;

namespace wServer.networking.handlers;

class AoeAckHandler : PacketHandlerBase<AoeAck>
{
    public override C2SPacketId C2SId => C2SPacketId.AoeAck;

    protected override void HandlePacket(Client client, AoeAck packet)
    {
        //TODO: implement something
    }
}