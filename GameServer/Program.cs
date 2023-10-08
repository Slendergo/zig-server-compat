using System.Globalization;
using Shared;
using Shared.resources;
using GameServer.realm;
using NLog;
using NLog.Config;
using NLog.Targets;
using Org.BouncyCastle.Asn1.Cms;
using System.Text;
using System.Xml.Linq;

namespace GameServer;

internal class Program {
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly ManualResetEvent Shutdown = new(false);

    internal static ServerConfig Config;
    internal static Resources Resources;
    internal static Database Database;

    private static void Main(string[] args) {

        //var files = Directory.GetFiles("Test", "*.xml");
        //foreach(var file in files)
        //{
        //    var info = new FileInfo(file);

        //    var xml = XElement.Load(file);

        //    var tag = xml.Elements("Object").Select(_ =>
        //    {
        //        {
        //            var e = _.Element("HitSound");
        //            if (e != null)
        //            {
        //                var f = e.Value;
        //                while (f != null)
        //                {
        //                    var index = f.IndexOf('_');
        //                    if (index == -1)
        //                        break;

        //                    var sb = new StringBuilder(f);
        //                    if (f.Contains("/"))
        //                        sb[f.IndexOf("/") + 1] = char.ToUpper(f[f.IndexOf("/") + 1]);
        //                    else
        //                        sb[0] = char.ToUpper(f[0]);
        //                    sb[index] = '#';
        //                    index++;
        //                    sb[index] = char.ToUpper(f[index]);
        //                    f = sb.ToString();
        //                }
        //                e.Value = f.Replace("#", "");
        //            }
        //        }

        //        {
        //            var e = _.Element("DeathSound");
        //            if (e != null)
        //            {
        //                var f = e.Value;
        //                while (f != null)
        //                {
        //                    var index = f.IndexOf('_');
        //                    if (index == -1)
        //                        break;

        //                    var sb = new StringBuilder(f);
        //                    if (f.Contains("/"))
        //                        sb[f.IndexOf("/") + 1] = char.ToUpper(f[f.IndexOf("/") + 1]);
        //                    else
        //                        sb[0] = char.ToUpper(f[0]);
        //                    sb[index] = '#';
        //                    index++;
        //                    sb[index] = char.ToUpper(f[index]);
        //                    f = sb.ToString();
        //                }
        //                e.Value = f.Replace("#", "");
        //            }
        //        }

        //        {
        //            var e = _.Element("Sound");
        //            if (e != null)
        //            {
        //                var f = e.Value;
        //                while (f != null)
        //                {
        //                    var index = f.IndexOf('_');
        //                    if (index == -1)
        //                        break;

        //                    var sb = new StringBuilder(f);
        //                    if (f.Contains("/"))
        //                        sb[f.IndexOf("/") + 1] = char.ToUpper(f[f.IndexOf("/") + 1]);
        //                    else
        //                        sb[0] = char.ToUpper(f[0]);
        //                    sb[index] = '#';
        //                    index++;
        //                    sb[index] = char.ToUpper(f[index]);
        //                    f = sb.ToString();
        //                }
        //                e.Value = f.Replace("#", "");
        //            }
        //        }


        //        {
        //            var e = _.Element("OldSound");
        //            if (e != null)
        //            {
        //                var f = e.Value;
        //                while (f != null)
        //                {
        //                    var index = f.IndexOf('_');
        //                    if (index == -1)
        //                        break;

        //                    var sb = new StringBuilder(f);
        //                    if (f.Contains("/"))
        //                        sb[f.IndexOf("/") + 1] = char.ToUpper(f[f.IndexOf("/") + 1]);
        //                    else
        //                        sb[0] = char.ToUpper(f[0]);
        //                    sb[index] = '#';
        //                    index++;
        //                    sb[index] = char.ToUpper(f[index]);
        //                    f = sb.ToString();
        //                }
        //                e.Value = f.Replace("#", "");
        //            }
        //        }

        //        return _;
        //    }).ToList();

        //    if (tag.Count != 0)
        //    {
        //        xml = new XElement("Objects", tag);
        //        File.Delete(file);
        //        using var fs = new FileStream(file, FileMode.OpenOrCreate);
        //        xml.Save(fs);
        //    }

        //    used for Renaming Files
        //    var f = info.Name;
        //    while (true)
        //    {
        //        var index = f.IndexOf('_');
        //        if (index == -1)
        //            break;

        //        var sb = new StringBuilder(f);
        //        sb[0] = char.ToUpper(f[0]);
        //        sb[index] = '#';
        //        index++;
        //        sb[index] = char.ToUpper(f[index]);
        //        f = sb.ToString();
        //    }

        //    var path = $"Test/{f.Replace("#", "")}";
        //    File.Move(info.FullName, path);
        //}

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