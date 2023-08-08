using common;

namespace wServer.networking.packets.incoming;

public class EditAccountList : IncomingMessage
{
    public int AccountListId { get; set; }
    public bool Add { get; set; }
    public int ObjectId { get; set; }

    public override C2SPacketId C2SId => C2SPacketId.EditAccountList;
    public override Packet CreateInstance() { return new EditAccountList(); }

    protected override void Read(NReader rdr)
    {
        AccountListId = rdr.ReadInt32();
        Add = rdr.ReadBoolean();
        ObjectId = rdr.ReadInt32();
    }

    protected override void Write(NWriter wtr)
    {
        wtr.Write(AccountListId);
        wtr.Write(Add);
        wtr.Write(ObjectId);
    }
}