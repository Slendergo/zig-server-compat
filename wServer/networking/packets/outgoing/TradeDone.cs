﻿using common;

namespace wServer.networking.packets.outgoing;

public class TradeDone : OutgoingMessage
{
    public int Code { get; set; }
    public string Description { get; set; }

    public override S2CPacketId S2CId => S2CPacketId.TradeDone;
    public override Packet CreateInstance() { return new TradeDone(); }

    protected override void Read(NReader rdr)
    {
        Code = rdr.ReadInt32();
        Description = rdr.ReadUTF();
    }

    protected override void Write(NWriter wtr)
    {
        wtr.Write(Code);
        wtr.WriteUTF(Description);
    }
}