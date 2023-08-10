using common;
using wServer.realm.terrain;

namespace wServer.networking.packets.outgoing;

public class Update : OutgoingMessage
{
    public struct TileData
    {
        public short X;
        public short Y;
        public Tile Tile;
    }

    public TileData[] Tiles { get; set; }
    public int[] Drops { get; set; }
    public ObjectDef[] NewObjs { get; set; }
    
    public override S2CPacketId S2CId => S2CPacketId.Update;
    public override Packet CreateInstance() { return new Update(); }

    protected override void Read(NReader rdr)
    {
        Tiles = new TileData[rdr.ReadInt16()];
        for (var i = 0; i < Tiles.Length; i++)
        {
            Tiles[i] = new TileData()
            {
                X = rdr.ReadInt16(),
                Y = rdr.ReadInt16(),
                Tile = (Tile)rdr.ReadUInt16(),
            };
        }

        Drops = new int[rdr.ReadInt16()];
        for (var i = 0; i < Drops.Length; i++)
            Drops[i] = rdr.ReadInt32();
        
        NewObjs = new ObjectDef[rdr.ReadInt16()];
        for (var i = 0; i < NewObjs.Length; i++)
            NewObjs[i] = ObjectDef.Read(rdr);
    }

    protected override void Write(NWriter wtr)
    {
        wtr.Write((short)Tiles.Length);
        foreach (var i in Tiles)
        {
            wtr.Write(i.X);
            wtr.Write(i.Y);
            wtr.Write((ushort)i.Tile);
        }
        wtr.Write((short)Drops.Length);
        foreach (var i in Drops)
        {
            wtr.Write(i);
        }
        wtr.Write((short)NewObjs.Length);
        foreach (var i in NewObjs)
        {
            i.Write(wtr);
        }
    }
}