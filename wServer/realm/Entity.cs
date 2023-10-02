using common.resources;
using NLog;
using wServer.logic;
using wServer.logic.transitions;
using wServer.realm.entities;
using wServer.realm.entities.vendors;
using wServer.realm.worlds;

namespace wServer.realm;

public class Entity : IProjectileOwner, ICollidable<Entity> {
    private const int EffectCount = 29;

    protected static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly SV<int> _altTextureIndex;
    private readonly SV<ulong> _conditionEffects1;

    private readonly ObjectDesc _desc;

    private readonly SV<string> _name;

    private readonly Position[] _posHistory;
    private readonly Projectile[] _projectiles;
    private readonly SV<int> _size;
    private readonly SV<float> _x;
    private readonly SV<float> _y;
    private ConditionEffects _conditionEffects;
    private int _originalSize;
    private byte _posIdx;
    private bool _stateEntry;
    private State _stateEntryCommonRoot;
    private Dictionary<object, object> _states;
    private bool _tickingEffects;
    public Player Controller;
    private int[] EffectDuration;
    public bool GivesNoXp;
    protected byte projectileId;

    public bool Spawned;

    protected Entity(RealmManager manager, ushort objType) {
        ObjectId = manager.Resources.GameData.ObjectTypeToId[objType];

        _name = new SV<string>(this, StatsType.Name, "");
        _size = new SV<int>(this, StatsType.Size, 100);
        _originalSize = 100;
        _altTextureIndex = new SV<int>(this, StatsType.AltTextureIndex, -1);
        _x = new SV<float>(this, StatsType.None, 0);
        _y = new SV<float>(this, StatsType.None, 0);
        _conditionEffects1 = new SV<ulong>(this, StatsType.Effects, 0);

        ObjectType = objType;
        Manager = manager;
        manager.Behaviors.ResolveBehavior(this);
        manager.Resources.GameData.ObjectDescs.TryGetValue(ObjectType, out _desc);
        if (_desc == null)
            return;

        if (_desc.Player) {
            _posHistory = new Position[256];
            _projectiles = new Projectile[256];
            EffectDuration = new int[EffectCount];
            return;
        }

        if (_desc.Enemy && !_desc.Static) {
            _projectiles = new Projectile[256];
            EffectDuration = new int[EffectCount];
            return;
        }

        if (_desc.Character) EffectDuration = new int[EffectCount];
    }

    public RealmManager Manager { get; }
    public World Owner { get; private set; }
    public int Id { get; internal set; }
    public ushort ObjectType { get; protected set; }
    public string ObjectId { get; protected set; }
    public Player AttackTarget { get; set; }
    public int LootValue { get; set; } = 1;
    public ObjectDesc ObjectDesc => _desc;

    public string Name {
        get => _name.GetValue();
        set => _name.SetValue(value);
    }

    public int Size {
        get => _size.GetValue();
        set => _size.SetValue(value);
    }

    public int AltTextureIndex {
        get => _altTextureIndex.GetValue();
        set => _altTextureIndex.SetValue(value);
    }

    public ConditionEffects ConditionEffects {
        get => _conditionEffects;
        set {
            _conditionEffects = value;
            _conditionEffects1?.SetValue((ulong) value);
        }
    }

    public float RealX => _x.GetValue();
    public float RealY => _y.GetValue();

    public bool TickStateManually { get; set; }
    public State CurrentState { get; private set; }

    public IDictionary<object, object> StateStorage {
        get {
            if (_states == null) _states = new Dictionary<object, object>();
            return _states;
        }
    }

    public CollisionNode<Entity> CollisionNode { get; set; }
    public CollisionMap<Entity> Parent { get; set; }

    public float X {
        get => _x.GetValue();
        private set => _x.SetValue(value);
    }

    public float Y {
        get => _y.GetValue();
        private set => _y.SetValue(value);
    }

    Entity IProjectileOwner.Self => this;
    Projectile[] IProjectileOwner.Projectiles => _projectiles;
    public event EventHandler<StatChangedEventArgs> StatChanged;

    protected virtual void ExportStats(IDictionary<StatsType, object> stats) {
        stats[StatsType.Name] = Name;
        stats[StatsType.Size] = Size;
        if (AltTextureIndex != -1)
            stats[StatsType.AltTextureIndex] = AltTextureIndex;
        stats[StatsType.Effects] = _conditionEffects1.GetValue();
    }

    public ObjectStats ExportStats() {
        var stats = new Dictionary<StatsType, object>();
        ExportStats(stats);

        return new ObjectStats {
            Id = Id,
            Position = new Position {X = RealX, Y = RealY},
            Stats = stats.ToArray()
        };
    }

    public ObjectDef ToDefinition() {
        return new ObjectDef {
            ObjectType = ObjectType,
            Stats = ExportStats()
        };
    }

    public virtual void Init(World owner) {
        Owner = owner;
    }

    public virtual void Tick(RealmTime time) {
        if (this is Projectile || Owner == null) return;
        if (CurrentState != null && Owner != null)
            if (!HasConditionEffect(ConditionEffects.Stasis) &&
                !TickStateManually &&
                (this.AnyPlayerNearby() || ConditionEffects != 0))
                TickState(time);
        if (_posHistory != null)
            _posHistory[++_posIdx] = new Position {X = X, Y = Y};
        if (EffectDuration != null)
            ProcessConditionEffects(time);
    }

    public void SwitchTo(State state) {
        var origState = CurrentState;

        CurrentState = state;
        GoDeeeeeeeep();

        _stateEntryCommonRoot = State.CommonParent(origState, CurrentState);
        _stateEntry = true;
    }

    private void GoDeeeeeeeep() {
        //always the first deepest sub-state
        if (CurrentState == null) return;
        while (CurrentState.States.Count > 0)
            CurrentState = CurrentState = CurrentState.States[0];
    }

    public void TickState(RealmTime time) {
        if (_stateEntry) {
            //State entry
            var s = CurrentState;
            while (s != null && s != _stateEntryCommonRoot) {
                foreach (var i in s.Behaviors)
                    i.OnStateEntry(this, time);
                s = s.Parent;
            }

            _stateEntryCommonRoot = null;
            _stateEntry = false;
        }

        var origState = CurrentState;
        var state = CurrentState;
        var transited = false;
        while (state != null) {
            if (!transited)
                foreach (var i in state.Transitions)
                    if (i.Tick(this, time)) {
                        transited = true;
                        break;
                    }

            foreach (var i in state.Behaviors) {
                if (Owner == null) break;
                i.Tick(this, time);
            }

            if (Owner == null) break;

            state = state.Parent;
        }

        if (transited) {
            //State exit
            var s = origState;
            while (s != null && s != _stateEntryCommonRoot) {
                foreach (var i in s.Behaviors)
                    i.OnStateExit(this, time);
                s = s.Parent;
            }
        }
    }

    public void ValidateAndMove(float x, float y) {
        if (Owner == null)
            return;

        var pos = new FPoint();
        ResolveNewLocation(x, y, pos);
        Move(pos.X, pos.Y);
    }

    private void ResolveNewLocation(float x, float y, FPoint pos) {
        if (HasConditionEffect(ConditionEffects.Paralyzed)) {
            pos.X = X;
            pos.Y = Y;
            return;
        }

        var dx = x - X;
        var dy = y - Y;

        const float colSkipBoundary = .4f;
        if (dx < colSkipBoundary &&
            dx > -colSkipBoundary &&
            dy < colSkipBoundary &&
            dy > -colSkipBoundary) {
            CalcNewLocation(x, y, pos);
            return;
        }

        var ds = colSkipBoundary / Math.Max(Math.Abs(dx), Math.Abs(dy));
        var tds = 0f;

        pos.X = X;
        pos.Y = Y;

        var done = false;
        while (!done) {
            if (tds + ds >= 1) {
                ds = 1 - tds;
                done = true;
            }

            CalcNewLocation(pos.X + dx * ds, pos.Y + dy * ds, pos);
            tds = tds + ds;
        }
    }

    private void CalcNewLocation(float x, float y, FPoint pos) {
        float fx = 0;
        float fy = 0;

        var isFarX = (X % .5f == 0 && x != X) || (int) (X / .5f) != (int) (x / .5f);
        var isFarY = (Y % .5f == 0 && y != Y) || (int) (Y / .5f) != (int) (y / .5f);

        if ((!isFarX && !isFarY) || RegionUnblocked(x, y)) {
            pos.X = x;
            pos.Y = y;
            return;
        }

        if (isFarX) {
            fx = x > X ? (int) (x * 2) / 2f : (int) (X * 2) / 2f;
            if ((int) fx > (int) X)
                fx = fx - 0.01f;
        }

        if (isFarY) {
            fy = y > Y ? (int) (y * 2) / 2f : (int) (Y * 2) / 2f;
            if ((int) fy > (int) Y)
                fy = fy - 0.01f;
        }

        if (!isFarX) {
            pos.X = x;
            pos.Y = fy;
            return;
        }

        if (!isFarY) {
            pos.X = fx;
            pos.Y = y;
            return;
        }

        var ax = x > X ? x - fx : fx - x;
        var ay = y > Y ? y - fy : fy - y;
        if (ax > ay) {
            if (RegionUnblocked(x, fy)) {
                pos.X = x;
                pos.Y = fy;
                return;
            }

            if (RegionUnblocked(fx, y)) {
                pos.X = fx;
                pos.Y = y;
                return;
            }
        }
        else {
            if (RegionUnblocked(fx, y)) {
                pos.X = fx;
                pos.Y = y;
                return;
            }

            if (RegionUnblocked(x, fy)) {
                pos.X = x;
                pos.Y = fy;
                return;
            }
        }

        pos.X = fx;
        pos.Y = fy;
    }

    private bool RegionUnblocked(float x, float y) {
        if (TileOccupied(x, y))
            return false;

        var xFrac = x - (int) x;
        var yFrac = y - (int) y;

        if (xFrac < 0.5) {
            if (TileFullOccupied(x - 1, y))
                return false;

            if (yFrac < 0.5) {
                if (TileFullOccupied(x, y - 1) || TileFullOccupied(x - 1, y - 1))
                    return false;
            }
            else {
                if (yFrac > 0.5)
                    if (TileFullOccupied(x, y + 1) || TileFullOccupied(x - 1, y + 1))
                        return false;
            }

            return true;
        }

        if (xFrac > 0.5) {
            if (TileFullOccupied(x + 1, y))
                return false;

            if (yFrac < 0.5) {
                if (TileFullOccupied(x, y - 1) || TileFullOccupied(x + 1, y - 1))
                    return false;
            }
            else {
                if (yFrac > 0.5)
                    if (TileFullOccupied(x, y + 1) || TileFullOccupied(x + 1, y + 1))
                        return false;
            }

            return true;
        }

        if (yFrac < 0.5) {
            if (TileFullOccupied(x, y - 1))
                return false;

            return true;
        }

        if (yFrac > 0.5)
            if (TileFullOccupied(x, y + 1))
                return false;

        return true;
    }

    public bool TileOccupied(float x, float y) {
        var x_ = (int) x;
        var y_ = (int) y;

        var map = Owner.Map;

        if (!map.Contains(x_, y_))
            return true;

        var tile = map[x_, y_];

        var tileDesc = Manager.Resources.GameData.Tiles[tile.TileId];
        if (tileDesc?.NoWalk == true)
            return true;

        if (tile.ObjectType != 0) {
            var objDesc = Manager.Resources.GameData.ObjectDescs[tile.ObjectType];
            if (objDesc?.EnemyOccupySquare == true)
                return true;
        }

        return false;
    }

    public bool TileFullOccupied(float x, float y) {
        var xx = (int) x;
        var yy = (int) y;

        if (!Owner.Map.Contains(xx, yy))
            return true;

        var tile = Owner.Map[xx, yy];

        if (tile.ObjectType != 0) {
            var objDesc = Manager.Resources.GameData.ObjectDescs[tile.ObjectType];
            if (objDesc?.FullOccupy == true)
                return true;
        }

        return false;
    }

    public virtual void Move(float x, float y) {
        if (Controller != null)
            return;

        MoveEntity(x, y);
    }

    public void MoveEntity(float x, float y) {
        if (Owner != null && !(this is Projectile) && !(this is Pet) &&
            (!(this is StaticObject) || (this as StaticObject).Hittestable))
            (this is Enemy || (this is StaticObject && !(this is Decoy))
                    ? Owner.EnemiesCollision
                    : Owner.PlayersCollision)
                .Move(this, x, y);
        X = x;
        Y = y;
    }

    public Position? TryGetHistory(long ticks) {
        if (_posHistory == null) return null;
        if (ticks > 255) return null;
        return _posHistory[(byte) (_posIdx - (byte) ticks)];
    }

    public static Entity Resolve(RealmManager manager, string name) {
        if (!manager.Resources.GameData.IdToObjectType.TryGetValue(name, out var id))
            return null;

        return Resolve(manager, id);
    }

    public static Entity Resolve(RealmManager manager, ushort id) {
        var node = manager.Resources.GameData.ObjectTypeToElement[id];
        var type = node.Element("Class").Value;
        switch (type) {
            case "Projectile":
                throw new Exception("Projectile should not instantiated using Entity.Resolve");
            case "Sign":
                return new Sign(manager, id);
            case "Wall":
            case "DoubleWall":
                return new Wall(manager, id, node);
            case "ConnectedWall":
            case "CaveWall":
                return new ConnectedObject(manager, id);
            case "GameObject":
            case "CharacterChanger":
            case "MoneyChanger":
            case "NameChanger":
                return new StaticObject(manager, id, StaticObject.GetHP(node), true, false, true);
            case "GuildRegister":
            case "GuildChronicle":
            case "GuildBoard":
                return new StaticObject(manager, id, null, false, false, false);
            case "Container":
                return new Container(manager, id);
            case "Player":
                throw new Exception("Player should not instantiated using Entity.Resolve");
            case "Character": //Other characters means enemy
                return new Enemy(manager, id);
            case "ArenaPortal":
            case "Portal":
                return new Portal(manager, id, null);
            case "GuildHallPortal":
                return new GuildHallPortal(manager, id, null);
            case "ClosedVaultChest":
                return new ClosedVaultChest(manager, id);
            case "ClosedVaultChestGold":
            case "ClosedGiftChest":
            case "VaultChest":
            case "Merchant":
                return new WorldMerchant(manager, id);
            case "GuildMerchant":
                return new GuildMerchant(manager, id);
            default:
                Log.Warn("Not supported type: {0}", type);
                return new Entity(manager, id);
        }
    }

    public Projectile CreateProjectile(ProjectileDesc desc, ushort container, int dmg, long time, Position pos,
        float angle) {
        var ret = new Projectile(Manager, desc) //Assume only one
        {
            ProjectileOwner = this,
            ProjectileId = projectileId++,
            Container = container,
            Damage = dmg,

            CreationTime = time,
            StartPos = pos,
            Angle = angle,

            X = pos.X,
            Y = pos.Y
        };
        if (_projectiles[ret.ProjectileId] != null)
            _projectiles[ret.ProjectileId].Destroy();
        _projectiles[ret.ProjectileId] = ret;
        return ret;
    }

    public virtual bool HitByProjectile(Projectile projectile, RealmTime time) {
        if (ObjectDesc == null)
            return true;
        return ObjectDesc.Enemy || ObjectDesc.Player;
    }

    private void ProcessConditionEffects(RealmTime time) {
        if (EffectDuration == null || !_tickingEffects) return;

        ConditionEffects newEffects = 0;
        _tickingEffects = false;
        for (var i = 0; i < EffectDuration.Length; i++)
            if (EffectDuration[i] > 0) {
                EffectDuration[i] -= time.ElapsedMsDelta;
                if (EffectDuration[i] > 0) {
                    newEffects |= (ConditionEffects) ((ulong) 1 << i);
                    _tickingEffects = true;
                }
                else {
                    EffectDuration[i] = 0;
                }
            }
            else if (EffectDuration[i] == -1) {
                newEffects |= (ConditionEffects) ((ulong) 1 << i);
            }

        ConditionEffects = newEffects;
    }

    public bool HasConditionEffect(ConditionEffects eff) {
        return (ConditionEffects & eff) != 0;
    }

    public void ApplyConditionEffect(params ConditionEffect[] effs) {
        foreach (var i in effs) {
            if (!ApplyCondition(i.Effect))
                continue;

            var eff = (int) i.Effect;

            EffectDuration[eff] = i.DurationMS;
            if (i.DurationMS != 0)
                ConditionEffects |= (ConditionEffects) ((ulong) 1 << eff);
        }

        _tickingEffects = true;
    }

    public void ApplyConditionEffect(ConditionEffectIndex effect, int durationMs = -1) {
        if (!ApplyCondition(effect))
            return;

        var eff = (int) effect;

        EffectDuration[eff] = durationMs;
        if (durationMs != 0)
            ConditionEffects |= (ConditionEffects) ((ulong) 1 << eff);

        _tickingEffects = true;
    }

    private bool ApplyCondition(ConditionEffectIndex effect) {
        if (effect == ConditionEffectIndex.Stunned &&
            HasConditionEffect(ConditionEffects.StunImmune))
            return false;

        if (effect == ConditionEffectIndex.Stasis &&
            HasConditionEffect(ConditionEffects.StasisImmune))
            return false;

        return true;
    }

    public void OnChatTextReceived(Player player, string text) {
        var state = CurrentState;
        while (state != null) {
            foreach (var t in state.Transitions.OfType<PlayerTextTransition>())
                t.OnChatReceived(player, text);
            state = state.Parent;
        }
    }

    public void InvokeStatChange(StatsType t, object val, bool updateSelfOnly = false) {
        StatChanged?.Invoke(this, new StatChangedEventArgs(t, val, updateSelfOnly));
    }

    public void SetDefaultSize(int size) {
        _originalSize = size;
        Size = size;
    }

    public void RestoreDefaultSize() {
        Size = _originalSize;
    }

    public int DamageWithDefense(int origDamage, int targetDefense, bool armorPiercing) {
        var def = (float) targetDefense;
        if (armorPiercing || HasConditionEffect(ConditionEffects.ArmorBroken))
            def = 0;
        else if (HasConditionEffect(ConditionEffects.Armored))
            def *= 2.0f;

        var min = origDamage * 0.25;
        var d = (int) Math.Max(min, origDamage - def);
        if (HasConditionEffect(ConditionEffects.Invulnerable) || HasConditionEffect(ConditionEffects.Invincible))
            d = 0;
        
        return d;
    }

    public virtual void Dispose() {
        Owner = null;
    }

    private class FPoint {
        public float X;
        public float Y;
    }
}