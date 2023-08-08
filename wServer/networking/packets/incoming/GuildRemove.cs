using common;

namespace wServer.networking.packets.incoming;

class GuildRemove : IncomingMessage
{
    public string Name;

    public override C2SPacketId C2SId => C2SPacketId.GuildRemove;
    public override Packet CreateInstance() { return new GuildRemove(); }

    protected override void Read(NReader rdr)
    {
        Name = rdr.ReadUTF();
    }

    protected override void Write(NWriter wtr)
    {
        wtr.WriteUTF(Name);
    }
}