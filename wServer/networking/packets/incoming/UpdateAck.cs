using common;

namespace wServer.networking.packets.incoming;

public class UpdateAck : IncomingMessage
{
    public override C2SPacketId C2SId => C2SPacketId.UpdateAck;
    public override Packet CreateInstance() { return new UpdateAck(); }

    protected override void Read(NReader rdr)
    {
    }

    protected override void Write(NWriter wtr)
    {
    }
}