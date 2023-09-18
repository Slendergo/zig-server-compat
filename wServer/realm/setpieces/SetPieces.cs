using common.resources;
using NLog;
using wServer.realm.worlds;
using wServer.realm.worlds.parser;

namespace wServer.realm.setpieces;

internal class SetPieces {
    private static Logger log = LogManager.GetCurrentClassLogger();

    private static readonly List<Tuple<ISetPiece, int, int, TerrainType[]>> setPieces = new() {
        SetPiece(new Building(), 80, 100, TerrainType.LowForest, TerrainType.LowPlains, TerrainType.MidForest),
        SetPiece(new Graveyard(), 5, 10, TerrainType.LowSand, TerrainType.LowPlains),
        SetPiece(new Grove(), 17, 25, TerrainType.MidForest, TerrainType.MidPlains),
        SetPiece(new LichyTemple(), 4, 7, TerrainType.MidForest, TerrainType.MidPlains),
        SetPiece(new Castle(), 4, 7, TerrainType.HighForest, TerrainType.HighPlains),
        SetPiece(new Tower(), 8, 15, TerrainType.HighForest, TerrainType.HighPlains),
        SetPiece(new TempleA(), 10, 20, TerrainType.MidForest, TerrainType.MidPlains),
        SetPiece(new TempleB(), 10, 20, TerrainType.MidForest, TerrainType.MidPlains),
        SetPiece(new Oasis(), 0, 5, TerrainType.LowSand, TerrainType.MidSand),
        SetPiece(new Pyre(), 0, 5, TerrainType.MidSand, TerrainType.HighSand),
        SetPiece(new LavaFissure(), 3, 5, TerrainType.Mountains),
        SetPiece(new Crystal(), 1, 1, TerrainType.Mountains),
        SetPiece(new KageKami(), 2, 3, TerrainType.HighForest, TerrainType.HighPlains)
    };

    private static Tuple<ISetPiece, int, int, TerrainType[]> SetPiece(ISetPiece piece, int min, int max,
        params TerrainType[] terrains) {
        return Tuple.Create(piece, min, max, terrains);
    }

    public static int[,] rotateCW(int[,] mat) {
        var M = mat.GetLength(0);
        var N = mat.GetLength(1);
        var ret = new int[N, M];
        for (var r = 0; r < M; r++)
        for (var c = 0; c < N; c++)
            ret[c, M - 1 - r] = mat[r, c];
        return ret;
    }

    public static int[,] reflectVert(int[,] mat) {
        var M = mat.GetLength(0);
        var N = mat.GetLength(1);
        var ret = new int[M, N];
        for (var x = 0; x < M; x++)
        for (var y = 0; y < N; y++)
            ret[x, N - y - 1] = mat[x, y];
        return ret;
    }

    public static int[,] reflectHori(int[,] mat) {
        var M = mat.GetLength(0);
        var N = mat.GetLength(1);
        var ret = new int[M, N];
        for (var x = 0; x < M; x++)
        for (var y = 0; y < N; y++)
            ret[M - x - 1, y] = mat[x, y];
        return ret;
    }

    private static int DistSqr(IntPoint a, IntPoint b) {
        return (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y);
    }

    public static void ApplySetPieces(World world) {
        log.Info("Applying set pieces to world {0}({1}).", world.Id, world.IdName);

        var map = world.Map;
        int w = map.Width, h = map.Height;

        var rand = new Random();
        var rects = new HashSet<Rect>();
        foreach (var dat in setPieces) {
            var size = dat.Item1.Size;
            var count = rand.Next(dat.Item2, dat.Item3);
            for (var i = 0; i < count; i++) {
                var pt = new IntPoint();
                Rect rect;

                var max = 50;
                do {
                    pt.X = rand.Next(0, w);
                    pt.Y = rand.Next(0, h);
                    rect = new Rect {x = pt.X, y = pt.Y, w = size, h = size};
                    max--;
                } while ((Array.IndexOf(dat.Item4, map[pt.X, pt.Y].Terrain) == -1 ||
                          rects.Any(_ => Rect.Intersects(rect, _))) &&
                         max > 0);

                if (max <= 0) continue;
                dat.Item1.RenderSetPiece(world, pt);
                rects.Add(rect);
            }
        }

        log.Info("Set pieces applied.");
    }

    public static void RenderSetpiece(World world, IntPoint pos, string map) {
        var mapData = MapParser.GetOrLoad(map);
        if (mapData == null) log.Error("MapData: {0} not found.", map);

        // todo
    }

    private struct Rect {
        public int x;
        public int y;
        public int w;
        public int h;

        public static bool Intersects(Rect r1, Rect r2) {
            return !(r2.x > r1.x + r1.w ||
                     r2.x + r2.w < r1.x ||
                     r2.y > r1.y + r1.h ||
                     r2.y + r2.h < r1.y);
        }
    }
}