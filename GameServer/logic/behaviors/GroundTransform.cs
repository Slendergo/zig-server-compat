using System.Xml.Linq;
using Shared;
using GameServer.realm;

namespace GameServer.logic.behaviors;

internal class GroundTransform : Behavior {
    private readonly bool _persist;
    private readonly int _radius;
    private readonly int? _relativeX;
    private readonly int? _relativeY;

    private readonly string _tileId;

    public GroundTransform(XElement e) {
        _tileId = e.ParseString("@tileId");
        _radius = e.ParseInt("@radius");
        _persist = e.ParseBool("@persist");
        _relativeX = e.ParseNInt("@relativeX");
        _relativeY = e.ParseNInt("@relativeY");
    }

    public GroundTransform(
        string tileId,
        int radius = 0,
        int? relativeX = null,
        int? relativeY = null,
        bool persist = false) {
        _tileId = tileId;
        _radius = radius;
        _persist = persist;
        _relativeX = relativeX;
        _relativeY = relativeY;
    }

    protected override void OnStateEntry(Entity host, RealmTime time, ref object state) {
        var map = host.Owner.Map;
        var hx = (int) host.X;
        var hy = (int) host.Y;

        var tileType = host.Manager.Resources.GameData.IdToTileType[_tileId];

        var tiles = new List<TileState>();

        if (_relativeX != null && _relativeY != null) {
            var x = hx + (int) _relativeX;
            var y = hy + (int) _relativeY;

            if (!map.Contains(new IntPoint(x, y)))
                return;

            var tile = map[x, y];

            if (tileType == tile.TileId)
                return;

            tiles.Add(new TileState {
                TileType = tile.TileId,
                X = x,
                Y = y,
                Spawned = tile.Spawned
            });

            tile.Spawned = host.Spawned;
            tile.TileId = tileType;
            tile.UpdateCount++;
            return;
        }

        for (var i = hx - _radius; i <= hx + _radius; i++)
        for (var j = hy - _radius; j <= hy + _radius; j++) {
            if (!map.Contains(new IntPoint(i, j)))
                continue;

            var tile = map[i, j];

            if (tileType == tile.TileId)
                continue;

            tiles.Add(new TileState {
                TileType = tile.TileId,
                X = i,
                Y = j,
                Spawned = tile.Spawned
            });

            tile.Spawned = host.Spawned;
            tile.TileId = tileType;
            tile.UpdateCount++;
        }

        state = tiles;
    }

    protected override void OnStateExit(Entity host, RealmTime time, ref object state) {
        if (state is not List<TileState> tiles || _persist)
            return;

        foreach (var tile in tiles) {
            var x = tile.X;
            var y = tile.Y;
            var tileType = tile.TileType;
            var spawned = tile.Spawned;
            var map = host.Owner.Map;

            var curTile = map[x, y];
            curTile.Spawned = spawned;
            curTile.TileId = tileType;
            curTile.UpdateCount++;
        }
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state) { }

    // state object: TileState
    private class TileState {
        public bool Spawned;
        public ushort TileType;
        public int X;
        public int Y;
    }
}