using common;

namespace wServer.networking.packets.incoming;

public class CheckCredits : IncomingMessage
{
    public override C2SPacketId C2SId => C2SPacketId.CheckCredits;
    public override Packet CreateInstance() { return new CheckCredits(); }

    protected override void Read(NReader rdr) { }

    protected override void Write(NWriter wtr) { }
}