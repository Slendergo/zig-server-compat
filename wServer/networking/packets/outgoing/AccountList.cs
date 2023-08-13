using common;

namespace wServer.networking.packets.outgoing;

public class AccountList : OutgoingMessage
{
    public int AccountListId { get; set; }
    public int[] AccountIds { get; set; }

    public override S2CPacketId S2CId => S2CPacketId.AccountList;
    public override Packet CreateInstance() { return new AccountList(); }

    protected override void Read(NReader rdr)
    {
        AccountListId = rdr.ReadInt32();
        AccountIds = new int[rdr.ReadUInt16()];
        for (int i = 0; i < AccountIds.Length; i++)
            AccountIds[i] = rdr.ReadInt32();
    }

    protected override void Write(NWriter wtr)
    {
        wtr.Write(AccountListId);
        wtr.Write((ushort)AccountIds.Length);
        foreach (var i in AccountIds)
            wtr.Write(i);
    }
}