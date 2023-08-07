using common;

namespace wServer.networking.packets.outgoing
{
    public class Text : OutgoingMessage
    {
        public string Name { get; set; }
        public int ObjectId { get; set; }
        public int NumStars { get; set; }
        public byte BubbleTime { get; set; }
        public string Recipient { get; set; }
        public string Txt { get; set; }

        public override PacketId ID => PacketId.TEXT;
        public override Packet CreateInstance() { return new Text(); }

        protected override void Read(NReader rdr)
        {
            Name = rdr.ReadUTF();
            ObjectId = rdr.ReadInt32();
            NumStars = rdr.ReadInt32();
            BubbleTime = rdr.ReadByte();
            Recipient = rdr.ReadUTF();
            Txt = rdr.ReadUTF();
        }
        protected override void Write(NWriter wtr)
        {
            wtr.WriteUTF(Name);
            wtr.Write(ObjectId);
            wtr.Write(NumStars);
            wtr.Write(BubbleTime);
            wtr.WriteUTF(Recipient);
            wtr.WriteUTF(Txt);
        }
    }
}
