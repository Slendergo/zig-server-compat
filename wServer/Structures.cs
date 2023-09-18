using wServer.realm;

namespace wServer;

internal struct StatValue {
    public object Value;
    public int UCount;
}

public struct IntPoint : IEquatable<IntPoint> {
    public int X;
    public int Y;
    public int Type;
    public int Generation;
    public bool Blocking;

    public IntPoint(int x, int y, int type = 8) {
        X = x;
        Y = y;
        Type = 8;
        Generation = 0;
        Blocking = false;
    }

    public bool Equals(IntPoint other) {
        return X == other.X && Y == other.Y;
    }

    public override bool Equals(object obj) {
        if (obj == null)
            return false;

        if (obj is IntPoint) {
            var p = (IntPoint) obj;
            return Equals(p);
        }

        return false;
    }

    public override int GetHashCode() {
        /*unchecked
        {
            int hash = 17;
            hash = hash * 23 + X.GetHashCode();
            hash = hash * 23 + Y.GetHashCode();
            return hash;
        }*/
        return 31 * X + 17 * Y; // could be problem if value is changed...
    }
}

public struct TradeItem {
    public int Item;
    public int SlotType;
    public bool Tradeable;
    public bool Included;
}

public enum EffectType {
    Potion = 1,
    Teleport = 2,
    Stream = 3,
    Throw = 4,
    AreaBlast = 5, //radius=pos1.x
    Dead = 6,
    Trail = 7,
    Diffuse = 8, //radius=dist(pos1,pos2)
    Flow = 9,
    Trap = 10, //radius=pos1.x
    Lightning = 11, //particleSize=pos2.x
    Concentrate = 12, //radius=dist(pos1,pos2)
    BlastWave = 13, //origin=pos1, radius = pos2.x
    Earthquake = 14,
    Flashing = 15, //period=pos1.x, numCycles=pos1.y
    BeachBall = 16
}

public struct ARGB {
    public ARGB(uint argb) {
        A = (byte) ((argb & 0xff000000) >> 24);
        R = (byte) ((argb & 0x00ff0000) >> 16);
        G = (byte) ((argb & 0x0000ff00) >> 8);
        B = (byte) ((argb & 0x000000ff) >> 0);
    }

    public byte A;
    public byte R;
    public byte G;
    public byte B;
}

public struct ObjectSlot {
    public int ObjectId;
    public byte SlotId;
    public int ObjectType;
}

public struct MoveRecord {
    public long Time;
    public float X;
    public float Y;
}

public struct Position {
    public float X;
    public float Y;
}

public struct TileData {
    public short X;
    public short Y;
    public ushort Tile;
}

public struct ObjectDef {
    public ushort ObjectType;
    public ObjectStats Stats;
}

public struct ObjectStats {
    public int Id;
    public Position Position;
    public KeyValuePair<StatsType, object>[] Stats;
}