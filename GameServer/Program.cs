using System.Globalization;
using Shared;
using Shared.resources;
using GameServer.realm;
using Org.BouncyCastle.Asn1.Cms;
using System.Text;
using System.Xml.Linq;
using GameServer.realm.worlds.parser;

namespace GameServer;

internal class Program {
    private static readonly ManualResetEvent Shutdown = new(false);

    internal static ServerConfig Config;
    internal static Resources Resources;
    internal static Database Database;

    private static void Main(string[] args) {
        AppDomain.CurrentDomain.UnhandledException += LogUnhandledException;
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.Name = "Entry";

        Config = ServerConfig.ReadFile("GameServer.json");

        using (Resources = new Resources(Config.serverSettings.resourceFolder))
        using (Database = new Database(Config.dbInfo.host, Config.dbInfo.port, Config.dbInfo.index, Config.dbInfo.auth, Resources)) {

#if DEBUG
            // this will try to convert maps every time we run.
            MapParser.ConvertMaps();
#endif

            Config.serverInfo.instanceId = Guid.NewGuid().ToString();

            var manager = new RealmManager(Resources, Database, Config);
            manager.Run();

            var server = new Server(manager,
                Config.serverInfo.port,
                Config.serverSettings.maxConnections);
            
            Console.CancelKeyPress += delegate { Shutdown.Set(); };

            Shutdown.WaitOne();
            SLog.Info("Terminating...");
            manager.Stop();
            server.Stop();
            SLog.Info("Server terminated.");
        }
    }

    public static void Stop(Task task = null) {
        if (task != null)
            SLog.Fatal(task.Exception);

        Shutdown.Set();
    }

    private static void LogUnhandledException(object sender, UnhandledExceptionEventArgs args) {
        SLog.Fatal((Exception) args.ExceptionObject);
    }
}