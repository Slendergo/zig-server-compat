using System.Collections.Concurrent;
using common;
using common.resources;
using NLog;
using wServer.logic;
using wServer.networking;
using wServer.realm.commands;
using wServer.realm.entities.vendors;
using wServer.realm.worlds;
using wServer.realm.worlds.logic;
using wServer.realm.worlds.parser;

namespace wServer.realm;

public struct RealmTime {
    public long TickCount;
    public long TotalElapsedMs;
    public long TotalElapsedMicroSeconds;
    public int ElapsedMsDelta;
}

public enum PendingPriority {
    Emergent,
    Destruction,
    Normal,
    Creation
}

public class RealmManager {
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public readonly ConcurrentDictionary<Client, PlayerInfo> Clients = new();

    public readonly ConcurrentDictionary<int, World> Worlds = new();
    private int _nextClientId;

    private int _nextWorldId;

    public RealmManager(Resources resources, Database db, ServerConfig config) {
        Log.Info("Initalizing Realm Manager...");

        Resources = resources;
        Database = db;
        Config = config;
        InstanceId = config.serverInfo.instanceId;

        // all these deal with db pub/sub... probably should put more thought into their structure...
        InterServer = new ISManager(Database, config);
        ISControl = new ISControl(this);
        Chat = new ChatManager(this);
        DbServerController = new DbServerManager(this); // probably could integrate this with ChatManager and rename...
        DbEvents = new DbEvents(this);

        // basic server necessities
        ConMan = new ConnectManager(this,
            config.serverSettings.maxPlayers,
            config.serverSettings.maxPlayersWithPriority);
        Behaviors = new BehaviorDb(this);
        Commands = new CommandManager(this);

        // some necessities that shouldn't be (will work this out later)
        MerchantLists.Init(this);

        var nexus = CreateNewWorld("Nexus");

        Monitor = new PortalMonitor(this, nexus);

        CreateNewRealm();

        Log.Info("Realm Manager initialized.");
    }

    public string InstanceId { get; private set; }
    public bool Terminating { get; private set; }

    public Resources Resources { get; }
    public Database Database { get; }
    public ServerConfig Config { get; }

    public ConnectManager ConMan { get; }
    public BehaviorDb Behaviors { get; private set; }
    public ISManager InterServer { get; }
    public ISControl ISControl { get; private set; }
    public ChatManager Chat { get; private set; }
    public DbServerManager DbServerController { get; private set; }
    public CommandManager Commands { get; private set; }
    public PortalMonitor Monitor { get; }
    public DbEvents DbEvents { get; private set; }
    public LogicTicker Logic { get; private set; }

    public void Run() {
        Log.Info("Starting Realm Manager...");

        // start server logic management
        Logic = new LogicTicker(this);
        var logic = new Task(() => Logic.TickLoop(), TaskCreationOptions.LongRunning);
        logic.ContinueWith(Program.Stop, TaskContinuationOptions.OnlyOnFaulted);
        logic.Start();

        Log.Info("Realm Manager started.");
    }

    public void Stop() {
        Log.Info("Stopping Realm Manager...");

        Terminating = true;
        InterServer.Dispose();
        Resources.Dispose();

        Log.Info("Realm Manager stopped.");
    }

    public bool TryConnect(Client client) {
        if (client?.Account == null)
            return false;

        client.Id = Interlocked.Increment(ref _nextClientId);
        var plrInfo = new PlayerInfo {
            AccountId = client.Account.AccountId,
            GuildId = client.Account.GuildId,
            Name = client.Account.Name,
            WorldInstance = -1
        };
        Clients[client] = plrInfo;

        // recalculate usage statistics
        Config.serverInfo.players = ConMan.GetPlayerCount();
        Config.serverInfo.maxPlayers = Config.serverSettings.maxPlayers;
        Config.serverInfo.playerList.Add(plrInfo);
        return true;
    }

    public void Disconnect(Client client) {
        var player = client.Player;
        player?.Owner?.LeaveWorld(player);

        Clients.TryRemove(client, out var plrInfo);
        
        // recalculate usage statistics
        Config.serverInfo.players = ConMan.GetPlayerCount();
        Config.serverInfo.playerList.Remove(plrInfo);
    }

    public void CreateNewRealm() {
        var world = CreateNewWorld("Realm of the Mad God");
        _ = Monitor.AddPortal(world.Id);
    }

    public World CreateNewWorld(string name, Client client = null) {
        return CreateNewWorld(Program.Resources.GameData.WorldTemplates.GetValueOrDefault(name), client);
    }

    public World CreateNewWorld(WorldTemplateData template, Client client = null) {
        if (template == null) {
            Console.WriteLine($"Unable to find template: {template}");
            return null;
        }

        World world;
        switch (template.Specialized) {
            case SpeicalizedDungeonType.Nexus:
                // dont make two nexus's
                if (Worlds.ContainsKey(World.Nexus))
                    return null;
                world = new Nexus(this, template);
                world.Id = World.Nexus; // special case only for nexus
                break;
            //case SpeicalizedDungeonType.Test:
            //    world = new Test(this, client, template);
            //    break;
            case SpeicalizedDungeonType.Vault:
                world = new Vault(this, template, client);
                break;
            case SpeicalizedDungeonType.Realm:
                world = new RealmOfTheMadGod(this, template);
                break;
            case SpeicalizedDungeonType.GuildHall:
                world = new GuildHall(this, template, client);
                break;
            case SpeicalizedDungeonType.OryxCastle:
                world = new OryxCastle(this, template);
                break;
            default:
                world = new World(this, template);
                break;
        }

        if (world.Id == 0)
            world.Id = Interlocked.Increment(ref _nextWorldId);

        var selectedMapData = MapParser.GetOrLoad(world.SelectMap(template));
        if (selectedMapData == null) {
            Console.WriteLine($"Unable to find MapData: {selectedMapData}");
            return null;
        }

        world.LoadMapFromData(selectedMapData);
        world.Init();

        Log.Info("World {0}({1}) added. {2} Worlds existing.", world.Id, world.IdName, Worlds.Count);
        Worlds[world.Id] = world;
        return world;
    }

    public World GetWorld(int id) {
        return Worlds.GetValueOrDefault(id);
    }

    public bool RemoveWorld(World world) {
        if (world.Manager == null)
            throw new InvalidOperationException("World is not added.");

        if (Worlds.TryRemove(world.Id, out world)) {
            OnWorldRemoved(world);
            return true;
        }

        return false;
    }

    private void OnWorldRemoved(World world) {
        //world.Manager = null;
        Monitor.RemovePortal(world.Id);
        Log.Info("World {0}({1}) removed.", world.Id, world.IdName);
    }

    public World GetRandomRealm() {
        var realms = Worlds.Values
            .OfType<RealmOfTheMadGod>()
            .Where(w => !w.Closed)
            .ToArray();

        return realms.Length == 0 ? Worlds[World.Nexus] : realms[Environment.TickCount % realms.Length];
    }
}