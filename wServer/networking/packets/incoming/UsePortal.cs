using common;

namespace wServer.networking.packets.incoming;

public class UsePortal : IncomingMessage
{
    public int ObjectId { get; set; }

    public override C2SPacketId C2SId => C2SPacketId.UsePortal;
    public override Packet CreateInstance() { return new UsePortal(); }

    protected override void Read(NReader rdr)
    {
        ObjectId = rdr.ReadInt32();
    }
    protected override void Write(NWriter wtr)
    {
        wtr.Write(ObjectId);
    }
}