using common;
using common.resources;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using wServer.networking;
using wServer.networking.server;
using wServer.realm;

namespace wServer
{
    class Program
    {
        static readonly Logger Log = LogManager.GetCurrentClassLogger();
        static readonly ManualResetEvent Shutdown = new ManualResetEvent(false);

        internal static ServerConfig Config;
        internal static Resources Resources;
        internal static Database Database;

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += LogUnhandledException;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.Name = "Entry";

            Config = args.Length > 0 ?
                ServerConfig.ReadFile(args[0]) :
                ServerConfig.ReadFile("wServer.json");

            LogManager.Configuration.Variables["logDirectory"] = Config.serverSettings.logFolder + "/wServer";
            LogManager.Configuration.Variables["buildConfig"] = Utils.GetBuildConfiguration();

            using (Resources = new Resources(Config.serverSettings.resourceFolder, true))
            using (Database = new Database(Resources, Config))
            {
                Config.serverInfo.instanceId = Guid.NewGuid().ToString();

                var manager = new RealmManager(Resources, Database, Config);
                manager.Run();

                var policy = new PolicyServer();
                policy.Start();

                var server = new Server(manager,
                    Config.serverInfo.port,
                    Config.serverSettings.maxConnections,
                    StringUtils.StringToByteArray(Config.serverSettings.key));
                server.Start();

                Console.CancelKeyPress += delegate
                {
                    Shutdown.Set();
                };

                Shutdown.WaitOne();
                Log.Info("Terminating...");
                manager.Stop();
                server.Stop();
                policy.Stop();
                Log.Info("Server terminated.");
            }
        }

        public static void Stop(Task task = null)
        {
            if (task != null)
                Log.Fatal(task.Exception);

            Shutdown.Set();
        }

        private static void LogUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            Log.Fatal((Exception)args.ExceptionObject);
        }
    }
}
