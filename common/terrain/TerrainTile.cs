using common.resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace terrain
{
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
                this.TileId == other.TileId &&
                this.TileObj == other.TileObj &&
                this.Name == other.Name &&
                this.Terrain == other.Terrain &&
                this.Region == other.Region;
        }
    }
}
