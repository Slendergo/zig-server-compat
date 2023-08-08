using common;

namespace wServer.networking.packets.outgoing;

public class Ping : OutgoingMessage
{
    public int Serial { get; set; }

    public override S2CPacketId S2CId => S2CPacketId.Ping;
    public override Packet CreateInstance() { return new Ping(); }

    protected override void Read(NReader rdr)
    {
        Serial = rdr.ReadInt32();
    }
    protected override void Write(NWriter wtr)
    {
        wtr.Write(Serial);
    }
}