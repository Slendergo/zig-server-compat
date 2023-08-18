﻿using common;

namespace wServer.networking.packets.outgoing;

public class ServerPlayerShoot : OutgoingMessage
{
    public byte BulletId { get; set; }
    public int OwnerId { get; set; }
    public ushort ContainerType { get; set; }
    public Position StartingPos { get; set; }
    public float Angle { get; set; }
    public short Damage { get; set; }

    public override S2CPacketId S2CId => S2CPacketId.ServerPlayerShoot;
    public override Packet CreateInstance() { return new ServerPlayerShoot(); }

    protected override void Read(NReader rdr)
    {
        BulletId = rdr.ReadByte();
        OwnerId = rdr.ReadInt32();
        ContainerType = rdr.ReadUInt16();
        StartingPos = Position.Read(rdr);
        Angle = rdr.ReadSingle();
        Damage = rdr.ReadInt16();
    }
    protected override void Write(NWriter wtr)
    {
        wtr.Write(BulletId);
        wtr.Write(OwnerId);
        wtr.Write(ContainerType);
        StartingPos.Write(wtr);
        wtr.Write(Angle);
        wtr.Write(Damage);
    }
}