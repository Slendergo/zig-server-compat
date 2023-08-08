using common;

namespace wServer.networking.packets.incoming;

public class SetCondition : IncomingMessage
{
    public int ConditionEffect { get; set; }
    public float ConditionDuration { get; set; }

    public override C2SPacketId C2SId => C2SPacketId.SetCondition;
    public override Packet CreateInstance() { return new SetCondition(); }

    protected override void Read(NReader rdr)
    {
        ConditionEffect = rdr.ReadInt32();
        ConditionDuration = rdr.ReadSingle();
    }

    protected override void Write(NWriter wtr)
    {
        wtr.Write(ConditionEffect);
        wtr.Write(ConditionDuration);
    }
}