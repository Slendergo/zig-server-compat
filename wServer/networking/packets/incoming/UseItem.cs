﻿using common;

namespace wServer.networking.packets.incoming;

public class UseItem : IncomingMessage
{
    public long Time { get; set; }
    public ObjectSlot SlotObject { get; set; }
    public Position ItemUsePos { get; set; }
    public byte UseType { get; set; }

    public override C2SPacketId C2SId => C2SPacketId.UseItem;
    public override Packet CreateInstance() { return new UseItem(); }

    protected override void Read(NReader rdr)
    {
        Time = rdr.ReadInt64();
        SlotObject = ObjectSlot.Read(rdr);
        ItemUsePos = Position.Read(rdr);
        UseType = rdr.ReadByte();
    }
    protected override void Write(NWriter wtr)
    {
        wtr.Write(Time);
        SlotObject.Write(wtr);
        ItemUsePos.Write(wtr);
        wtr.Write(UseType);
    }
}