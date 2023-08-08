using common;

namespace wServer.networking.packets.incoming;

class CreateGuild : IncomingMessage
{
    public string Name;

    public override C2SPacketId C2SId => C2SPacketId.CreateGuild;
    public override Packet CreateInstance() { return new CreateGuild(); }

    protected override void Read(NReader rdr)
    {
        Name = rdr.ReadUTF();
    }

    protected override void Write(NWriter wtr)
    {
        wtr.WriteUTF(Name);
    }
}