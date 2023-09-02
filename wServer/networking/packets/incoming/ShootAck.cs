using common;

namespace wServer.networking.packets.incoming;

public class ShootAck : IncomingMessage
{
    public long Time { get; set; }

    public override C2SPacketId C2SId => C2SPacketId.ShootAck;
    public override Packet CreateInstance() { return new ShootAck(); }

    protected override void Read(NReader rdr)
    {
        Time = rdr.ReadInt64();
    }
    protected override void Write(NWriter wtr)
    {
        wtr.Write(Time);
    }
}