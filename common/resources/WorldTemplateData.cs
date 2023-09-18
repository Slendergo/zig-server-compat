using System.Xml.Linq;

namespace common.resources;

public enum VisibilityType {
    Full = 0,
    Path = 1,
    LineOfSight = 2
}

public enum SpeicalizedDungeonType {
    Default,
    Nexus,
    Realm,
    Vault,
    GuildHall,
    OryxCastle
}

public sealed class WorldTemplateData {
    public int Background;

    public int BackgroundLightColor;
    public float BackgroundLightIntensity;
    public float DayLightIntensity;
    public int Difficulty;
    public bool DisableTeleport;
    public string DisplayName;
    public string IdName;
    public bool Instanced;
    public string[] Maps;

    public int MaxPlayers;
    public string[] Music;
    public float NightLightIntensity;
    public bool Persists;

    public string[] Portals;
    public bool ShowDisplays;
    public SpeicalizedDungeonType Specialized;
    public VisibilityType VisibilityType;

    public WorldTemplateData(XElement e) {
        IdName = e.GetAttribute<string>("id");
        DisplayName = e.GetAttribute("displayName", IdName);
        Difficulty = e.GetAttribute("difficulty", -1);
        Instanced = e.GetAttribute("instanced", "false") == "true";
        Persists = e.GetAttribute("persists", "false") == "true";
        DisableTeleport = e.GetAttribute("disableTeleport", "false") == "true";
        ShowDisplays = e.GetAttribute("showDisplays", "false") == "true";
        VisibilityType = Enum.Parse<VisibilityType>(e.GetAttribute("visibilityType", "full"), true);
        Specialized = Enum.Parse<SpeicalizedDungeonType>(e.GetAttribute("specialized", "default"), true);

        MaxPlayers = e.GetValue("MaxPlayers", 85);
        Background = e.GetValue("Background", 0);

        BackgroundLightColor = e.GetValue("BackgroundLightColor", 0);
        BackgroundLightIntensity = e.GetValue("BackgroundLightIntensity", 0.0f);
        DayLightIntensity = e.GetValue("DayLightIntensity", 0.0f);
        NightLightIntensity = e.GetValue("NightLightIntensity", 0.0f);

        Portals = e.Element("Portal")?.GetAttribute("ids", string.Empty).CommaToArray<string>() ??
                  Array.Empty<string>();
        Music = e.Element("Music")?.GetAttribute("files", string.Empty).CommaToArray<string>() ?? Array.Empty<string>();
        Maps = e.Element("Map")?.GetAttribute("files", string.Empty).CommaToArray<string>() ?? Array.Empty<string>();
    }
}