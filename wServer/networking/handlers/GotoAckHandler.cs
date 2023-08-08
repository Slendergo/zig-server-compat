using wServer.networking.packets;
using wServer.networking.packets.incoming;

namespace wServer.networking.handlers;

class GotoAckHandler : PacketHandlerBase<GotoAck>
{
    public override C2SPacketId C2SId => C2SPacketId.GotoAck;

    protected override void HandlePacket(Client client, GotoAck packet)
    {
        client.Player.GotoAckReceived();
    }
}