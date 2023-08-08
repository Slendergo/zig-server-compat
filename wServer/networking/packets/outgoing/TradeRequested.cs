﻿using common;

namespace wServer.networking.packets.outgoing;

public class TradeRequested : OutgoingMessage
{
    public string Name { get; set; }

    public override S2CPacketId S2CId => S2CPacketId.TradeRequested;
    public override Packet CreateInstance() { return new TradeRequested(); }

    protected override void Read(NReader rdr)
    {
        Name = rdr.ReadUTF();
    }

    protected override void Write(NWriter wtr)
    {
        wtr.WriteUTF(Name);
    }
}