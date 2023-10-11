using GameServer.logic;
using GameServer.realm.entities.player;
using GameServer.realm.terrain;
using Shared.resources;
using System.Diagnostics;

namespace GameServer.realm;

public class Sight
{
    private static readonly HashSet<IntPoint> FullCache = FullCache ?? GenerateCachedPoints();

    // this is hacky and used for line algorithm
    // rather than draw line between all tiles within a circle just do the circumference requires alot less iterating as u go from 709+ tiles to check -> 100 tiles
    private static readonly HashSet<IntPoint> CircleCircumferenceCache = CircleCircumferenceCache ?? GenerateCachedPoints(true); 
    private static HashSet<IntPoint> GenerateCachedPoints(bool circumferenceCheck = false)
    {
        var ret = new HashSet<IntPoint>();
        for (var x = -Player.RADIUS; x <= Player.RADIUS; x++)
            for (var y = -Player.RADIUS; y <= Player.RADIUS; y++)
            {
                var dist = x * x + y * y;
                var flag = dist <= Player.RADIUS_SQR;
                if (circumferenceCheck)
                    flag &= dist >= Player.CIRCUMFERENCE_SQR;

                if (flag)
                    _ = ret.Add(new IntPoint(x, y));
            }
        return ret;
    }

    private readonly Player Host;
    public readonly HashSet<IntPoint> VisibleTiles = new HashSet<IntPoint>();

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
            case VisibilityType.LineOfSight:
                LineOfSight();
                break;
        }
    }

    private void FullSight()
    {
        var x = (int)Host.X;
        var y = (int)Host.Y;
        foreach (var p in FullCache)
        {
            var ip = new IntPoint(x + p.X, y + p.Y);
            if (!Host.Owner.Map.Contains(ip))
                continue;
            _ = VisibleTiles.Add(ip);
        }
    }

    private void LineOfSight()
    {
        var startX = (int)Host.X;
        var startY = (int)Host.Y;
        foreach (var point in CircleCircumferenceCache)
            BresenhamLine(startX, startY, startX + point.X, startY + point.Y, (x, y) =>
            {
                _ = VisibleTiles.Add(new IntPoint(x, y));
                return Host.Owner.Map.IsBlocking(x, y);
            });
    }

    public sealed class TimedProfiler : IDisposable
    {
        private string Message { get; }
        private Stopwatch Stopwatch { get; }

        public TimedProfiler(string message)
        {
            Message = message;
            Stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            Stopwatch.Stop();
            var time = Stopwatch.Elapsed;
            var ms = Stopwatch.Elapsed.TotalMilliseconds;
            Console.WriteLine($"{Message} - Elapsed: {time} ({ms}ms)");
        }
    }

    private static void BresenhamLine(int currentX, int currentY, int endX, int endY, Func<int, int, bool> func)
    {
        var w = endX - currentX;
        var h = endY - currentY;
        var dx1 = 0;
        var dy1 = 0;
        var dx2 = 0;
        var dy2 = 0;
        if (w < 0)
            dx1 = -1;
        else if (w > 0)
            dx1 = 1;
        if (h < 0)
            dy1 = -1;
        else if (h > 0)
            dy1 = 1;
        if (w < 0)
            dx2 = -1;
        else if (w > 0)
            dx2 = 1;

        var longest = Math.Abs(w);
        var shortest = Math.Abs(h);
        if (!(longest > shortest))
        {
            longest = Math.Abs(h);
            shortest = Math.Abs(w);
            if (h < 0)
                dy2 = -1;
            else if (h > 0)
                dy2 = 1;
            dx2 = 0;
        }

        var numerator = longest >> 1;
        for (var i = 0; i <= longest; i++)
        {
            if (func.Invoke(currentX, currentY))
                break;

            numerator += shortest;
            if (!(numerator < longest))
            {
                numerator -= longest;
                currentX += dx1;
                currentY += dy1;
            }
            else
            {
                currentX += dx2;
                currentY += dy2;
            }
        }
    }
}