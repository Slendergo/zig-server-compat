using System.Diagnostics;
using System.Collections.Concurrent;
using NLog;
using wServer.networking;

namespace wServer.realm;

public class FLLogicTicker
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public readonly int TPS;
    public readonly int MillisecondsPerTick;
    public RealmTime RealmTime;

    private readonly RealmManager RealmManager;
    private readonly ConcurrentQueue<Action<RealmTime>>[] PendingActions = new ConcurrentQueue<Action<RealmTime>>[5];

    public FLLogicTicker(RealmManager manager)
    {
        RealmManager = manager;
        RealmTime = new RealmTime();

        for (var i = 0; i < PendingActions.Length; i++)
            PendingActions[i] = new ConcurrentQueue<Action<RealmTime>>();

        TPS = manager.Config.serverSettings.tps;
        MillisecondsPerTick = 1000 / TPS;
    }

    public void TickLoop()
    {
        Log.Info("Logic loop started.");

        var watch = Stopwatch.StartNew();
        var lastMS = 0L;

        while (!RealmManager.Terminating)
        {
            var currentMS = RealmTime.TotalElapsedMs = watch.ElapsedMilliseconds;

            var delta = (int)(currentMS - lastMS);
            if (delta >= MillisecondsPerTick)
            {
                RealmTime.TickCount++;
                RealmTime.ElaspedMsDelta = delta;

                var start = watch.ElapsedMilliseconds;

                foreach (var i in PendingActions)
                    while (i.TryDequeue(out var callback))
                        try
                        {
                            callback.Invoke(RealmTime);
                        }
                        catch (Exception e)
                        {
                            Log.Error(e);
                        }

                RealmManager.Monitor.Tick(RealmTime);
                RealmManager.InterServer.Tick(RealmTime.ElaspedMsDelta);

                // catch per world to keep other worlds ticking if a world fails to tick??
                // though it ideally it wont and this can be removed because its just bad mindset
                foreach (var w in RealmManager.Worlds.Values)
                    try
                    {
                        w.Tick(RealmTime);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                // wth is this lol why, yeet it if possible
                foreach (var client in RealmManager.Clients.Keys)
                    if (client.Player != null && client.Player.Owner != null)
                        client.Player.Flush();

                var end = watch.ElapsedMilliseconds;
                var logicExecutionTime = (int)(end - start);

                lastMS = currentMS + logicExecutionTime; // logic update time added ontop might not be neededx
            }
        }

        Log.Info("Logic loop stopped.");
    }

    public void AddPendingAction(Action<RealmTime> callback,
        PendingPriority priority = PendingPriority.Normal)
    {
        PendingActions[(int)priority].Enqueue(callback);
    }
}