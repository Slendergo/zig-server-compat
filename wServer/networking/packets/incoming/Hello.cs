using common;

namespace wServer.networking.packets.incoming;

public class Hello : IncomingMessage
{
    public string BuildVersion { get; set; }
    public int GameId { get; set; }
    public string GUID { get; set; }
    public string Password { get; set; }
    public short CharId { get; set; }
    public bool CreateCharacter { get; set; }
    public ushort CharacterType { get; set; }
    public ushort SkinType { get; set; }

    public override C2SPacketId C2SId => C2SPacketId.Hello;
    public override Packet CreateInstance() { return new Hello(); }

    protected override void Read(NReader rdr)
    {
        BuildVersion = rdr.ReadUTF();
        GameId = rdr.ReadInt32();
        GUID = rdr.ReadUTF();
        Password = rdr.ReadUTF();
        CharId = rdr.ReadInt16();
        CreateCharacter = rdr.ReadBoolean();
        if (CreateCharacter)
        {
            CharacterType = rdr.ReadUInt16();
            SkinType = rdr.ReadUInt16();
        }
    }

    protected override void Write(NWriter wtr)
    {
        wtr.WriteUTF(BuildVersion);
        wtr.Write(GameId);
        wtr.WriteUTF(GUID);
        wtr.WriteUTF(Password);
        wtr.Write(CharId);
        wtr.Write(CreateCharacter);
        if (CreateCharacter)
        {
            wtr.Write(CharacterType);
            wtr.Write(SkinType);
        }
    }
}