using Shared.resources;
using GameServer.realm.entities;
using GameServer.realm.entities.player;
using GameServer.realm.terrain;
using NLog;

namespace GameServer.realm;

public class Sight {
    private const int Radius = Player.Radius;
    private const int RadiusSqr = Player.RadiusSqr;
    private const int AppoxAreaOfSight = (int) (Math.PI * Radius * Radius + 1);
    private const int MaxNumRegions = 2048;

    // blocked line of sight vars
    private const float StartAngle = 0;
    private const float EndAngle = (float) (2 * Math.PI);
    private const float RayStepSize = .1f;
    private const float AngleStepSize = 2.30f / Radius;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private static readonly IntPoint[] SurroundingPoints = {
        new(1, 0),
        new(1, 1),
        new(0, 1),
        new(-1, 1),
        new(-1, 0),
        new(-1, -1),
        new(0, -1),
        new(1, -1)
    };

    private static List<IntPoint> _unblockedView;
    private static Dictionary<float, IntPoint[]> _sightRays;
    private static readonly List<int> Primes;
    private static readonly List<IntPoint> VisibleTilesList = new(1024 * 1024);
    private static readonly HashSet<IntPoint> VisibleTilesSet = new();

    private readonly Player _player;
    private readonly HashSet<IntPoint> _sCircle;
    private readonly List<IntPoint> _visibleTilesList;

    private IntPoint _ip;

    private IntPoint _sTile;

    static Sight() {
        InitUnblockedView();
        InitSightRays();

        Primes = MathsUtils.GeneratePrimes(MaxNumRegions);
    }

    public Sight(Player player) {
        _player = player;
        _ip = new IntPoint(0, 0);
        _sCircle = new HashSet<IntPoint>();
        _visibleTilesList = new List<IntPoint>(AppoxAreaOfSight);
        UpdateCount++;
    }

    public int LastX { get; private set; }
    public int LastY { get; private set; }
    public int UpdateCount { get; set; }

    private static void InitUnblockedView() {
        var x = 0;
        var y = 0;
        var i = 1;
        var j = 2;

        _unblockedView = new List<IntPoint> {new(0, 0)};
        do {
            UnblockedViewTryAdd(x = x + 1, y);
            for (var k = 0; k < i; k++)
                UnblockedViewTryAdd(x, y = y - 1);
            for (var k = 0; k < j; k++)
                UnblockedViewTryAdd(x = x - 1, y);
            for (var k = 0; k < j; k++)
                UnblockedViewTryAdd(x, y = y + 1);
            for (var k = 0; k < j; k++)
                UnblockedViewTryAdd(x = x + 1, y);

            i += 2;
            j += 2;
        } while (j <= 2 * Radius);
    }

    private static void UnblockedViewTryAdd(int x, int y) {
        if (x * x + y * y <= RadiusSqr)
            _unblockedView.Add(new IntPoint(x, y));
    }

    private static void InitSightRays() {
        _sightRays = new Dictionary<float, IntPoint[]>();

        var currentAngle = StartAngle;
        while (currentAngle < EndAngle) {
            var ray = new List<IntPoint>(Radius);
            var dist = RayStepSize;
            while (dist < Radius) {
                var point = new IntPoint(
                    (int) (dist * Math.Cos(currentAngle)),
                    (int) (dist * Math.Sin(currentAngle)));

                if (!ray.Contains(point))
                    ray.Add(point);

                dist += RayStepSize;
            }

            _sightRays[currentAngle] = ray.ToArray();

            currentAngle += AngleStepSize;
        }
    }

    public HashSet<IntPoint> GetSightCircle(VisibilityType blocked) {
        if (UpdateCount <= 0)
            return _sCircle;

        UpdateCount = 0;
        LastX = (int) _player.X;
        LastY = (int) _player.Y;

        if (_player.Owner == null) {
            _sCircle.Clear();
            return _sCircle;
        }

        var map = _player.Owner.Map;
        switch (blocked) {
            case VisibilityType.Full:
                CalcUnblockedSight(map);
                break;
            case VisibilityType.Path:
                CalcBlockedRoomSight(map);
                break;
            case VisibilityType.LineOfSight:
                CalcBlockedLineOfSight(map);
                break;
        }

        return _sCircle;
    }

    private void CalcUnblockedSight(Wmap map) {
        _sCircle.Clear();
        foreach (var p in _unblockedView) {
            _ip.X = LastX + p.X;
            _ip.Y = LastY + p.Y;

            if (!map.Contains(_ip))
                continue;

            _sCircle.Add(_ip);
        }
    }

    private void CalcBlockedRoomSight(Wmap map) {
        var height = map.Height;
        var width = map.Width;

        _sCircle.Clear();
        _visibleTilesList.Clear();

        _sTile = new IntPoint(LastX, LastY);
        _visibleTilesList.Add(_sTile);

        for (var i = 0; i < _visibleTilesList.Count; i++) {
            var tile = _visibleTilesList[i];

            if (tile.Generation > Radius)
                continue;

            foreach (var sPoint in SurroundingPoints) {
                var x = _sTile.X = tile.X + sPoint.X;
                var y = _sTile.Y = tile.Y + sPoint.Y;

                var dx = LastX - x;
                var dy = LastY - y;

                if (_sCircle.Contains(_sTile) ||
                    x < 0 || x >= width ||
                    y < 0 || y >= height ||
                    dx * dx + dy * dy > RadiusSqr)
                    continue;

                _sTile.Generation = tile.Generation + 1;
                _sCircle.Add(_sTile);

                var t = map[x, y];
                if (IsBlocking(t))
                    continue;

                _visibleTilesList.Add(_sTile);
            }
        }
    }

    private void CalcBlockedLineOfSight(Wmap map) {
        _sCircle.Clear();
        foreach (var ray in _sightRays)
            for (var i = 0; i < ray.Value.Length; i++) {
                _ip.X = LastX + ray.Value[i].X;
                _ip.Y = LastY + ray.Value[i].Y;

                if (!map.Contains(_ip))
                    continue;

                _sCircle.Add(_ip);

                var t = map[_ip.X, _ip.Y];
                if (t.ObjectType != 0 && t.ObjectDesc != null && t.ObjectDesc.BlocksSight)
                    break;
                foreach (var sPoint in SurroundingPoints) {
                    var _intPoint = new IntPoint(sPoint.X + _ip.X, sPoint.Y + _ip.Y);
                    if (map.Contains(_intPoint))
                        _sCircle.Add(_intPoint);
                }
            }
    }

    private static bool IsBlocking(WmapTile tile) {
        return tile.ObjectType != 0 &&
               tile.ObjectDesc != null &&
               tile.ObjectDesc.BlocksSight;
    }
}