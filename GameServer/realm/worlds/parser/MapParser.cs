using Shared.resources;
using Ionic.Zlib;
using NLog;

namespace GameServer.realm.worlds.parser;

public sealed class TileData {
    public readonly byte Elevation;
    public readonly ObjectDesc ObjectDesc;
    public readonly TileRegion Region;
    public readonly TerrainType Terrain;
    public readonly TileDesc TileDesc;

    public TileData(BinaryReader rdr, bool isRealm) {
        TileDesc = Program.Resources.GameData.Tiles.GetValueOrDefault(rdr.ReadUInt16(),
            Program.Resources.GameData.Tiles[0xFF]);
        ObjectDesc = Program.Resources.GameData.ObjectDescs.GetValueOrDefault(rdr.ReadUInt16());
        Region = (TileRegion) rdr.ReadByte();
        if (isRealm) {
            Terrain = (TerrainType) rdr.ReadByte();
            Elevation = rdr.ReadByte();
        }
    }
}

public sealed class MapData {
    public readonly int Height;
    public readonly TileData[,] Tiles;
    public readonly int Width;

    public MapData(BinaryReader rdr, bool isRealm) {
        /*StartX*/
        _ = rdr.ReadUInt16();
        /*StartY*/
        _ = rdr.ReadUInt16();
        Width = rdr.ReadUInt16();
        Height = rdr.ReadUInt16();
        Tiles = new TileData[Width, Height];

        var tileDatas = new TileData[rdr.ReadUInt16()];
        for (var i = 0; i < tileDatas.Length; i++)
            tileDatas[i] = new TileData(rdr, isRealm);

        var byteRead = tileDatas.Length <= 256;
        for (var y = 0; y < Height; y++)
        for (var x = 0; x < Width; x++) {
            var index = byteRead ? rdr.ReadByte() : rdr.ReadUInt16();
            Tiles[x, y] = tileDatas[index];
        }
    }
}

// this should be miles better for setpieces and map parsing

public sealed class MapParser {
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    
    // do i have the overhead of a Concurrent Dictionary? idk, this is only really a safety precaution as we cache on the go
    private static readonly object AccessLock = new();
    private static readonly Dictionary<string, MapData> Cache = new();

    public static MapData GetOrLoad(string map) {
        lock (AccessLock) {
            if (!Cache.TryGetValue(map, out var data))
                data = TryCacheMap(map);

            // adding in this line for debug only to warn, no need for logger just a cw is fine for its purpose
            // the code i write will prevent any issues with a null map, it just simply wont work if map isnt found 
            // ~ Slendergo

            Log.Debug(data != null ? $"Cached: {map}" : $"Unable to locate: {map} in MapParser cache");
            return data;
        }
    }

    private static MapData TryCacheMap(string map) {
        Log.Debug($"Caching: {map}");
        var path = $"{Program.Resources.ResourcePath}/worlds/{map}";
        if (!File.Exists(path))
            return null;

        using var rdr =
            new BinaryReader(new ZlibStream(new MemoryStream(File.ReadAllBytes(path)), CompressionMode.Decompress));
        var isRealm = rdr.ReadBoolean();
        return new MapData(rdr, isRealm);
    }

    public static byte[] ConvertWmapRealmToMapData(byte[] data) {
        using var stream = new MemoryStream(data);

        var version = stream.ReadByte();
        using var rdr = new BinaryReader(new ZlibStream(stream, CompressionMode.Decompress));

        var dict = new List<ConvertData>();

        var c = rdr.ReadInt16();
        for (var i = 0; i < c; i++) {
            var tileId = rdr.ReadUInt16();
            var objTypeStr = rdr.ReadString();
            var objectType = objTypeStr == string.Empty
                ? ushort.MaxValue
                : Program.Resources.GameData.IdToObjectType[objTypeStr];
            _ = rdr.ReadString();
            var terrain = rdr.ReadByte();
            var region = rdr.ReadByte();
            var elevation = rdr.ReadByte();

            dict.Add(new ConvertData {
                TileType = tileId,
                ObjectType = objectType,
                Region = region,
                Terrain = terrain,
                Elevation = elevation
            });
        }

        var width = rdr.ReadInt32();
        var height = rdr.ReadInt32();

        var indices = new List<short>();
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
            indices.Add(rdr.ReadInt16());

        // todo parse to map format

        var ms = new MemoryStream();
        using var wtr = new BinaryWriter(ms);

        // is realm is true
        wtr.Write(true);

        wtr.Write((ushort) 0); // StartX
        wtr.Write((ushort) 0); // StartY
        wtr.Write((ushort) width);
        wtr.Write((ushort) height);

        wtr.Write((ushort) dict.Count);
        foreach (var tile in dict) {
            // im leaving the casting here so i can see the types visually
            wtr.Write(tile.TileType);
            wtr.Write(tile.ObjectType);
            wtr.Write(tile.Region);

            // realm will include so we add this
            wtr.Write(tile.Terrain);
            wtr.Write(tile.Elevation);
        }

        var writeByte = dict.Count <= 256;
        foreach (var index in indices)
            if (writeByte)
                wtr.Write((byte) index);
            else
                wtr.Write((ushort) index);
        return ZlibStream.CompressBuffer(ms.ToArray());
    }


    public static byte[] ConvertWmapToMapData(byte[] data) {
        using var stream = new MemoryStream(data);

        var version = stream.ReadByte();
        using var rdr = new BinaryReader(new ZlibStream(stream, CompressionMode.Decompress));

        var dict = new List<ConvertData>();

        var c = rdr.ReadInt16();
        for (var i = 0; i < c; i++) {
            var tileId = rdr.ReadUInt16();
            var objTypeStr = rdr.ReadString();
            var objectType = Program.Resources.GameData.IdToObjectType.GetValueOrDefault(objTypeStr, ushort.MaxValue);
            _ = rdr.ReadString();
            var terrain = rdr.ReadByte();
            var region = rdr.ReadByte();
            var elevation = (byte) 0;

            dict.Add(new ConvertData {
                TileType = tileId,
                ObjectType = objectType,
                Region = region,
                Terrain = terrain,
                Elevation = elevation
            });
        }

        var width = rdr.ReadInt32();
        var height = rdr.ReadInt32();

        var indices = new List<short>();
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++) {
            indices.Add(rdr.ReadInt16());
            var e = rdr.ReadByte();
        }

        // todo parse to map format

        var ms = new MemoryStream();
        using var wtr = new BinaryWriter(ms);

        // is realm is true
        wtr.Write(false);

        wtr.Write((ushort) 0); // StartX
        wtr.Write((ushort) 0); // StartY
        wtr.Write((ushort) width);
        wtr.Write((ushort) height);

        wtr.Write((ushort) dict.Count);
        foreach (var tile in dict) {
            // im leaving the casting here so i can see the types visually
            wtr.Write(tile.TileType);
            wtr.Write(tile.ObjectType);
            wtr.Write(tile.Region);

            // realm will include so we add this
            //wtr.Write((byte)tile.Terrain);
            //wtr.Write((byte)tile.Elevation);
        }

        var writeByte = dict.Count <= 256;
        foreach (var index in indices)
            if (writeByte)
                wtr.Write((byte) index);
            else
                wtr.Write((ushort) index);

        return ZlibStream.CompressBuffer(ms.ToArray());
    }

    private struct ConvertData : IEquatable<ConvertData> {
        public ushort TileType;
        public ushort ObjectType;
        public byte Region;
        public byte Terrain;
        public byte Elevation;

        public bool Equals(ConvertData other) {
            return TileType == other.TileType &&
                   ObjectType == other.ObjectType &&
                   Region == other.Region &&
                   Terrain == other.Terrain &&
                   Elevation == other.Elevation;
        }
    }
}