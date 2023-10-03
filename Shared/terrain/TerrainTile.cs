using Shared.resources;

namespace Shared.terrain;

public struct TerrainTile : IEquatable<TerrainTile> {
    public int PolygonId;
    public byte Elevation;
    public float Moisture;
    public string Biome;
    public ushort TileId;
    public string Name;
    public string TileObj;
    public TerrainType Terrain;
    public TileRegion Region;

    public bool Equals(TerrainTile other) {
        return
            TileId == other.TileId &&
            TileObj == other.TileObj &&
            Name == other.Name &&
            Terrain == other.Terrain &&
            Region == other.Region;
    }
}