using System.Xml.Linq;
using common;
using common.resources;
using wServer.realm;
using wServer.realm.entities;

namespace wServer.logic.behaviors;

internal class TossObject : Behavior {
    private readonly double? _angle;
    private readonly ushort[] _children;
    private readonly int _coolDownOffset;
    private readonly double? _densityRange;
    private readonly string _group;
    private readonly double? _maxAngle;
    private readonly int? _maxDensity;
    private readonly double? _maxRange;
    private readonly double? _minAngle;
    private readonly double? _minRange;

    private readonly double _probability;
    //State storage: cooldown timer

    private readonly double _range;
    private readonly TileRegion _region;
    private readonly double _regionRange;
    private readonly bool _tossInvis;
    private Cooldown _coolDown;
    private List<IntPoint> _reproduceRegions;

    public TossObject(XElement e) {
        _group = e.ParseString("@group");
        if (_group == null)
            _children = new[] {GetObjType(e.ParseString("@child"))};
        else
            _children = BehaviorDb.InitGameData.ObjectDescs.Values
                .Where(x => x.Group == _group)
                .Select(x => x.ObjectType).ToArray();

        _range = e.ParseFloat("@range", 5);
        _angle = e.ParseNFloat("@angle") * Math.PI / 180;
        _coolDown = new Cooldown().Normalize(e.ParseInt("@cooldown", 1000));
        _coolDownOffset = e.ParseInt("@coolDownOffset");
        _tossInvis = e.ParseBool("@tossInvis");
        _probability = e.ParseFloat("@probability", 1);
        _minRange = e.ParseNFloat("@minRange");
        _maxRange = e.ParseNFloat("@maxRange");
        _minAngle = e.ParseNFloat("@minAngle") * Math.PI / 180;
        _maxAngle = e.ParseNFloat("@maxAngle") * Math.PI / 180;
        _densityRange = e.ParseNFloat("@densityRange");
        _maxDensity = e.ParseNInt("@maxDensity");
        _region = (TileRegion) Enum.Parse(typeof(TileRegion), e.ParseString("@region", "None"));
        _regionRange = e.ParseFloat("@regionRange", 10);
    }

    public TossObject(string child, double range = 5, double? angle = null,
        Cooldown coolDown = new(), int coolDownOffset = 0,
        bool tossInvis = false, double probability = 1, string group = null,
        double? minAngle = null, double? maxAngle = null,
        double? minRange = null, double? maxRange = null,
        double? densityRange = null, int? maxDensity = null,
        TileRegion region = TileRegion.None, double regionRange = 10
    ) {
        if (group == null)
            _children = new[] {GetObjType(child)};
        else
            _children = BehaviorDb.InitGameData.ObjectDescs.Values
                .Where(x => x.Group == group)
                .Select(x => x.ObjectType).ToArray();

        _range = range;
        _angle = angle * Math.PI / 180;
        _coolDown = coolDown.Normalize();
        _coolDownOffset = coolDownOffset;
        _tossInvis = tossInvis;
        _probability = probability;
        _minRange = minRange;
        _maxRange = maxRange;
        _minAngle = minAngle * Math.PI / 180;
        _maxAngle = maxAngle * Math.PI / 180;
        _densityRange = densityRange;
        _maxDensity = maxDensity;
        _group = group;
        _region = region;
        _regionRange = regionRange;
    }

    protected override void OnStateEntry(Entity host, RealmTime time, ref object state) {
        state = _coolDownOffset;

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
        var cool = (int) state;

        if (cool <= 0) {
            if (host.HasConditionEffect(ConditionEffects.Stunned))
                return;

            if (Random.NextDouble() > _probability) {
                state = _coolDown.Next(Random);
                return;
            }

            var player = host.GetNearestEntity(_range, null);
            if (player != null || _angle != null) {
                if (_densityRange != null && _maxDensity != null) {
                    var cnt = 0;
                    if (_children.Length > 1)
                        cnt = host.CountEntity((double) _densityRange, _group);
                    else
                        cnt = host.CountEntity((double) _densityRange, _children[0]);

                    if (cnt >= _maxDensity) {
                        state = _coolDown.Next(Random);
                        return;
                    }
                }

                var r = _range;
                if (_minRange != null && _maxRange != null)
                    r = (double) _minRange + Random.NextDouble() * ((double) _maxRange - (double) _minRange);

                var a = _angle;
                if (_angle == null && _minAngle != null && _maxAngle != null)
                    a = (double) _minAngle + Random.NextDouble() * ((double) _maxAngle - (double) _minAngle);

                Position target;
                if (a != null)
                    target = new Position {
                        X = host.X + (float) (r * Math.Cos(a.Value)),
                        Y = host.Y + (float) (r * Math.Sin(a.Value))
                    };
                else
                    target = new Position {
                        X = player.X,
                        Y = player.Y
                    };

                if (_reproduceRegions != null && _reproduceRegions.Count > 0) {
                    var sx = (int) host.X;
                    var sy = (int) host.Y;
                    var regions = _reproduceRegions
                        .Where(p => Math.Abs(sx - p.X) <= _regionRange &&
                                    Math.Abs(sy - p.Y) <= _regionRange).ToList();
                    var tile = regions[Random.Next(regions.Count)];
                    target = new Position {
                        X = tile.X,
                        Y = tile.Y
                    };
                }

                if (!_tossInvis)
                    foreach (var otherPlayer in host.Owner.Players.Values)
                        if (otherPlayer.DistSqr(host) < Player.RadiusSqr)
                            otherPlayer.Client.SendShowEffect(EffectType.Throw, host.Id, target, new Position(),
                                new ARGB(0xffffbf00));
                host.Owner.Timers.Add(new WorldTimer(1500, (world, t) => {
                    if (!world.IsPassable(target.X, target.Y, true))
                        return;

                    var entity = Entity.Resolve(host.Manager, _children[Random.Next(_children.Length)]);
                    entity.Move(target.X, target.Y);

                    if (host.Spawned) entity.Spawned = true;

                    if (host is Enemy enemyHost && entity is Enemy enemyEntity) enemyEntity.Terrain = enemyHost.Terrain;

                    world.EnterWorld(entity);
                }));
                cool = _coolDown.Next(Random);
            }
        }
        else {
            cool -= time.ElapsedMsDelta;
        }

        state = cool;
    }
}