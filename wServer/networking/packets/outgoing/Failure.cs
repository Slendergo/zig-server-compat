using common;

namespace wServer.networking.packets.outgoing;

public class Failure : OutgoingMessage
{
    public const int MessageWithDisconnect = 0;
    public const int ClientUpdateNeeded = 1;
    public const int ForceCloseGame = 2;

    public int ErrorId { get; set; }
    public string ErrorDescription { get; set; }

    public override S2CPacketId S2CId => S2CPacketId.Failure;
    public override Packet CreateInstance() { return new Failure(); }

    protected override void Read(NReader rdr)
    {
        ErrorId = rdr.ReadInt32();
        ErrorDescription = rdr.ReadUTF();
    }

    protected override void Write(NWriter wtr)
    {
        wtr.Write(ErrorId);
        wtr.WriteUTF(ErrorDescription);
    }
}