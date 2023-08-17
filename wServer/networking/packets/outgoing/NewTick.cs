using common;

namespace wServer.networking.packets.outgoing;

public class NewTick : OutgoingMessage
{
    public int TickId { get; set; }
    public int TickTime { get; set; }
    public ObjectStats[] Statuses { get; set; }

    public override S2CPacketId S2CId => S2CPacketId.NewTick;
    public override Packet CreateInstance() { return new NewTick(); }

    protected override void Read(NReader rdr)
    {
    }

    protected override void Write(NWriter wtr)
    {
        wtr.Write(TickId);
        wtr.Write(TickTime);

        wtr.Write((ushort)Statuses.Length);
        foreach (var i in Statuses)
            i.Write(wtr);
    }
}