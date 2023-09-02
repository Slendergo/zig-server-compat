using common;

namespace wServer.networking.packets.incoming;

public class GroundDamage : IncomingMessage
{
    public long Time { get; set; }
    public Position Position { get; set; }

    public override C2SPacketId C2SId => C2SPacketId.GroundDamage;
    public override Packet CreateInstance() { return new GroundDamage(); }

    protected override void Read(NReader rdr)
    {
        Time = rdr.ReadInt64();
        Position = Position.Read(rdr);
    }
    protected override void Write(NWriter wtr)
    {
        wtr.Write(Time);
        Position.Write(wtr);
    }
}