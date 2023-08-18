using common;
using wServer.realm.terrain;

namespace wServer.networking.packets.outgoing;

public class Update : OutgoingMessage
{
    public struct TileData
    {
        public short X;
        public short Y;
        public ushort Tile;
    }

    public TileData[] Tiles { get; set; }
    public int[] Drops { get; set; }
    public ObjectDef[] NewObjs { get; set; }
    
    public override S2CPacketId S2CId => S2CPacketId.Update;
    public override Packet CreateInstance() { return new Update(); }

    protected override void Read(NReader rdr)
    {
    }

    protected override void Write(NWriter wtr)
    {
        wtr.Write((ushort)Tiles.Length);
        foreach (var i in Tiles)
        {
            wtr.Write(i.X);
            wtr.Write(i.Y);
            wtr.Write(i.Tile);
        }
        wtr.Write((ushort)Drops.Length);
        foreach (var i in Drops)
        {
            wtr.Write(i);
        }
        wtr.Write((ushort)NewObjs.Length);
        foreach (var i in NewObjs)
        {
            i.Write(wtr);
        }
    }
}