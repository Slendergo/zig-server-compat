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
using NLog.Targets;
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

            Config = ServerConfig.ReadFile("wServer.json");

            var logConfig = new NLog.Config.LoggingConfiguration();
            var consoleTarget = new ColoredConsoleTarget("consoleTarget")
            {
                Layout = @"${date:format=HH\:mm\:ss} ${logger} ${message}"
            };
            logConfig.AddTarget(consoleTarget);
            logConfig.AddRule(LogLevel.Info, LogLevel.Fatal, consoleTarget);
            
            var fileTarget = new FileTarget("fileTarget")
            {
                FileName = "${var:logDirectory}/log.txt",
                Layout = @"${date:format=HH\:mm\:ss} ${logger} ${message}"
            };
            logConfig.AddTarget(fileTarget);
            LogManager.Configuration = logConfig;
            LogManager.Configuration.Variables["buildConfig"] = Utils.GetBuildConfiguration();

            using (Resources = new Resources(Config.serverSettings.resourceFolder, true))
            using (Database = new Database(Resources, Config))
            {
                Config.serverInfo.instanceId = Guid.NewGuid().ToString();

                var manager = new RealmManager(Resources, Database, Config);
                manager.Run();

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
