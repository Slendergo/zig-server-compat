using common;

namespace wServer.networking.packets.incoming;

class JoinGuild : IncomingMessage
{
    public string GuildName;

    public override C2SPacketId C2SId => C2SPacketId.JoinGuild;
    public override Packet CreateInstance() { return new JoinGuild(); }

    protected override void Read(NReader rdr)
    {
        GuildName = rdr.ReadUTF();
    }

    protected override void Write(NWriter wtr)
    {
        wtr.WriteUTF(GuildName);
    }
}