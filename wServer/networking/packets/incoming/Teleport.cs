using common;

namespace wServer.networking.packets.incoming;

public class Teleport : IncomingMessage
{
    public int ObjectId { get; set; }

    public override C2SPacketId C2SId => C2SPacketId.Teleport;
    public override Packet CreateInstance() { return new Teleport(); }

    protected override void Read(NReader rdr)
    {
        ObjectId = rdr.ReadInt32();
    }
    protected override void Write(NWriter wtr)
    {
        wtr.Write(ObjectId);
    }
}