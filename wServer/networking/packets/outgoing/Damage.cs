using common;
using common.resources;

namespace wServer.networking.packets.outgoing;

public class Damage : OutgoingMessage
{
    public int TargetId { get; set; }
    public ConditionEffects Effects { get; set; }
    public ushort DamageAmount { get; set; }
    public bool Kill { get; set; }
    public byte BulletId { get; set; }
    public int ObjectId { get; set; }

    public override S2CPacketId S2CId => S2CPacketId.Damage;
    public override Packet CreateInstance() { return new Damage(); }

    protected override void Read(NReader rdr)
    {
        TargetId = rdr.ReadInt32();
        byte c = rdr.ReadByte();
        Effects = 0;
        for (int i = 0; i < c; i++)
            Effects |= (ConditionEffects)(1 << rdr.ReadByte());
        DamageAmount = rdr.ReadUInt16();
        Kill = rdr.ReadBoolean();
        BulletId = rdr.ReadByte();
        ObjectId = rdr.ReadInt32();
    }
    protected override void Write(NWriter wtr)
    {
        wtr.Write(TargetId);
        wtr.Write((ulong)Effects);
        wtr.Write(DamageAmount);
        wtr.Write(Kill);
        wtr.Write(BulletId);
        wtr.Write(ObjectId);
    }
}