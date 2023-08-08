using common;

namespace wServer.networking.packets.incoming;

public class PlayerText : IncomingMessage
{
    public string Text { get; set; }

    public override C2SPacketId C2SId => C2SPacketId.PlayerText;
    public override Packet CreateInstance() { return new PlayerText(); }

    protected override void Read(NReader rdr)
    {
        Text = rdr.ReadUTF();
    }

    protected override void Write(NWriter wtr)
    {
        wtr.WriteUTF(Text);
    }
}