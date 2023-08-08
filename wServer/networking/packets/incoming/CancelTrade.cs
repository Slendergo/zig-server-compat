using common;

namespace wServer.networking.packets.incoming;

public class CancelTrade : IncomingMessage
{
    public override C2SPacketId C2SId => C2SPacketId.CancelTrade;
    public override Packet CreateInstance() { return new CancelTrade(); }

    protected override void Read(NReader rdr) { }

    protected override void Write(NWriter wtr) { }
}