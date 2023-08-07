using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace common.resources
{
    public class Resources : IDisposable
    {
        static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public readonly string ResourcePath;
        public readonly XmlData GameData;
        public readonly Dictionary<string, byte[]> WebFiles = new Dictionary<string, byte[]>();
        public readonly WorldData Worlds;
        public readonly AppSettings Settings;

        public Resources(string resourcePath, bool wServer = false)
        {
            Log.Info("Loading resources...");
            ResourcePath = resourcePath;
            GameData = new XmlData(resourcePath + "/xml");
            Settings = new AppSettings(resourcePath + "/data/init.xml");

            if (!wServer)
            {
                webFiles(resourcePath + "/web");
            }
            else
            {
                Worlds = new WorldData(resourcePath + "/worlds", GameData);
            }
        }

        void webFiles(string dir)
        {
            Log.Info("Loading web data...");

            var files = Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var webPath = file.Substring(dir.Length, file.Length - dir.Length)
                    .Replace("\\", "/");

                WebFiles[webPath] = File.ReadAllBytes(file);
            }
        }

        public void Dispose()
        {

        }
    }
}
