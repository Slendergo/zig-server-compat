﻿using System.Xml.Linq;
using NLog;

namespace Shared.resources;

public class Resources : IDisposable {
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public XmlData GameData;

    public readonly string ResourcePath;
    public readonly AppSettings Settings;
    public readonly Dictionary<string, byte[]> WebFiles = new();
    public IEnumerable<XElement> XmlBehaviors;

    public Resources(string resourcePath, bool appEngine = false) {
        SLog.Info("Loading resources...");
        ResourcePath = resourcePath;
        GameData = new XmlData(resourcePath + "/xml");
        Settings = new AppSettings(resourcePath + "/data/init.xml");
        XmlBehaviors = LoadBehaviors(resourcePath + "/behaviors");

        if (appEngine)
            webFiles(resourcePath + "/web");
    }

    public void Dispose() { }

    private void webFiles(string dir) {
        SLog.Info("Loading web data...");

        var files = Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories);
        foreach (var file in files) {
            var webPath = file.Substring(dir.Length, file.Length - dir.Length)
                .Replace("\\", "/");

            WebFiles[webPath] = File.ReadAllBytes(file);
        }
    }

    private IEnumerable<XElement> LoadBehaviors(string path) {
        var xmls = Directory.EnumerateFiles(path, "*.xml", SearchOption.AllDirectories).ToArray();
        for (var i = 0; i < xmls.Length; i++) {
            var xml = XElement.Parse(File.ReadAllText(xmls[i]));
            foreach (var elem in xml.Elements().Where(x => x.Name == "BehaviorEntry"))
                yield return elem;
        }
    }
}