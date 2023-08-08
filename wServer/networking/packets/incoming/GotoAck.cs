using common;

namespace wServer.networking.packets.incoming;

public class GotoAck : IncomingMessage
{
    public int Time { get; set; }

    public override C2SPacketId C2SId => C2SPacketId.GotoAck;
    public override Packet CreateInstance() { return new GotoAck(); }

    protected override void Read(NReader rdr)
    {
        Time = rdr.ReadInt32();
    }
    protected override void Write(NWriter wtr)
    {
        wtr.Write(Time);
    }
}