using NLog;
using System.Xml.Linq;

namespace common.resources;

public class Resources : IDisposable
{
    static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public readonly string ResourcePath;
    public readonly XmlData GameData;
    public readonly Dictionary<string, byte[]> WebFiles = new();
    public readonly WorldData Worlds;
    public readonly AppSettings Settings;
    public IEnumerable<XElement> XmlBehaviors;

    public Resources(string resourcePath, bool wServer = false)
    {
        Log.Info("Loading resources...");
        ResourcePath = resourcePath;
        GameData = new XmlData(resourcePath + "/xml");
        Settings = new AppSettings(resourcePath + "/data/init.xml");
        XmlBehaviors = LoadBehaviors(resourcePath + "/behaviors");

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

    private IEnumerable<XElement> LoadBehaviors(string path)
    {
        var xmls = Directory.EnumerateFiles(path, "*.xml", SearchOption.AllDirectories).ToArray();
        for (var i = 0; i < xmls.Length; i++)
        {
            var xml = XElement.Parse(File.ReadAllText(xmls[i]));
            foreach (var elem in xml.Elements().Where(x => x.Name == "BehaviorEntry"))
                yield return elem;
        }
    }

    public void Dispose()
    {

    }
}