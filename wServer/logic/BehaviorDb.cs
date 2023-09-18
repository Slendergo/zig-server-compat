using common;
using common.resources;
using NLog;
using wServer.logic.loot;
using wServer.realm;
using wServer.realm.entities;

namespace wServer.logic;

public class BehaviorDb {
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private static int _initializing;
    internal static BehaviorDb InitDb;

    public BehaviorDb(RealmManager manager) {
        Log.Info("Initializing behavior database...");

        Manager = manager;
        MobDrops.Init(manager);

        Definitions = new Dictionary<ushort, Tuple<State, Loot>>();

        if (Interlocked.Exchange(ref _initializing, 1) == 1) {
            Log.Error("Attempted to initialize multiple BehaviorDb at the same time.");
            throw new InvalidOperationException("Attempted to initialize multiple BehaviorDb at the same time.");
        }

        InitDb = this;

        ResolveBehaviors();
        _initializing = 0;
        Log.Info("Behavior database initialized...");
    }

    public RealmManager Manager { get; }
    internal static XmlData InitGameData => InitDb.Manager.Resources.GameData;

    public Dictionary<ushort, Tuple<State, Loot>> Definitions { get; }

    public void ResolveBehaviors(bool loaded = false) {
        var dat = InitDb.Manager.Resources;
        var id2ObjType = dat.GameData.IdToObjectType;
        foreach (var xmlBehavior in dat.XmlBehaviors) {
            var entry = new XmlBehaviorEntry(xmlBehavior, xmlBehavior.GetAttribute<string>("id"));
            var rootState = entry.Behaviors.OfType<State>()
                .FirstOrDefault(x => x.Name == "root");
            if (rootState == null) {
                Log.Error($"Error when adding \"{entry.Id}\": no root state.");
                continue;
            }

            var d = new Dictionary<string, State>();
            rootState.Resolve(d);
            rootState.ResolveChildren(d);
            if (!id2ObjType.ContainsKey(entry.Id)) {
                Log.Error($"Error when adding \"{entry.Id}\": entity not found.");
                continue;
            }

            if (entry.Loots.Length > 0) {
                var loot = new Loot(entry.Loots);
                rootState.Death += (_, e) => loot.Handle((Enemy) e.Host);
                if (loaded) {
                    Definitions[id2ObjType[entry.Id]] = new Tuple<State, Loot>(rootState, loot);
                    continue;
                }

                Definitions.Add(id2ObjType[entry.Id], new Tuple<State, Loot>(rootState, loot));
            }
            else {
                if (loaded) {
                    Definitions[id2ObjType[entry.Id]] = new Tuple<State, Loot>(rootState, null);
                    continue;
                }

                Definitions.Add(id2ObjType[entry.Id], new Tuple<State, Loot>(rootState, null));
            }
        }

        Log.Info($"Loaded {Definitions.Count} XML Behaviors.");
    }

    public void ResolveBehavior(Entity entity) {
        if (Definitions.TryGetValue(entity.ObjectType, out var def))
            entity.SwitchTo(def.Item1);
    }
}