using System.IO.Compression;
using common;
using common.resources;
using Newtonsoft.Json;

namespace terrain;

internal class Json2Wmap {
    public static void Convert(XmlData data, string from, string to) {
        var x = Convert(data, File.ReadAllText(from));
        File.WriteAllBytes(to, x);
    }

    public static byte[] Convert(XmlData data, string json) {
        var obj = JsonConvert.DeserializeObject<json_dat>(json);
        Span<byte> dat = new();
        var length = 0;
        using (var decompressor = new ZLibStream(new MemoryStream(obj.data), CompressionMode.Decompress)) {
            length = decompressor.Read(dat);
        }

        var tileDict = new Dictionary<short, TerrainTile>();
        for (var i = 0; i < obj.dict.Length; i++) {
            var o = obj.dict[i];
            tileDict[(short) i] = new TerrainTile {
                TileId = o.ground == null ? (ushort) 0xff : data.IdToTileType[o.ground],
                TileObj = o.objs == null ? null : o.objs[0].id,
                Name = o.objs == null ? "" : o.objs[0].name ?? "",
                Terrain = TerrainType.None,
                Region = o.regions == null
                    ? TileRegion.None
                    : (TileRegion) Enum.Parse(typeof(TileRegion), o.regions[0].id.Replace(' ', '_'))
            };
        }

        var tiles = new TerrainTile[obj.width, obj.height];
        using (var rdr = new NReader(new MemoryStream(dat.ToArray()))) {
            for (var y = 0; y < obj.height; y++)
            for (var x = 0; x < obj.width; x++)
                tiles[x, y] = tileDict[rdr.ReadInt16()];
        }

        return WorldMapExporter.Export(tiles);
    }

    private struct obj {
        public string name;
        public string id;
    }

    private struct loc {
        public string ground;
        public obj[] objs;
        public obj[] regions;
    }

    private struct json_dat {
        public byte[] data;
        public int width;
        public int height;
        public loc[] dict;
    }
}