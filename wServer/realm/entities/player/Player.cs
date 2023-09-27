using common;
using common.resources;
using NLog;
using wServer.logic;
using wServer.networking;
using wServer.realm.terrain;
using wServer.realm.worlds;
using wServer.realm.worlds.logic;

namespace wServer.realm.entities;

internal interface IPlayer {
    void Damage(int dmg, Entity src);
    bool IsVisibleToEnemy();
}

public partial class Player : Character, IContainer, IPlayer {
    private new static readonly Logger Log = LogManager.GetCurrentClassLogger();

    //Stats
    private readonly SV<int> _accountId;

    private readonly SV<int> _credits;

    private readonly SV<int> _currentFame;

    private readonly SV<int> _experience;

    private readonly SV<int> _experienceGoal;

    private readonly SV<int> _fame;

    private readonly SV<int> _fameGoal;

    private readonly SV<int> _glow;

    private readonly SV<string> _guild;

    private readonly SV<int> _guildRank;

    private readonly SV<bool> _hasBackpack;

    private readonly SV<int> _level;

    private readonly SV<int> _mp;

    private readonly SV<bool> _nameChosen;

    private readonly SV<int> _oxygenBar;
    private readonly SV<int> _skin;

    private readonly SV<int> _stars;

    private readonly SV<int> _texture1;

    private readonly SV<int> _texture2;

    public readonly StatsManager Stats;

    private bool _dead;

    private float _hpRegenCounter;
    private float _mpRegenCounter;

    private int _originalSkin;

    private byte[,] tiles;

    public Player(Client client, bool saveInventory = true)
        : base(client.Manager, client.Character.ObjectType) {
        var settings = Manager.Resources.Settings;
        var gameData = Manager.Resources.GameData;

        Client = client;

        // found in player.update partial
        Sight = new Sight(this);
        _clientEntities = new UpdatedSet(this);

        _accountId = new SV<int>(this, StatsType.AccountId, client.Account.AccountId, true);
        _experience = new SV<int>(this, StatsType.Experience, client.Character.Experience, true);
        _experienceGoal = new SV<int>(this, StatsType.ExperienceGoal, 0, true);
        _level = new SV<int>(this, StatsType.Level, client.Character.Level);
        _currentFame = new SV<int>(this, StatsType.CurrentFame, client.Account.Fame, true);
        _fame = new SV<int>(this, StatsType.Fame, client.Character.Fame, true);
        _fameGoal = new SV<int>(this, StatsType.FameGoal, 0, true);
        _stars = new SV<int>(this, StatsType.Stars, 0);
        _guild = new SV<string>(this, StatsType.Guild, "");
        _guildRank = new SV<int>(this, StatsType.GuildRank, -1);
        _credits = new SV<int>(this, StatsType.Credits, client.Account.Credits, true);
        _nameChosen = new SV<bool>(this, StatsType.NameChosen, client.Account.NameChosen, false,
            v => Client.Account?.NameChosen ?? v);
        _texture1 = new SV<int>(this, StatsType.Texture1, client.Character.Tex1);
        _texture2 = new SV<int>(this, StatsType.Texture2, client.Character.Tex2);
        _skin = new SV<int>(this, StatsType.Skin, 0);
        _glow = new SV<int>(this, StatsType.Glow, 0);
        _mp = new SV<int>(this, StatsType.MP, client.Character.MP);
        _hasBackpack = new SV<bool>(this, StatsType.HasBackpack, client.Character.HasBackpack, true);
        _oxygenBar = new SV<int>(this, StatsType.OxygenBar, -1, true);

        Name = client.Account.Name;
        HP = client.Character.HP;
        ConditionEffects = 0;

        var s = (ushort) client.Character.Skin;
        if (gameData.Skins.Keys.Contains(s)) {
            SetDefaultSkin(s);
            SetDefaultSize(gameData.Skins[s].Size);
        }

        var guild = Manager.Database.GetGuild(client.Account.GuildId);
        if (guild?.Name != null) {
            Guild = guild.Name;
            GuildRank = client.Account.GuildRank;
        }

        PetId = client.Character.PetId;

        HealthPots = new ItemStacker(this, 254, 0x0A22,
            client.Character.HealthStackCount, settings.MaxStackablePotions);
        MagicPots = new ItemStacker(this, 255, 0x0A23,
            client.Character.MagicStackCount, settings.MaxStackablePotions);
        Stacks = new[] {HealthPots, MagicPots};

        // inventory setup
        DbLink = new DbCharInv(Client.Account, Client.Character.CharId);
        Inventory = new Inventory(this,
            Utils.ResizeArray(
                (DbLink as DbCharInv).Items
                .Select(_ => _ == 0xffff || !gameData.Items.ContainsKey(_) ? null : gameData.Items[_])
                .ToArray(),
                20));
        if (!saveInventory)
            DbLink = null;

        Inventory.InventoryChanged += (sender, e) => Stats.ReCalculateValues(e);
        SlotTypes = Utils.ResizeArray(
            gameData.Classes[ObjectType].SlotTypes,
            20);
        Stats = new StatsManager(this);

        Manager.Database.IsMuted(client.IP)
            .ContinueWith(t => { Muted = !Client.Account.Admin && t.IsCompleted && t.Result; });

        Manager.Database.IsLegend(AccountId)
            .ContinueWith(t => { Glow = t.Result ? 1 : -1; });
    }

    public Client Client { get; }

    public int AccountId {
        get => _accountId.GetValue();
        set => _accountId.SetValue(value);
    }

    public int Experience {
        get => _experience.GetValue();
        set => _experience.SetValue(value);
    }

    public int ExperienceGoal {
        get => _experienceGoal.GetValue();
        set => _experienceGoal.SetValue(value);
    }

    public int Level {
        get => _level.GetValue();
        set => _level.SetValue(value);
    }

    public int CurrentFame {
        get => _currentFame.GetValue();
        set => _currentFame.SetValue(value);
    }

    public int Fame {
        get => _fame.GetValue();
        set => _fame.SetValue(value);
    }

    public int FameGoal {
        get => _fameGoal.GetValue();
        set => _fameGoal.SetValue(value);
    }

    public int Stars {
        get => _stars.GetValue();
        set => _stars.SetValue(value);
    }

    public string Guild {
        get => _guild.GetValue();
        set => _guild.SetValue(value);
    }

    public int GuildRank {
        get => _guildRank.GetValue();
        set => _guildRank.SetValue(value);
    }

    public int Credits {
        get => _credits.GetValue();
        set => _credits.SetValue(value);
    }

    public bool NameChosen {
        get => _nameChosen.GetValue();
        set => _nameChosen.SetValue(value);
    }

    public int Texture1 {
        get => _texture1.GetValue();
        set => _texture1.SetValue(value);
    }

    public int Texture2 {
        get => _texture2.GetValue();
        set => _texture2.SetValue(value);
    }

    public int Skin {
        get => _skin.GetValue();
        set => _skin.SetValue(value);
    }

    public int Glow {
        get => _glow.GetValue();
        set => _glow.SetValue(value);
    }

    public int MP {
        get => _mp.GetValue();
        set => _mp.SetValue(value);
    }

    public bool HasBackpack {
        get => _hasBackpack.GetValue();
        set => _hasBackpack.SetValue(value);
    }

    public int OxygenBar {
        get => _oxygenBar.GetValue();
        set => _oxygenBar.SetValue(value);
    }

    public int PetId { get; set; }
    public Pet Pet { get; set; }
    public int? GuildInvite { get; set; }
    public bool Muted { get; set; }

    public ItemStacker HealthPots { get; }
    public ItemStacker MagicPots { get; }
    public ItemStacker[] Stacks { get; }
    public FameCounter FameCounter { get; private set; }

    public RInventory DbLink { get; }
    public int[] SlotTypes { get; }
    public Inventory Inventory { get; }

    public void Damage(int dmg, Entity src) {
        if (IsInvulnerable())
            return;

        dmg = DamageWithDefense(dmg, Stats[3], false);

        HP -= dmg;
        foreach (var player in Owner.Players.Values)
            if (player.Id != Id && player.DistSqr(this) < RadiusSqr)
                player.Client.SendDamage(Id, 0, (ushort)dmg, HP < 0);

        if (HP <= 0)
            Death(src.ObjectDesc.DisplayId ??
                  src.ObjectDesc.ObjectId,
                src);
    }

    protected override void ExportStats(IDictionary<StatsType, object> stats) {
        base.ExportStats(stats);
        stats[StatsType.AccountId] = AccountId;
        stats[StatsType.Experience] = Experience - GetLevelExp(Level);
        stats[StatsType.ExperienceGoal] = ExperienceGoal;
        stats[StatsType.Level] = Level;
        stats[StatsType.CurrentFame] = CurrentFame;
        stats[StatsType.Fame] = Fame;
        stats[StatsType.FameGoal] = FameGoal;
        stats[StatsType.Stars] = Stars;
        stats[StatsType.Guild] = Guild;
        stats[StatsType.GuildRank] = GuildRank;
        stats[StatsType.Credits] = Credits;
        stats[StatsType.NameChosen] = Client.Account?.NameChosen ?? NameChosen;
        stats[StatsType.Texture1] = Texture1;
        stats[StatsType.Texture2] = Texture2;
        stats[StatsType.Skin] = Skin;
        stats[StatsType.Glow] = Glow;
        stats[StatsType.MP] = MP;
        stats[StatsType.Inventory0] = Inventory[0]?.ObjectType ?? -1;
        stats[StatsType.Inventory1] = Inventory[1]?.ObjectType ?? -1;
        stats[StatsType.Inventory2] = Inventory[2]?.ObjectType ?? -1;
        stats[StatsType.Inventory3] = Inventory[3]?.ObjectType ?? -1;
        stats[StatsType.Inventory4] = Inventory[4]?.ObjectType ?? -1;
        stats[StatsType.Inventory5] = Inventory[5]?.ObjectType ?? -1;
        stats[StatsType.Inventory6] = Inventory[6]?.ObjectType ?? -1;
        stats[StatsType.Inventory7] = Inventory[7]?.ObjectType ?? -1;
        stats[StatsType.Inventory8] = Inventory[8]?.ObjectType ?? -1;
        stats[StatsType.Inventory9] = Inventory[9]?.ObjectType ?? -1;
        stats[StatsType.Inventory10] = Inventory[10]?.ObjectType ?? -1;
        stats[StatsType.Inventory11] = Inventory[11]?.ObjectType ?? -1;
        stats[StatsType.BackPack0] = Inventory[12]?.ObjectType ?? -1;
        stats[StatsType.BackPack1] = Inventory[13]?.ObjectType ?? -1;
        stats[StatsType.BackPack2] = Inventory[14]?.ObjectType ?? -1;
        stats[StatsType.BackPack3] = Inventory[15]?.ObjectType ?? -1;
        stats[StatsType.BackPack4] = Inventory[16]?.ObjectType ?? -1;
        stats[StatsType.BackPack5] = Inventory[17]?.ObjectType ?? -1;
        stats[StatsType.BackPack6] = Inventory[18]?.ObjectType ?? -1;
        stats[StatsType.BackPack7] = Inventory[19]?.ObjectType ?? -1;
        stats[StatsType.MaximumHP] = Stats[0];
        stats[StatsType.MaximumMP] = Stats[1];
        stats[StatsType.Attack] = Stats[2];
        stats[StatsType.Defense] = Stats[3];
        stats[StatsType.Speed] = Stats[4];
        stats[StatsType.Dexterity] = Stats[5];
        stats[StatsType.Vitality] = Stats[6];
        stats[StatsType.Wisdom] = Stats[7];
        stats[StatsType.HPBoost] = Stats.Boost[0];
        stats[StatsType.MPBoost] = Stats.Boost[1];
        stats[StatsType.AttackBonus] = Stats.Boost[2];
        stats[StatsType.DefenseBonus] = Stats.Boost[3];
        stats[StatsType.SpeedBonus] = Stats.Boost[4];
        stats[StatsType.DexterityBonus] = Stats.Boost[5];
        stats[StatsType.VitalityBonus] = Stats.Boost[6];
        stats[StatsType.WisdomBonus] = Stats.Boost[7];
        stats[StatsType.HealthStackCount] = HealthPots.Count;
        stats[StatsType.MagicStackCount] = MagicPots.Count;
        stats[StatsType.HasBackpack] = HasBackpack;
        stats[StatsType.OxygenBar] = OxygenBar;
    }

    public void SaveToCharacter() {
        var chr = Client.Character;
        chr.Level = Level;
        chr.Experience = Experience;
        chr.Fame = Fame;
        chr.HP = Math.Max(1, HP);
        chr.MP = MP;
        chr.Stats = Stats.Base.GetStats();
        chr.Tex1 = Texture1;
        chr.Tex2 = Texture2;
        chr.Skin = _originalSkin;
        chr.FameStats = FameCounter.Stats.Write();
        chr.LastSeen = DateTime.Now;
        chr.HealthStackCount = HealthPots.Count;
        chr.MagicStackCount = MagicPots.Count;
        chr.HasBackpack = HasBackpack;
        chr.PetId = PetId;
        chr.Items = Inventory.GetItemTypes();
    }

    public override void Init(World owner) {
        var x = 0;
        var y = 0;
        var spawnRegions = owner.GetSpawnPoints();
        if (spawnRegions.Any()) {
            var rand = new Random();
            var sRegion = spawnRegions.ElementAt(rand.Next(0, spawnRegions.Length));
            x = sRegion.Key.X;
            y = sRegion.Key.Y;
        }

        Move(x + 0.5f, y + 0.5f);
        tiles = new byte[owner.Map.Width, owner.Map.Height];

        // spawn pet if player has one attached
        SpawnPetIfAttached(owner);

        FameCounter = new FameCounter(this);
        FameGoal = GetFameGoal(FameCounter.ClassStats[ObjectType].BestFame);
        ExperienceGoal = GetExpGoal(Client.Character.Level);
        Stars = GetStars();

        if (owner.IdName.Equals("OceanTrench"))
            OxygenBar = 100;

        SetNewbiePeriod();

        base.Init(owner);
    }

    private void SpawnPetIfAttached(World owner) {
        // despawn old pet if found
        Pet?.Owner?.LeaveWorld(Pet);

        // create new pet
        var petId = PetId;
        if (petId != 0) {
            var pet = new Pet(Manager, this, (ushort) petId);
            pet.Move(X, Y);
            owner.EnterWorld(pet);
            pet.SetDefaultSize(pet.ObjectDesc.Size);
            Pet = pet;
        }
    }

    public override void Tick(RealmTime time) {
        if (!KeepAlive(time))
            return;

        CheckTradeTimeout(time);
        HandleQuest(time);

        if (!HasConditionEffect(ConditionEffects.Paused)) {
            HandleRegen(time);
            HandleEffects(time);
            HandleOceanTrenchGround(time);
            FameCounter.Tick(time);

            // TODO, server side ground damage
            //if (HandleGround(time))
            //    return; // death resulted
        }

        base.Tick(time);

        SendUpdate(time);
        SendNewTick(time);

        if (HP <= 0) Death("Unknown");
    }

    private void HandleRegen(RealmTime time) {
        // hp regen
        if (HP == Stats[0] || !CanHpRegen()) {
            _hpRegenCounter = 0;
        }
        else {
            _hpRegenCounter += Stats.GetHPRegen() * time.ElapsedMsDelta / 1000f;
            var regen = (int) _hpRegenCounter;
            if (regen > 0) {
                HP = Math.Min(Stats[0], HP + regen);
                _hpRegenCounter -= regen;
            }
        }

        // mp regen
        if (MP == Stats[1] || !CanMpRegen()) {
            _mpRegenCounter = 0;
        }
        else {
            _mpRegenCounter += Stats.GetMPRegen() * time.ElapsedMsDelta / 1000f;
            var regen = (int) _mpRegenCounter;
            if (regen > 0) {
                MP = Math.Min(Stats[1], MP + regen);
                _mpRegenCounter -= regen;
            }
        }
    }

    public void TeleportPosition(RealmTime time, float x, float y, bool ignoreRestrictions = false) {
        TeleportPosition(time, new Position {X = x, Y = y}, ignoreRestrictions);
    }

    public void TeleportPosition(RealmTime time, Position position, bool ignoreRestrictions = false) {
        if (!ignoreRestrictions) {
            if (!TPCooledDown()) {
                SendErrorText("Too soon to teleport again!");
                return;
            }

            SetTPDisabledPeriod();
            SetNewbiePeriod();
            FameCounter.Teleport();
        }

        HandleQuest(time, true, position);

        foreach (var player in Owner.Players.Values) {
            player.Client.SendGoto(Id, position.X, position.Y);
            player.AwaitGotoAck(time.TotalElapsedMs);
            player.Client.SendShowEffect(EffectType.Teleport, Id, position, new Position(), new ARGB(0xFFFFFFFF));
        }
    }

    public void Teleport(RealmTime time, int objId, bool ignoreRestrictions = false) {
        var obj = Owner.GetEntity(objId);
        if (obj == null) {
            SendErrorText("Target does not exist.");
            return;
        }

        if (!ignoreRestrictions) {
            if (Id == objId) {
                SendInfo("You are already at yourself, and always will be!");
                return;
            }

            if (!Owner.AllowTeleport) {
                SendErrorText("Cannot teleport here.");
                return;
            }

            if (HasConditionEffect(ConditionEffects.Paused)) {
                SendErrorText("Cannot teleport while paused.");
                return;
            }

            if (!(obj is Player)) {
                SendErrorText("Can only teleport to players.");
                return;
            }

            if (obj.HasConditionEffect(ConditionEffects.Invisible)) {
                SendErrorText("Cannot teleport to an invisible player.");
                return;
            }

            if (obj.HasConditionEffect(ConditionEffects.Paused)) {
                SendErrorText("Cannot teleport to a paused player.");
                return;
            }
        }

        TeleportPosition(time, obj.X, obj.Y, ignoreRestrictions);
    }

    public bool IsInvulnerable() {
        if (HasConditionEffect(ConditionEffects.Paused) ||
            HasConditionEffect(ConditionEffects.Stasis) ||
            HasConditionEffect(ConditionEffects.Invincible) ||
            HasConditionEffect(ConditionEffects.Invulnerable))
            return true;
        return false;
    }

    public override bool HitByProjectile(Projectile projectile, RealmTime time) {
        if (projectile.ProjectileOwner is Player ||
            IsInvulnerable())
            return false;

        var dmg = DamageWithDefense(projectile.Damage, Stats[3], projectile.ProjDesc.ArmorPiercing);
        HP -= dmg;

        ApplyConditionEffect(projectile.ProjDesc.Effects);
        foreach (var player in Owner.Players.Values)
            if (player.Id != Id && player.DistSqr(this) < RadiusSqr)
                player.Client.SendDamage(Id, projectile.ConditionEffects, (ushort) dmg, HP <= 0);

        if (HP <= 0)
            Death(projectile.ProjectileOwner.Self.ObjectDesc.DisplayId ??
                  projectile.ProjectileOwner.Self.ObjectDesc.ObjectId,
                projectile.ProjectileOwner.Self);

        return base.HitByProjectile(projectile, time);
    }

    private void GenerateGravestone(bool phantomDeath = false) {
        var playerDesc = Manager.Resources.GameData.Classes[ObjectType];
        var maxed = playerDesc.Stats.Where((t, i) => Stats.Base[i] >= t.MaxValue).Count();
        ushort objType;
        int time;
        switch (maxed) {
            case 8:
                objType = 0x0735;
                time = 600000;
                break;
            case 7:
                objType = 0x0734;
                time = 600000;
                break;
            case 6:
                objType = 0x072b;
                time = 600000;
                break;
            case 5:
                objType = 0x072a;
                time = 600000;
                break;
            case 4:
                objType = 0x0729;
                time = 600000;
                break;
            case 3:
                objType = 0x0728;
                time = 600000;
                break;
            case 2:
                objType = 0x0727;
                time = 600000;
                break;
            case 1:
                objType = 0x0726;
                time = 600000;
                break;
            default:
                objType = 0x0725;
                time = 300000;
                if (Level < 20) {
                    objType = 0x0724;
                    time = 60000;
                }

                if (Level <= 1) {
                    objType = 0x0723;
                    time = 30000;
                }

                break;
        }

        var obj = new StaticObject(Manager, objType, time, true, true, false);
        obj.Move(X, Y);
        obj.Name = !phantomDeath ? Name : $"{Name} got rekt";
        Owner.EnterWorld(obj);
    }

    private bool TestWorld(string killer) {
        if (!(Owner is Test))
            return false;

        GenerateGravestone();
        ReconnectToNexus();
        return true;
    }

    private bool Resurrection() {
        for (var i = 0; i < 4; i++) {
            var item = Inventory[i];

            if (item == null || !item.Resurrects)
                continue;

            Inventory[i] = null;
            foreach (var player in Owner.Players.Values)
                player.SendInfo($"{Name}'s {item.DisplayName} breaks and he disappears");

            ReconnectToNexus();
            return true;
        }

        return false;
    }

    private void ReconnectToNexus() {
        HP = 1;
        Client.Reconnect("Nexus", World.Nexus);
    }

    private void AnnounceDeath(string killer) {
        var playerDesc = Manager.Resources.GameData.Classes[ObjectType];
        var maxed = playerDesc.Stats.Where((t, i) => Stats.Base[i] >= t.MaxValue).Count();
        var deathMessage = string.Format(
            "{0} died at level {1}, killed by {2}",
            Name, Level, killer);

        foreach (var i in Owner.Players.Values) i.SendInfo(deathMessage);
    }

    public void Death(string killer, Entity entity = null, WmapTile tile = null) {
        if (_dead)
            return;

        _dead = true;

        if (TestWorld(killer))
            return;
        if (Resurrection())
            return;

        SaveToCharacter();
        Manager.Database.Death(Manager.Resources.GameData, Client.Account,
            Client.Character, FameCounter.Stats, killer);

        GenerateGravestone();
        AnnounceDeath(killer);

        Client.SendDeath(AccountId, Client.Character.CharId, killer);

        Owner.Timers.Add(new WorldTimer(1000, (w, t) => {
            if (Client.Player != this)
                return;

            Client.Disconnect();
        }));
    }

    public void Reconnect(object portal, World world) {
        ((Portal) portal).WorldInstanceSet -= Reconnect;

        if (world == null)
            SendErrorText("Portal Not Implemented!");
        else
            Client.Reconnect(world.IdName, world.Id);
    }

    public int GetCurrency(CurrencyType currency) {
        switch (currency) {
            case CurrencyType.Gold:
                return Credits;
            case CurrencyType.Fame:
                return CurrentFame;
            default:
                return 0;
        }
    }

    public void SetCurrency(CurrencyType currency, int amount) {
        switch (currency) {
            case CurrencyType.Gold:
                Credits = amount;
                break;
            case CurrencyType.Fame:
                CurrentFame = amount;
                break;
        }
    }

    public override void Move(float x, float y) {
        base.Move(x, y);

        if ((int) X != Sight.LastX || (int) Y != Sight.LastY) {
            if (IsNoClipping()) Client.Manager.Logic.AddPendingAction(t => Client.Disconnect());

            Sight.UpdateCount++;
        }
    }

    public override void Dispose() {
        base.Dispose();
        _clientEntities.Dispose();
    }

    public void SetDefaultSkin(int skin) {
        _originalSkin = skin;
        Skin = skin;
    }

    public void RestoreDefaultSkin() {
        Skin = _originalSkin;
    }
}