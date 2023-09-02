using common;

namespace wServer.networking.packets.incoming;

public class Pong : IncomingMessage
{
    public int Serial { get; set; }
    public long Time { get; set; }

    public override C2SPacketId C2SId => C2SPacketId.Pong;
    public override Packet CreateInstance() { return new Pong(); }

    protected override void Read(NReader rdr)
    {
        Serial = rdr.ReadInt32();
        Time = rdr.ReadInt64();
    }
    protected override void Write(NWriter wtr)
    {
        wtr.Write(Serial);
        wtr.Write(Time);
    }
}