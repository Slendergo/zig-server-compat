using System.Globalization;
using Shared;
using Shared.resources;
using GameServer.realm;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace GameServer;

internal class Program {
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly ManualResetEvent Shutdown = new(false);

    internal static ServerConfig Config;
    internal static Resources Resources;
    internal static Database Database;

    private static void Main(string[] args) {
        AppDomain.CurrentDomain.UnhandledException += LogUnhandledException;
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.Name = "Entry";

        Config = ServerConfig.ReadFile("GameServer.json");

        var logConfig = new LoggingConfiguration();
        var consoleTarget = new ColoredConsoleTarget("consoleTarget") {
            Layout = @"${date:format=HH\:mm\:ss} ${logger} ${message}"
        };
        logConfig.AddTarget(consoleTarget);
        logConfig.AddRule(LogLevel.Info, LogLevel.Fatal, consoleTarget);

        var fileTarget = new FileTarget("fileTarget") {
            FileName = "${var:logDirectory}/log.txt",
            Layout = @"${date:format=HH\:mm\:ss} ${logger} ${message}"
        };
        logConfig.AddTarget(fileTarget);
        LogManager.Configuration = logConfig;
        LogManager.Configuration.Variables["buildConfig"] = Utils.GetBuildConfiguration();

        using (Resources = new Resources(Config.serverSettings.resourceFolder))
        using (Database = new Database(Config.dbInfo.host,
                   Config.dbInfo.port,
                   Config.dbInfo.index,
                   Config.dbInfo.auth,
                   Resources)) {
            //var data = MapParser.ConvertWmapRealmToMapData(File.ReadAllBytes("realm.wmap"));
            //File.WriteAllBytes($"{Environment.CurrentDirectory}/realm.pmap", data);

            //data = MapParser.ConvertWmapToMapData(File.ReadAllBytes("nexus.wmap"));
            //File.WriteAllBytes($"{Environment.CurrentDirectory}/nexus.pmap", data);

            Config.serverInfo.instanceId = Guid.NewGuid().ToString();

            var manager = new RealmManager(Resources, Database, Config);
            manager.Run();

            var server = new Server(manager,
                Config.serverInfo.port,
                Config.serverSettings.maxConnections);
            
            Console.CancelKeyPress += delegate { Shutdown.Set(); };

            Shutdown.WaitOne();
            Log.Info("Terminating...");
            manager.Stop();
            server.Stop();
            Log.Info("Server terminated.");
        }
    }

    public static void Stop(Task task = null) {
        if (task != null)
            Log.Fatal(task.Exception);

        Shutdown.Set();
    }

    private static void LogUnhandledException(object sender, UnhandledExceptionEventArgs args) {
        Log.Fatal((Exception) args.ExceptionObject);
    }
}