using System.Xml.Linq;
using common;
using common.resources;
using wServer.realm;
using wServer.realm.entities;

namespace wServer.logic.behaviors;

internal class Reproduce : Behavior {
    private readonly ushort? _children;

    private readonly int _densityMax;
    //State storage: cooldown timer

    private readonly double _densityRadius;
    private readonly TileRegion _region;
    private readonly double _regionRange;
    private Cooldown _coolDown;
    private List<IntPoint> _reproduceRegions;

    public Reproduce(XElement e) {
        var childrenString = e.ParseString("@children");
        _children = childrenString == null ? null : GetObjType(childrenString);
        _densityRadius = e.ParseFloat("@densityRadius", 10);
        _densityMax = e.ParseInt("@densityMax", 5);
        _coolDown = new Cooldown().Normalize(e.ParseInt("@cooldown", 60000));
        _region = (TileRegion) Enum.Parse(typeof(TileRegion), e.ParseString("@region", "None"));
        _regionRange = e.ParseInt("@regionRange", 10);
    }

    public Reproduce(string children = null,
        double densityRadius = 10,
        int densityMax = 5,
        Cooldown coolDown = new(),
        TileRegion region = TileRegion.None,
        double regionRange = 10) {
        _children = children == null ? null : GetObjType(children);
        _densityRadius = densityRadius;
        _densityMax = densityMax;
        _coolDown = coolDown.Normalize(60000);
        _region = region;
        _regionRange = regionRange;
    }

    protected override void OnStateEntry(Entity host, RealmTime time, ref object state) {
        base.OnStateEntry(host, time, ref state);

        if (_region == TileRegion.None)
            return;

        var map = host.Owner.Map;

        var w = map.Width;
        var h = map.Height;

        _reproduceRegions = new List<IntPoint>();

        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++) {
            if (map[x, y].Region != _region)
                continue;

            _reproduceRegions.Add(new IntPoint(x, y));
        }
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state) {
        var cool = state == null ? _coolDown.Next(Random) : (int) state;

        if (cool <= 0) {
            var count = host.CountEntity(_densityRadius, _children ?? host.ObjectType);

            if (count < _densityMax) {
                double targetX = host.X;
                double targetY = host.Y;

                if (_reproduceRegions != null && _reproduceRegions.Count > 0) {
                    var sx = (int) host.X;
                    var sy = (int) host.Y;
                    var regions = _reproduceRegions
                        .Where(p => Math.Abs(sx - p.X) <= _regionRange &&
                                    Math.Abs(sy - p.Y) <= _regionRange).ToList();
                    var tile = regions[Random.Next(regions.Count)];
                    targetX = tile.X;
                    targetY = tile.Y;
                }

                /*int i = 0;
                do
                {
                    var angle = Random.NextDouble() * 2 * Math.PI;
                    targetX = host.X + densityRadius * 0.5 * Math.Cos(angle);
                    targetY = host.Y + densityRadius * 0.5 * Math.Sin(angle);
                    i++;
                } while (targetX < host.Owner.Map.Width &&
                         targetY < host.Owner.Map.Height &&
                         targetX > 0 && targetY > 0 &&
                         host.Owner.Map[(int)targetX, (int)targetY].Terrain !=
                         host.Owner.Map[(int)host.X, (int)host.Y].Terrain &&
                    i < 10);*/

                if (!host.Owner.IsPassable(targetX, targetY, true)) {
                    state = _coolDown.Next(Random);
                    return;
                }

                var entity = Entity.Resolve(host.Manager, _children ?? host.ObjectType);
                entity.GivesNoXp = true;
                entity.Move((float) targetX, (float) targetY);

                if (host is Enemy enemyHost && entity is Enemy enemyEntity) {
                    enemyEntity.Terrain = enemyHost.Terrain;
                    if (enemyHost.Spawned) enemyEntity.Spawned = true;
                }

                host.Owner.EnterWorld(entity);
            }

            cool = _coolDown.Next(Random);
        }
        else {
            cool -= time.ElapsedMsDelta;
        }

        state = cool;
    }
}