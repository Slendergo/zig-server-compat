using System.Collections.Concurrent;
using Shared;
using Shared.resources;
using DungeonGen;
using DungeonGen.templates;
using GameServer.realm.entities;
using GameServer.realm.entities.player;
using GameServer.realm.entities.vendors;
using GameServer.realm.terrain;
using GameServer.realm.worlds.logic;
using GameServer.realm.worlds.parser;
using NLog;
using GameServer.logic.behaviors;
using System.Security.Cryptography;

namespace GameServer.realm.worlds;

public class World {
    //public const int Tutorial = -1;
    public const int Nexus = -2;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    protected static readonly Random Rand = new((int) DateTime.Now.Ticks);


    private static int _entityInc;

    private readonly object _deleteLock = new();
    private int _totalConnects;

    private long ElapsedTime;

    public World(RealmManager manager, WorldTemplateData template) {
        Manager = manager;

        IdName = template.IdName;
        DisplayName = template.DisplayName;
        Difficulty = template.Difficulty;
        Background = template.Background;
        Persists = template.Persists;
        MaxPlayers = template.MaxPlayers;
        AllowTeleport = !template.DisableTeleport;
        ShowDisplays = template.ShowDisplays;
        VisibilityType = template.VisibilityType;

        BgLightColor = template.BackgroundLightColor;
        BgLightIntensity = template.BackgroundLightIntensity;
        DayLightIntensity = template.DayLightIntensity;
        NightLightIntensity = template.NightLightIntensity;
    }
    //public const int Test = -3;
    //public const int Vault = -4;
    //public const int Realm = -5;
    //public const int GuildHall = -6;

    public RealmManager Manager { get; }

    public int Id { get; internal set; }
    public string IdName { get; set; }
    public string DisplayName { get; set; }
    public int Difficulty { get; protected set; }
    public int MaxPlayers { get; protected set; }
    public int Background { get; protected set; }
    public bool AllowTeleport { get; protected set; }
    public bool ShowDisplays { get; protected set; }
    public bool Persists { get; protected set; }
    public VisibilityType VisibilityType { get; protected set; }

    public int BgLightColor { get; protected set; }
    public float BgLightIntensity { get; protected set; }
    public float DayLightIntensity { get; protected set; }
    public float NightLightIntensity { get; protected set; }

    public Wmap Map { get; private set; }
    public bool Deleted { get; protected set; }
    public int TotalConnects => _totalConnects;
    public bool Closed { get; set; }

    public ConcurrentDictionary<int, Player> Players { get; private set; } = new();
    public ConcurrentDictionary<int, Enemy> Enemies { get; private set; } = new();
    public ConcurrentDictionary<int, Enemy> Quests { get; } = new();
    public ConcurrentDictionary<int, Pet> Pets { get; private set; } = new();
    public ConcurrentDictionary<Tuple<int, byte>, Projectile> Projectiles { get; private set; } = new();
    public ConcurrentDictionary<int, StaticObject> StaticObjects { get; private set; } = new();
    public List<WorldTimer> Timers { get; } = new();

    public CollisionMap<Entity> EnemiesCollision { get; private set; }
    public CollisionMap<Entity> PlayersCollision { get; private set; }

    public string GetDisplayName() {
        if (DisplayName != null && DisplayName.Length > 0)
            return DisplayName;
        return IdName;
    }

    public virtual bool AllowedAccess(Client client) {
        return !Closed || client.Account.Admin;
    }

    public virtual KeyValuePair<IntPoint, TileRegion>[] GetSpawnPoints() {
        return Map.Regions.Where(t => t.Value == TileRegion.Spawn).ToArray();
    }

    public long GetAge() {
        return ElapsedTime;
    }

    public virtual void Init() { }

    public bool Delete() {
        using (TimedLock.Lock(_deleteLock)) {
            if (Players.Count > 0)
                return false;

            Deleted = true;
            Manager.RemoveWorld(this);
            Id = 0;

            DisposeEntities(Players);
            DisposeEntities(Enemies);
            DisposeEntities(Projectiles);
            DisposeEntities(StaticObjects);
            DisposeEntities(Pets);

            Players = null;
            Enemies = null;
            Projectiles = null;
            StaticObjects = null;
            Pets = null;

            return true;
        }
    }

    private void DisposeEntities<T, TU>(ConcurrentDictionary<T, TU> dictionary) {
        var entities = dictionary.Values.ToArray();
        foreach (var entity in entities)
            (entity as Entity).Dispose();
    }

    protected void FromDungeonGen(int seed, DungeonTemplate template) {
        Log.Info("Loading template for world {0}({1})...", Id, IdName);

        var gen = new Generator(seed, template);
        gen.Generate();
        var ras = new Rasterizer(seed, gen.ExportGraph());
        ras.Rasterize();
        var dTiles = ras.ExportMap();

        if (Map == null) {
            Map = new Wmap(Manager.Resources.GameData);
            Interlocked.Add(ref _entityInc, Map.Load(dTiles, _entityInc));
        }
        else {
            Map.ResetTiles();
        }

        InitMap();
    }

    public virtual string SelectMap(WorldTemplateData template) {
        return template.Maps[Rand.Next(0, template.Maps.Length)];
    }

    public void LoadMapFromData(MapData mapData) {
        Log.Info("Loading map for world {0}({1})...", Id, IdName);

        // assume nothing is wrong, this should be allowed to crash and cause issues so devs will fix the missing maps,
        // preferably not during production runs
        // to save time rewriting entire world system im doing this 
        // ~Slendergo

        if (Map == null) {
            Map = new Wmap(Manager.Resources.GameData);
            _ = Interlocked.Add(ref _entityInc, Map.LoadFromMapData(mapData, _entityInc));
        }
        else {
            Map.ResetTiles();
        }

        InitMap();
    }

    private void InitMap() {
        var w = Map.Width;
        var h = Map.Height;

        EnemiesCollision = new CollisionMap<Entity>(0, w, h);
        PlayersCollision = new CollisionMap<Entity>(1, w, h);

        Projectiles.Clear();
        StaticObjects.Clear();
        Enemies.Clear();
        Players.Clear();
        Quests.Clear();
        Timers.Clear();

        foreach (var entity in Map.InstantiateEntities(Manager))
            _ = EnterWorld(entity);

        foreach (var shop in MerchantLists.Shops) {
            var shopItems = new List<ISellableItem>(shop.Value.Item1);
            var mLocations = Map.Regions
                .Where(r => shop.Key == r.Value)
                .Select(r => r.Key)
                .ToArray();

            if (shopItems.Count <= 0 || shopItems.All(i => i.ItemId == ushort.MaxValue))
                continue;

            var rotate = shopItems.Count > mLocations.Length;

            var reloadOffset = 0;
            foreach (var loc in mLocations) {
                var shopItem = shopItems[0];
                shopItems.RemoveAt(0);
                while (shopItem.ItemId == ushort.MaxValue) {
                    if (shopItems.Count <= 0)
                        shopItems.AddRange(shop.Value.Item1);

                    shopItem = shopItems[0];
                    shopItems.RemoveAt(0);
                }

                reloadOffset += 500;
                var m = new WorldMerchant(Manager, 0x01ca) {
                    ShopItem = shopItem,
                    Item = shopItem.ItemId,
                    Price = shopItem.Price,
                    Count = shopItem.Count,
                    Currency = shop.Value.Item2,
                    RankReq = shop.Value.Item3,
                    ItemList = shop.Value.Item1,
                    TimeLeft = -1,
                    ReloadOffset = reloadOffset,
                    Rotate = rotate
                };

                m.Move(loc.X + .5f, loc.Y + .5f);
                EnterWorld(m);

                if (shopItems.Count <= 0)
                    shopItems.AddRange(shop.Value.Item1);
            }
        }
    }

    public void AddProjectile(Projectile projectile)
    {
        var index = Tuple.Create(projectile.ProjectileOwner.Self.Id, projectile.ProjectileId);
        Projectiles[index] = projectile;
    }

    public bool RemoveProjectile(Projectile projectile)
    {
        var index = Tuple.Create(projectile.ProjectileOwner.Self.Id, projectile.ProjectileId);
        return Projectiles.TryRemove(index, out _);
    }

    public virtual int EnterWorld(Entity entity, bool noIdChange = false) {
        switch (entity) {
            case Player player:
                if (!noIdChange)
                    player.Id = GetNextEntityId();
                player.Init(this);
                Players.TryAdd(player.Id, player);
                PlayersCollision.Insert(player);
                Interlocked.Increment(ref _totalConnects);
                break;
            case Enemy enemy: {
                enemy.Id = GetNextEntityId();
                enemy.Init(this);
                Enemies.TryAdd(enemy.Id, enemy);
                EnemiesCollision.Insert(enemy);
                if (enemy.ObjectDesc.Quest)
                    Quests.TryAdd(enemy.Id, enemy);
                break;
            }
            case StaticObject staticObject: {
                staticObject.Id = GetNextEntityId();
                staticObject.Init(this);
                StaticObjects.TryAdd(staticObject.Id, staticObject);
                if (staticObject is Decoy)
                    PlayersCollision.Insert(staticObject);
                else
                    EnemiesCollision.Insert(staticObject);
                break;
            }
            case Pet pet:
                pet.Id = GetNextEntityId();
                pet.Init(this);
                Pets.TryAdd(pet.Id, pet);
                PlayersCollision.Insert(pet);
                break;
        }

        return entity.Id;
    }

    public virtual void LeaveWorld(Entity entity) {
        if (entity is Player) {
            Players.TryRemove(entity.Id, out var player);
            PlayersCollision.Remove(entity);

            // if in trade, cancel it...
            if (player.tradeTarget != null)
                player.CancelTrade();

            if (player.Pet != null)
                LeaveWorld(player.Pet);
        }
        else if (entity is Enemy) {
            Enemy dummy;
            Enemies.TryRemove(entity.Id, out dummy);
            EnemiesCollision.Remove(entity);
            if (entity.ObjectDesc.Quest)
                Quests.TryRemove(entity.Id, out dummy);
        }
        else if (entity is StaticObject) {
            StaticObject dummy;
            StaticObjects.TryRemove(entity.Id, out dummy);

            if (entity.ObjectDesc?.BlocksSight == true)
                foreach (var plr in Players.Values.Where(p =>
                             MathsUtils.DistSqr(p.X, p.Y, entity.X, entity.Y) < Player.RadiusSqr))
                    plr.Sight.UpdateCount++;

            if (entity is Decoy)
                PlayersCollision.Remove(entity);
            else
                EnemiesCollision.Remove(entity);
        }
        else if (entity is Pet) {
            Pet dummy;
            Pets.TryRemove(entity.Id, out dummy);
            PlayersCollision.Remove(entity);
        }

        entity.Dispose();
    }

    public int GetNextEntityId() {
        return Interlocked.Increment(ref _entityInc);
    }

    public Entity GetEntity(int id) {
        if (Players.TryGetValue(id, out var ret1)) return ret1;
        if (Enemies.TryGetValue(id, out var ret2)) return ret2;
        if (StaticObjects.TryGetValue(id, out var ret3)) return ret3;
        return null;
    }

    public Player GetUniqueNamedPlayer(string name) {
        if (Database.GuestNames.Contains(name))
            return null;

        foreach (var i in Players)
            if (i.Value.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)) {
                if (!i.Value.NameChosen && !(this is Test))
                    Manager.Database.ReloadAccount(i.Value.Client.Account);

                if (i.Value.Client.Account.NameChosen)
                    return i.Value;

                break;
            }

        return null;
    }

    public bool IsPassable(double x, double y, bool spawning = false) {
        var x_ = (int) x;
        var y_ = (int) y;

        if (!Map.Contains(x_, y_))
            return false;

        var tile = Map[x_, y_];

        if (tile.TileDesc.NoWalk)
            return false;

        if (tile.ObjectType != 0 && tile.ObjectDesc != null)
            if (tile.ObjectDesc.FullOccupy || tile.ObjectDesc.EnemyOccupySquare ||
                spawning && tile.ObjectDesc.OccupySquare)
                return false;

        return true;
    }

    public void QuakeToWorld(World newWorld) {
        if (!Persists || this is RealmOfTheMadGod)
            Closed = true;

        foreach (var player in Players.Values)
            player.Client.SendShowEffect(EffectType.Earthquake, 0, new Position(), new Position(), new ARGB(0));

        Timers.Add(new WorldTimer(8000, (w, t) => {
            foreach (var plr in w.Players.Values)
                if (plr.HasConditionEffect(ConditionEffects.Paused)) {
                    plr.Client.Reconnect("Nexus", Nexus);
                }
                else {
                    plr.Client.Reconnect(newWorld.IdName, newWorld.Id);
                }
        }));

        if (!Persists)
            Timers.Add(new WorldTimer(20000, (w2, t2) => {
                foreach (var plr in w2.Players.Values)
                    plr.Client.Disconnect();
            }));
    }

    public void ChatReceived(Player player, string text) {
        foreach (var en in Enemies)
            en.Value.OnChatTextReceived(player, text);
        foreach (var en in StaticObjects)
            en.Value.OnChatTextReceived(player, text);
    }

    public Position? GetRegionPosition(TileRegion region) {
        if (Map.Regions.All(t => t.Value != region))
            return null;

        var reg = Map.Regions.Single(t => t.Value == region);
        return new Position {X = reg.Key.X, Y = reg.Key.Y};
    }

    public virtual void Tick(RealmTime time) {
        // if Tick is overrided and you make a call to this function
        // make sure not to do anything after the call (or at least check)
        // as it is possible for the world to have been removed at that point.

        ElapsedTime += time.ElapsedMsDelta;

        if (!Persists && ElapsedTime > 60000 && Players.IsEmpty) {
            _ = Delete();
            return;
        }

        for (var i = Timers.Count - 1; i >= 0; i--)
            try {
                if (Timers[i].Tick(this, time))
                    Timers.RemoveAt(i);
            }
            catch (Exception e) {
                var msg = e.Message + "\n" + e.StackTrace;
                Log.Error(msg);
                Timers.RemoveAt(i);
            }

        // if player excepts we will not brick other players or entities
        foreach (var i in Players)
            try {
                i.Value.Tick(time);
            }
            catch (Exception e) {
                var msg = "Player: " + e.Message + "\n" + e.StackTrace;
                Log.Error(msg);
            }

        if (EnemiesCollision != null) {
            foreach (var i in EnemiesCollision.GetActiveChunks(PlayersCollision))
                i.Tick(time);

            foreach (var i in StaticObjects.Where(x => x.Value is Decoy))
                i.Value.Tick(time);
        }
        else {
            foreach (var i in Enemies)
                i.Value.Tick(time);

            foreach (var i in StaticObjects)
                i.Value.Tick(time);
        }

        foreach (var i in Pets)
            i.Value.Tick(time);

        foreach (var i in Projectiles)
            i.Value.Tick(time);
    }

    public Projectile GetProjectile(int objectId, int bulletId) {
        var entity = GetEntity(objectId);
        if (entity != null)
            return ((IProjectileOwner) entity).Projectiles[bulletId];
        return Projectiles.SingleOrDefault(p => p.Value.ProjectileOwner.Self.Id == objectId && p.Value.ProjectileId == bulletId).Value;
    }
}