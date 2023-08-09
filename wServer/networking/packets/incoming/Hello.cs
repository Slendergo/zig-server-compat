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

        // could just send -1 for a charid instead of sending a createcharacter 
        // would save u one byte lol
        //CharId = rdr.ReadInt16();
        //if (CharId != -1)
        //{
        //    CharacterType = rdr.ReadUInt16();
        //    SkinType = rdr.ReadUInt16();
        //}

        CharId = rdr.ReadInt16();
        CreateCharacter = rdr.ReadBoolean();
        if (CreateCharacter)
        {
            CharacterType = rdr.ReadUInt16();
            SkinType = rdr.ReadUInt16();
        } // seems like it will be good to go
    }

    protected override void Write(NWriter wtr) { }
}