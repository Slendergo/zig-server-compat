using Shared.resources;
using GameServer.realm.entities;
using GameServer.realm.entities.player;
using NLog;

namespace GameServer.realm.worlds.logic;

public class RealmOfTheMadGod : World {
    private static string[] _realmNames = {
        "Lich", "Goblin", "Ghost",
        "Giant", "Gorgon", "Blob",
        "Leviathan", "Unicorn", "Minotaur",
        "Cube", "Pirate", "Spider",
        "Snake", "Deathmage", "Gargoyle",
        "Scorpion", "Djinn", "Phoenix",
        "Satyr", "Drake", "Orc",
        "Flayer", "Cyclops", "Sprite",
        "Chimera", "Kraken", "Hydra",
        "Slime", "Ogre", "Hobbit",
        "Titan", "Medusa", "Golem",
        "Demon", "Skeleton", "Mummy",
        "Imp", "Bat", "Wyrm",
        "Spectre", "Reaper", "Beholder",
        "Dragon", "Harpy"
    };

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly int _mapId;

    private readonly bool _oryxPresent;

    private Oryx _overseer;
    private Task _overseerTask;

    public RealmOfTheMadGod(RealmManager manager, WorldTemplateData template)
        : base(manager, template) {
        _oryxPresent = true;
        _mapId = 1;
    }

    public override bool AllowedAccess(Client client) {
        // since map gets reset, admins not allowed to join when closed. Possible to crash server otherwise.
        return !Closed && base.AllowedAccess(client);
    }

    public override void Init() {
        Log.Info("Initializing Game World {0}({1}) from map {2}...", Id, IdName, _mapId);

        DisplayName = _realmNames[Environment.TickCount % _realmNames.Length];

        base.Init();

        //SetPieces.ApplySetPieces(this);

        if (_oryxPresent) {
            _overseer = new Oryx(this);
            _overseer.Init();
        }

        Log.Info("Game World initalized.");
    }

    public override void Tick(RealmTime time) {
        if (Closed)
            Manager.Monitor.ClosePortal(Id);
        else
            Manager.Monitor.OpenPortal(Id);

        base.Tick(time);

        if (Deleted)
            return;

        if (_overseerTask == null || _overseerTask.IsCompleted)
            _overseerTask = Task.Factory.StartNew(() => {
                var secondsElapsed = time.TotalElapsedMs / 1000;
                if (secondsElapsed > 10 && secondsElapsed % 1800 < 10 && !IsClosing())
                    CloseRealm();

                if (Closed && Players.Count == 0 && _overseer != null) {
                    Init(); // will reset everything back to the way it was when made
                    Closed = false;
                }

                _overseer?.Tick(time);
            }).ContinueWith(e =>
                    Log.Error(e.Exception.InnerException.ToString()),
                TaskContinuationOptions.OnlyOnFaulted);
    }

    public void EnemyKilled(Enemy enemy, Player killer) {
        if (_overseer != null && !enemy.Spawned)
            _overseer.OnEnemyKilled(enemy, killer);
    }

    public override int EnterWorld(Entity entity, bool noIdChange = false) {
        var ret = base.EnterWorld(entity, noIdChange);
        if (entity is Player player)
            _overseer?.OnPlayerEntered(player);
        return ret;
    }

    public bool CloseRealm() {
        if (_overseer == null)
            return false;

        _overseer.InitCloseRealm();
        return true;
    }

    public bool IsClosing() {
        if (_overseer == null)
            return false;

        return _overseer.Closing;
    }
}