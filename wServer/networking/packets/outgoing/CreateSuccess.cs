﻿using common;

namespace wServer.networking.packets.outgoing;

public class CreateSuccess : OutgoingMessage
{
    public int ObjectId { get; set; }
    public int CharId { get; set; }

    public override S2CPacketId S2CId => S2CPacketId.CreateSuccess;
    public override Packet CreateInstance() { return new CreateSuccess(); }

    protected override void Read(NReader rdr)
    {
        ObjectId = rdr.ReadInt32();
        CharId = rdr.ReadInt32();
    }

    protected override void Write(NWriter wtr)
    {
        wtr.Write(ObjectId);
        wtr.Write(CharId);
    }
}