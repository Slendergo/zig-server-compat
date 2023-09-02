﻿using common;

namespace wServer.networking.packets.incoming;

public class PlayerShoot : IncomingMessage
{
    public long Time { get; set; }
    public byte BulletId { get; set; }
    public ushort ContainerType { get; set; }
    public Position StartingPos { get; set; }
    public float Angle { get; set; }

    public override C2SPacketId C2SId => C2SPacketId.PlayerShoot;
    public override Packet CreateInstance() { return new PlayerShoot(); }

    protected override void Read(NReader rdr)
    {
        Time = rdr.ReadInt64();
        BulletId = rdr.ReadByte();
        ContainerType = rdr.ReadUInt16();
        StartingPos = Position.Read(rdr);
        Angle = rdr.ReadSingle();
    }
    protected override void Write(NWriter wtr)
    {
        wtr.Write(Time);
        wtr.Write(BulletId);
        wtr.Write(ContainerType);
        StartingPos.Write(wtr);
        wtr.Write(Angle);
    }
}