using System.Collections.Concurrent;
using System.Diagnostics;
using NLog;

namespace wServer.realm;

public sealed class LogicTicker {
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public readonly int MillisecondsPerTick;

    private readonly RealmManager RealmManager;

    public readonly int TPS;
    public RealmTime RealmTime;

    public LogicTicker(RealmManager manager) {
        RealmManager = manager;
        RealmTime = new RealmTime();

        TPS = manager.Config.serverSettings.tps;
        MillisecondsPerTick = 1000 / TPS;
    }

    public void TickLoop() {
        Log.Info("Logic loop started.");

        var watch = Stopwatch.StartNew();
        var lastMS = 0L;

        while (!RealmManager.Terminating) {
            var currentMS = RealmTime.TotalElapsedMs = watch.ElapsedMilliseconds;
            RealmTime.TotalElapsedMicroSeconds = (long) watch.Elapsed.TotalMicroseconds;

            var delta = (int) (currentMS - lastMS);
            if (delta >= MillisecondsPerTick) {
                RealmTime.TickCount++;
                RealmTime.ElapsedMsDelta = delta;

                var start = watch.ElapsedMilliseconds;

                RealmManager.Monitor.Tick(RealmTime);
                RealmManager.InterServer.Tick(RealmTime.ElapsedMsDelta);

                // catch per world to keep other worlds ticking if a world fails to tick??
                // though it ideally it wont and this can be removed because its just bad mindset
                foreach (var w in RealmManager.Worlds.Values)
                    try {
                        w.Tick(RealmTime);
                    }
                    catch (Exception e) {
                        Console.WriteLine(e);
                    }

                var end = watch.ElapsedMilliseconds;
                var logicExecutionTime = (int) (end - start);

                lastMS = currentMS + logicExecutionTime; // logic update time added ontop might not be neededx
            }
        }

        Log.Info("Logic loop stopped.");
    }
}