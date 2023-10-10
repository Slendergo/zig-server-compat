using GameServer.realm.entities.player;
using GameServer.realm.terrain;
using Shared.resources;

namespace GameServer.realm;

public class Sight
{
    private static readonly IntPoint[] SurroundingPoints = 
    {
        new IntPoint(1, 0),
        new IntPoint(1, 1),
        new IntPoint(0, 1),
        new IntPoint(-1, 1),
        new IntPoint(-1, 0),
        new IntPoint(-1, -1),
        new IntPoint(0, -1),
        new IntPoint(1, -1)
    };

    private static readonly List<IntPoint> FullSightCache = new List<IntPoint>();

    private readonly Player Host;
    public readonly HashSet<IntPoint> VisibleTiles = new HashSet<IntPoint>();

    static Sight()
    {
        for (var x = -Player.RADIUS; x <= Player.RADIUS; x++)
            for (var y = -Player.RADIUS; y <= Player.RADIUS; y++)
                if (x * x + y * y <= Player.RADIUS_SQR)
                    FullSightCache.Add(new IntPoint(x, y));
    }

    public Sight(Player player) => Host = player;

    public void UpdateVisibility()
    {
        if (Host.Owner == null)
            return;

        VisibleTiles.Clear();
        switch (Host.Owner.VisibilityType)
        {
            case VisibilityType.Full:
                FullSight();
                break;
            case VisibilityType.Path:
                PathSight();
                break;
            case VisibilityType.LineOfSight:
                LineOfSight();
                break;
        }
    }

    private void FullSight()
    {
        foreach (var p in FullSightCache)
        {
            var ip = new IntPoint((int)Host.X + p.X, (int)Host.Y + p.Y);
            if (!Host.Owner.Map.Contains(ip))
                continue;
            _ = VisibleTiles.Add(ip);
        }
    }

    private void PathSight()
    {
        // todo a different algorithm
        FullSight(); // temp
    }

    private void LineOfSight()
    {
        // todo use line algorithm
        FullSight(); // temp
    }

    private static bool IsBlocking(WmapTile tile) => tile.ObjectType != 0 && tile.ObjectDesc != null && tile.ObjectDesc.BlocksSight;
}