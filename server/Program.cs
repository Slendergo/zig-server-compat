using Anna;
using Anna.Request;
using common;
using common.resources;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace server
{
    class Program
    {
        static readonly Logger Log = LogManager.GetCurrentClassLogger();
        static readonly ManualResetEvent Shutdown = new ManualResetEvent(false);

        internal static ServerConfig Config;
        internal static Resources Resources;
        internal static Database Database;
        internal static ISManager ISManager;
        internal static ChatManager ChatManager;
        internal static ISControl ISControl;
        internal static LegendSweeper LegendSweeper;

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += LogUnhandledException;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.Name = "Entry";

            Config = args.Length > 0 ?
                ServerConfig.ReadFile(args[0]) :
                ServerConfig.ReadFile("server.json");

            LogManager.Configuration.Variables["logDirectory"] = Config.serverSettings.logFolder + "/server";
            LogManager.Configuration.Variables["buildConfig"] = Utils.GetBuildConfiguration();

            using (Resources = new Resources(Config.serverSettings.resourceFolder, false))
            using (Database = new Database(Resources, Config))
            {
                RequestHandlers.Initialize(Resources);

                Config.serverInfo.instanceId = Guid.NewGuid().ToString();
                ISManager = new ISManager(Database, Config);
                ISManager.Run();
                ChatManager = new ChatManager(ISManager);
                ISControl = new ISControl(ISManager);
                LegendSweeper = new LegendSweeper(Database);
                LegendSweeper.Run();

                Console.CancelKeyPress += delegate
                {
                    Shutdown.Set();
                };

                var port = Config.serverInfo.port;
                var address = Config.serverInfo.bindAddress;
                using (var server = new HttpServer($"http://{address}:{port}/"))
                {
                    foreach (var uri in RequestHandlers.Get.Keys)
                        server.GET(uri).Subscribe(Response);
                    foreach (var uri in RequestHandlers.Post.Keys)
                        server.POST(uri).Subscribe(Response);

                    Log.Info("Listening at address {0}:{1}...", address, port);
                    Shutdown.WaitOne();
                }

                Log.Info("Terminating...");
                ISManager.Dispose();
            }
        }

        public static void Stop(Task task = null)
        {
            if (task != null)
                Log.Fatal(task.Exception);

            Shutdown.Set();
        }

        private static void Response(RequestContext rContext)
        {
            try
            {
                Log.Trace("Dispatching '{0}'@{1}",
                    rContext.Request.Url.LocalPath,
                    rContext.Request.ClientIP());

                if (rContext.Request.HttpMethod.Equals("GET"))
                {
                    var query = HttpUtility.ParseQueryString(rContext.Request.Url.Query);
                    RequestHandlers.Get[rContext.Request.Url.LocalPath].HandleRequest(rContext, query);
                    return;
                }

                GetBody(rContext.Request, 4096).Subscribe(body =>
                {
                    try
                    {
                        var query = HttpUtility.ParseQueryString(body);
                        RequestHandlers.Post[rContext.Request.Url.LocalPath]
                            .HandleRequest(rContext, query);
                    }
                    catch (Exception e)
                    {
                        OnError(e, rContext);
                    }
                });
            }
            catch (Exception e)
            {
                OnError(e, rContext);
            }
        }

        private static void OnError(Exception e, RequestContext rContext)
        {
            Log.Error($"{e.Message}\r\n{e.StackTrace}");

            try
            {
                rContext.Respond("<Error>Internal server error</Error>", 500);
            }
            catch
            {
            }
        }

        private static IObservable<string> GetBody(Request r, int maxContentLength = 50000)
        {
            int bufferSize = maxContentLength;
            if (r.Headers.ContainsKey("Content-Length"))
                bufferSize = Math.Min(maxContentLength, int.Parse(r.Headers["Content-Length"].First()));

            var buffer = new byte[bufferSize];
            return Observable.FromAsyncPattern<byte[], int, int, int>(r.InputStream.BeginRead, r.InputStream.EndRead)(buffer, 0, bufferSize)
                .Select(bytesRead => r.ContentEncoding.GetString(buffer, 0, bytesRead));
        }

        private static void LogUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            Log.Fatal((Exception)args.ExceptionObject);
        }
    }
}
