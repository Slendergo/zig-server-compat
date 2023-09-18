using System.Collections;
using System.Collections.Concurrent;
using Newtonsoft.Json;

namespace common;

public class ServerConfig {
    public DbInfo dbInfo { get; set; } = new();
    public ServerInfo serverInfo { get; set; } = new();
    public ServerSettings serverSettings { get; set; } = new();

    public static ServerConfig ReadFile(string fileName) {
        using (var r = new StreamReader(fileName)) {
            return ReadJson(r.ReadToEnd());
        }
    }

    public static ServerConfig ReadJson(string json) {
        return JsonConvert.DeserializeObject<ServerConfig>(json);
    }
}

public class DbInfo {
    public string host { get; set; } = "127.0.0.1";
    public int port { get; set; } = 6379;
    public string auth { get; set; } = "";
    public int index { get; set; } = 0;
}

public class ServerInfo {
    public ServerType type { get; set; } = ServerType.World;
    public string name { get; set; } = "Localhost";
    public string address { get; set; } = "127.0.0.1";
    public string bindAddress { get; set; } = "127.0.0.1";
    public int port { get; set; } = 2051;
    public Coordinates coordinates { get; set; } = new();
    public int players { get; set; } = 0;
    public int maxPlayers { get; set; } = 100;
    public bool adminOnly { get; set; } = false;
    public int minRank { get; set; } = 0;
    public string instanceId { get; set; } = "";
    public PlayerList playerList { get; set; } = new();
}

public class ServerSettings {
    public string logFolder { get; set; } = "./logs";
    public string resourceFolder { get; set; } = "./resources";
    public string version { get; set; } = "1.0.0";
    public int tps { get; set; } = 20;
    public string key { get; set; } = "B1A5ED";
    public int maxConnections { get; set; } = 256;
    public int maxPlayers { get; set; } = 100;
    public int maxPlayersWithPriority { get; set; } = 120;
    public string sendGridApiKey { get; set; } = "";
}

public enum ServerType {
    Account,
    World
}

public class Coordinates {
    public float latitude { get; set; } = 0;
    public float longitude { get; set; } = 0;
}

public class PlayerInfo {
    public int AccountId;
    public int GuildId;
    public string Name;
    public int WorldInstance;
    public string WorldName;
}

public class PlayerList : IEnumerable<PlayerInfo> {
    private readonly ConcurrentDictionary<PlayerInfo, int> PlayerInfo;

    public PlayerList(IEnumerable<PlayerInfo> playerList = null) {
        PlayerInfo = new ConcurrentDictionary<PlayerInfo, int>();

        if (playerList == null)
            return;

        foreach (var plr in playerList) Add(plr);
    }

    IEnumerator<PlayerInfo> IEnumerable<PlayerInfo>.GetEnumerator() {
        return PlayerInfo.Keys.GetEnumerator();
    }

    public IEnumerator GetEnumerator() {
        return PlayerInfo.Keys.GetEnumerator();
    }

    public void Add(PlayerInfo playerInfo) {
        PlayerInfo.TryAdd(playerInfo, 0);
    }

    public void Remove(PlayerInfo playerInfo) {
        if (playerInfo == null)
            return;

        int ignored;
        PlayerInfo.TryRemove(playerInfo, out ignored);
    }
}