using common;

namespace wServer.networking.packets.incoming;

public class Move : IncomingMessage
{
    public int TickId { get; set; }
    public long Time { get; set; }
    public Position NewPosition { get; set; }
    public MoveRecord[] Records { get; set; }

    public override C2SPacketId C2SId => C2SPacketId.Move;
    public override Packet CreateInstance() { return new Move(); }

    protected override void Read(NReader rdr)
    {
        TickId = rdr.ReadInt32();
        Time = rdr.ReadInt64();
        NewPosition = Position.Read(rdr);
        Records = new MoveRecord[rdr.ReadInt16()];
        for (var i = 0; i < Records.Length; i++)
            Records[i] = MoveRecord.Read(rdr);
    }
    protected override void Write(NWriter wtr)
    {
        wtr.Write(TickId);
        wtr.Write(Time);
        NewPosition.Write(wtr);
        wtr.Write((ushort)Records.Length);
        foreach (var i in Records)
            i.Write(wtr);
    }
}