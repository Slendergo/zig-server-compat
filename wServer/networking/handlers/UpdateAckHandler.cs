using wServer.networking.packets;
using wServer.networking.packets.incoming;

namespace wServer.networking.handlers;

class UpdateAckHandler : PacketHandlerBase<UpdateAck>
{
    public override C2SPacketId C2SId => C2SPacketId.UpdateAck;

    protected override void HandlePacket(Client client, UpdateAck packet)
    {
        client.Player.UpdateAckReceived();
    }
}