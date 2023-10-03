using System.IO.Compression;
using Shared.resources;
using Shared.terrain;
using Newtonsoft.Json;

namespace GameServer.realm.terrain;

public class JsonMapExporter {
    public string Export(XmlData data, TerrainTile[,] tiles) {
        var w = tiles.GetLength(0);
        var h = tiles.GetLength(1);
        var dat = new byte[w * h * 2];
        var i = 0;
        var idxs = new Dictionary<TerrainTile, short>(new TileComparer());
        var dict = new List<loc>();
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++) {
            var tile = tiles[x, y];
            if (!idxs.TryGetValue(tile, out var idx)) {
                idxs.Add(tile, idx = (short) dict.Count);
                dict.Add(new loc {
                    ground = data.TileTypeToId[tile.TileId],
                    objs = tile.TileObj == null
                        ? null
                        : new obj[] {
                            new() {
                                id = tile.TileObj,
                                name = tile.Name == null ? null : tile.Name
                            }
                        },
                    regions = null
                });
            }

            dat[i + 1] = (byte) (idx & 0xff);
            dat[i] = (byte) (idx >> 8);
            i += 2;
        }

        Span<byte> compressed = new();
        var length = 0;
        using (var compressor = new ZLibStream(new MemoryStream(dat), CompressionMode.Compress)) {
            length = compressor.Read(compressed);
        }

        var ret = new json_dat {
            data = compressed.ToArray(),
            width = w,
            height = h,
            dict = dict.ToArray()
        };
        return JsonConvert.SerializeObject(ret);
    }

    private struct TileComparer : IEqualityComparer<TerrainTile> {
        public bool Equals(TerrainTile x, TerrainTile y) {
            return x.TileId == y.TileId && x.TileObj == y.TileObj;
        }

        public int GetHashCode(TerrainTile obj) {
            return obj.TileId * 13 +
                   (obj.TileObj == null ? 0 : obj.TileObj.GetHashCode() * obj.Name.GetHashCode() * 29);
        }
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