using wServer.networking.packets;
using wServer.networking.packets.incoming;

namespace wServer.networking.handlers;

class SetConditionHandler : PacketHandlerBase<SetCondition>
{
    public override C2SPacketId C2SId => C2SPacketId.SetCondition;

    protected override void HandlePacket(Client client, SetCondition packet)
    {
        //TODO: implement something
    }
}