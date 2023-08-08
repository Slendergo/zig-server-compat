﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.IO;
using System.IO.Compression;
using common;
using common.resources;
using NLog.Fluent;

namespace terrain
{
    class Json2Wmap
    {
        private struct obj
        {
            public string name;
            public string id;
        }
        private struct loc
        {
            public string ground;
            public obj[] objs;
            public obj[] regions;
        }
        private struct json_dat
        {
            public byte[] data;
            public int width;
            public int height;
            public loc[] dict;
        }

        public static void Convert(XmlData data, string from, string to)
        {
            var x = Convert(data, File.ReadAllText(from));
            File.WriteAllBytes(to, x);
        }
        public static byte[] Convert(XmlData data, string json)
        {
            var obj = JsonConvert.DeserializeObject<json_dat>(json);
            int length = 0;
            var ms = new MemoryStream(obj.data);
            Span<byte> dat = new();
            using (var decompressor = new ZLibStream(ms, CompressionMode.Decompress))
                length = decompressor.Read(dat);

            Dictionary<short, TerrainTile> tileDict = new Dictionary<short, TerrainTile>();
            for (int i = 0; i < obj.dict.Length; i++)
            {
                var o = obj.dict[i];
                tileDict[(short)i] = new TerrainTile()
                {
                    TileId = o.ground == null ? (ushort)0xff : data.IdToTileType[o.ground],
                    TileObj = o.objs == null ? null : o.objs[0].id,
                    Name = o.objs == null ? "" : o.objs[0].name ?? "",
                    Terrain = TerrainType.None,
                    Region = o.regions == null ? TileRegion.None : (TileRegion)Enum.Parse(typeof(TileRegion), o.regions[0].id.Replace(' ', '_'))
                };
            }
            
            var tiles = new TerrainTile[obj.width, obj.height];
            try
            {
                using (NReader rdr = new NReader(new MemoryStream(dat.ToArray())))
                    for (int y = 0; y < obj.height; y++)
                    for (int x = 0; x < obj.width; x++)
                    {
                        tiles[x, y] = tileDict[rdr.ReadInt16()];
                    }
            }
            catch (Exception)
            {
                return null;
            }

            return WorldMapExporter.Export(tiles);
        }
    }
}
