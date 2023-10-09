using System.Net;
using Shared.resources;
using Ionic.Zlib;
using Newtonsoft.Json;
using NLog;
using Shared;

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

public sealed class MapParser
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    // do i have the overhead of a Concurrent Dictionary? idk, this is only really a safety precaution as we cache on the go
    private static readonly object AccessLock = new();
    private static readonly Dictionary<string, MapData> Cache = new();

    public static MapData GetOrLoad(string map)
    {
        lock (AccessLock)
        {
            if (!Cache.TryGetValue(map, out var data))
                data = TryCacheMap(map);

            // adding in this line for debug only to warn, no need for logger just a cw is fine for its purpose
            // the code i write will prevent any issues with a null map, it just simply wont work if map isnt found 
            // ~ Slendergo

            Log.Debug(data != null ? $"Cached: {map}" : $"Unable to locate: {map} in MapParser cache");
            return data;
        }
    }

    private static MapData TryCacheMap(string map)
    {
        Log.Debug($"Caching: {map}");
        var path = $"{Program.Resources.ResourcePath}/worlds/{map}";
        if (!File.Exists(path))
            return null;

        using var rdr =
            new BinaryReader(new ZlibStream(new MemoryStream(File.ReadAllBytes(path)), CompressionMode.Decompress));
        var isRealm = rdr.ReadBoolean();
        return new MapData(rdr, isRealm);
    }

    #region Legacy Converter Tools

    #region Jm

    [Serializable]
    private struct Obj
    {
        public readonly string Name;
        public readonly string Id;

        public Obj(string name, string id)
        {
            Name = name;
            Id = id;
        }
    }

    [Serializable]
    private struct Location
    {
        public readonly string Ground;
        public readonly Obj[] Objs;
        public readonly Obj[] Regions;

        public Location(string ground, Obj[] objs, Obj[] regions)
        {
            Ground = ground;
            Objs = objs;
            Regions = regions;
        }
    }

    [Serializable]
    private struct JsonData
    {
        public readonly byte[] Data;
        public readonly int Width;
        public readonly int Height;
        public readonly Location[] Dict;

        public JsonData(byte[] data, int width, int height, Location[] dict)
        {
            Data = data;
            Width = width;
            Height = height;
            Dict = dict;
        }
    }

    public struct TerrainTile : IEquatable<TerrainTile>
    {
        public int PolygonId;
        public byte Elevation;
        public float Moisture;
        public string Biome;
        public ushort TileId;
        public string Name;
        public string TileObj;
        public TerrainType Terrain;
        public TileRegion Region;

        public bool Equals(TerrainTile other)
        {
            return
                TileId == other.TileId &&
                TileObj == other.TileObj &&
                Name == other.Name &&
                Terrain == other.Terrain &&
                Region == other.Region;
        }
    }

    public static byte[] ExportWmap(TerrainTile[,] tiles)
    {
        var dict = new List<TerrainTile>();
        var w = tiles.GetLength(0);
        var h = tiles.GetLength(1);
        var dat = new byte[w * h * 3];
        var idx = 0;
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var tile = tiles[x, y];
                var i = (short)dict.IndexOf(tile);
                if (i == -1)
                {
                    i = (short)dict.Count;
                    dict.Add(tile);
                }

                dat[idx] = (byte)(i & 0xff);
                dat[idx + 1] = (byte)(i >> 8);
                dat[idx + 2] = tile.Elevation;
                idx += 3;
            }

        MemoryStream ms = new MemoryStream();
        using (BinaryWriter wtr = new BinaryWriter(ms))
        {
            wtr.Write((short)dict.Count);
            foreach (var i in dict)
            {
                wtr.Write(i.TileId);
                wtr.Write(i.TileObj ?? "");
                wtr.Write(i.Name ?? "");
                wtr.Write((byte)i.Terrain);
                wtr.Write((byte)i.Region);
            }

            wtr.Write(w);
            wtr.Write(h);
            wtr.Write(dat);
        }

        var buff = ZlibStream.CompressBuffer(ms.ToArray());
        var ret = new byte[buff.Length + 1];
        Buffer.BlockCopy(buff, 0, ret, 1, buff.Length);
        ret[0] = 2;
        return ret;
    }

    public static byte[] ConvertJmToWmap(string json, bool bigEndian = false)
    {
        var obj = JsonConvert.DeserializeObject<JsonData>(json);
        var dat = ZlibStream.UncompressBuffer(obj.Data);

        var tileDict = new Dictionary<short, TerrainTile>();
        for (var i = 0; i < obj.Dict.Length; i++)
        {
            var o = obj.Dict[i];
            tileDict[(short)i] = new TerrainTile()
            {
                TileId = o.Ground == null ? (ushort)0xff : Program.Resources.GameData.IdToTileType[o.Ground],
                TileObj = o.Objs?[0].Id,
                Name = o.Objs == null ? "" : o.Objs[0].Name ?? "",
                Terrain = TerrainType.None,
                Region = o.Regions == null ? TileRegion.None : (TileRegion)Enum.Parse(typeof(TileRegion), o.Regions[0].Id.Replace(' ', '_'))
            };
        }

        var tiles = new TerrainTile[obj.Width, obj.Height];
        using var rdr = new NReader(new MemoryStream(dat));

        for (var y = 0; y < obj.Height; y++)
            for (var x = 0; x < obj.Width; x++)
                tiles[x, y] = bigEndian ? tileDict[IPAddress.NetworkToHostOrder(rdr.ReadInt16())] : tileDict[rdr.ReadInt16()];
        return ExportWmap(tiles);
    }

    #endregion

    #region Wmap

    private struct ConvertData : IEquatable<ConvertData>
    {
        public ushort TileType;
        public ushort ObjectType;
        public byte Region;
        public byte Terrain;
        public byte Elevation;

        public bool Equals(ConvertData other) => TileType == other.TileType && ObjectType == other.ObjectType && Region == other.Region && Terrain == other.Terrain && Elevation == other.Elevation;
    }

    public static byte[] ConvertWmapRealmToMapData(byte[] data)
    {
        using var stream = new MemoryStream(data);

        var version = stream.ReadByte();
        using var rdr = new BinaryReader(new ZlibStream(stream, CompressionMode.Decompress));

        var dict = new List<ConvertData>();

        var c = rdr.ReadInt16();
        for (var i = 0; i < c; i++)
        {
            var tileId = rdr.ReadUInt16();
            var objTypeStr = rdr.ReadString();
            var objectType = objTypeStr == string.Empty ? ushort.MaxValue : Program.Resources.GameData.IdToObjectType[objTypeStr];
            _ = rdr.ReadString();
            var terrain = rdr.ReadByte();
            var region = rdr.ReadByte();
            var elevation = rdr.ReadByte();

            dict.Add(new ConvertData()
            {
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

        wtr.Write((ushort)0); // StartX
        wtr.Write((ushort)0); // StartY
        wtr.Write((ushort)width);
        wtr.Write((ushort)height);

        wtr.Write((ushort)dict.Count);
        foreach (var tile in dict)
        {
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
                wtr.Write((byte)index);
            else
                wtr.Write((ushort)index);

        return ZlibStream.CompressBuffer(ms.ToArray());
    }

    public static byte[] ConvertWmapToMapData(byte[] data)
    {
        using var stream = new MemoryStream(data);

        var version = stream.ReadByte();
        using var rdr = new BinaryReader(new ZlibStream(stream, CompressionMode.Decompress));

        var dict = new List<ConvertData>();

        var c = rdr.ReadInt16();
        for (var i = 0; i < c; i++)
        {
            var tileId = rdr.ReadUInt16();
            var objTypeStr = rdr.ReadString();
            var objectType = Program.Resources.GameData.IdToObjectType.GetValueOrDefault(objTypeStr, ushort.MaxValue);
            _ = rdr.ReadString();
            var terrain = rdr.ReadByte();
            var region = rdr.ReadByte();
            var elevation = (byte)0;

            dict.Add(new ConvertData
            {
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
            {
                indices.Add(rdr.ReadInt16());
                var e = rdr.ReadByte();
            }

        // todo parse to map format

        var ms = new MemoryStream();
        using var wtr = new BinaryWriter(ms);

        // is realm is true
        wtr.Write(false);

        wtr.Write((ushort)0); // StartX
        wtr.Write((ushort)0); // StartY
        wtr.Write((ushort)width);
        wtr.Write((ushort)height);

        wtr.Write((ushort)dict.Count);
        foreach (var tile in dict)
        {
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
                wtr.Write((byte)index);
            else
                wtr.Write((ushort)index);

        return ZlibStream.CompressBuffer(ms.ToArray());
    }

    #endregion

    public static void ConvertMaps()
    {
        var basePath = $"{Environment.CurrentDirectory}/converter";
        var inputDir = $"{basePath}/input";
        if (!Directory.Exists(inputDir))
        {
            _ = Directory.CreateDirectory(inputDir);
            Console.WriteLine($"No files found in {inputDir} skipping converting stage");
            return;
        }

        var outputDir = $"{basePath}/output";
        if (!Directory.Exists(outputDir))
            _ = Directory.CreateDirectory(outputDir);

        ConvertWmapsToMapData(inputDir, outputDir);
        ConvertJmsToMapData(inputDir, outputDir); 
    }

    // Converts all jm files to pmap files. input and output directories are located in the executable's directory.
    private static void ConvertJmsToMapData(string inputDir, string outputDir)
    {
        var files = Directory.GetFiles(inputDir, "*.jm");
        if (files.Length == 0)
        {
            Console.WriteLine("No jm files found in input directory.");
            return;
        }

        foreach (var file in files)
        {
            var name = Path.GetFileNameWithoutExtension(file);
            if (File.Exists(file))
            {
                Console.WriteLine($"{name}.jm has been converted already, skipping process");
                continue;
            }
            var wmapData = ConvertJmToWmap(File.ReadAllText(file), true);
            var data = ConvertWmapToMapData(wmapData);
            File.WriteAllBytes($"{outputDir}/{name}.pmap", data);
            Console.WriteLine($"Converted {name}.jm to {name}.pmap");
        }
    }

    // Converts all wmap files to pmap files. input and output directories are located in the executable's directory.
    private static void ConvertWmapsToMapData(string inputDir, string outputDir)
    {
        var files = Directory.GetFiles(inputDir, "*.wmap");
        if (files.Length == 0)
        {
            Console.WriteLine("No wmap files found in input directory.");
            return;
        }

        foreach (var file in files)
        {
            var name = Path.GetFileNameWithoutExtension(file);
            if (File.Exists(file))
            {
                Console.WriteLine($"{name}.wmap has been converted already, skipping process");
                continue;
            }

            var data = ConvertWmapToMapData(File.ReadAllBytes(file));

            File.WriteAllBytes($"{outputDir}/{name}.pmap", data);
            Console.WriteLine($"Converted {name}.wmap to {name}.pmap");
        }
    }

    #endregion
}