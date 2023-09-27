using common.resources;
using wServer.logic;
using wServer.realm.worlds;

namespace wServer.realm.entities;

public class Enemy : Character {
    private readonly SV<int> defense;

    private float bleeding;

    public bool isPet; // TODO quick hack for backwards compatibility
    public Enemy ParentEntity;

    private Position? pos;
    private bool stat;

    public Enemy(RealmManager manager, ushort objType)
        : base(manager, objType) {
        stat = ObjectDesc.MaxHP == 0;
        DamageCounter = new DamageCounter(this);
        defense = new SV<int>(this, StatsType.Defense, ObjectDesc.Defense);
    }

    public int Defense {
        get => defense.GetValue();
        set => defense.SetValue(value);
    }

    public DamageCounter DamageCounter { get; }

    public TerrainType Terrain { get; set; }
    public Position SpawnPoint => pos ?? new Position {X = X, Y = Y};

    public override void Init(World owner) {
        base.Init(owner);
        if (ObjectDesc.StunImmune)
            ApplyConditionEffect(new ConditionEffect {
                Effect = ConditionEffectIndex.StunImmune,
                DurationMS = -1
            });
        if (ObjectDesc.StasisImmune)
            ApplyConditionEffect(new ConditionEffect {
                Effect = ConditionEffectIndex.StasisImmune,
                DurationMS = -1
            });
    }

    protected override void ExportStats(IDictionary<StatsType, object> stats) {
        stats[StatsType.Defense] = Defense;
        base.ExportStats(stats);
    }

    public event EventHandler<BehaviorEventArgs> OnDeath;

    public void Death(RealmTime time) {
        DamageCounter.Death(time);
        if (CurrentState != null)
            CurrentState.OnDeath(new BehaviorEventArgs(this, time));
        OnDeath?.Invoke(this, new BehaviorEventArgs(this, time));
        Owner.LeaveWorld(this);
    }

    public int Damage(Player from, RealmTime time, int dmg, bool noDef, params ConditionEffect[] effs) {
        if (stat) return 0;
        if (HasConditionEffect(ConditionEffects.Invincible))
            return 0;
        if (!HasConditionEffect(ConditionEffects.Paused) &&
            !HasConditionEffect(ConditionEffects.Stasis)) {
            dmg = DamageWithDefense(dmg, Defense, noDef);
            if (dmg > HP)
                dmg = HP;

            HP -= dmg;

            ApplyConditionEffect(effs);
            foreach (var player in Owner.Players.Values)
                if (player.Id != Id && player.DistSqr(this) < Player.RadiusSqr)
                    player.Client.SendDamage(Id, 0, (ushort)dmg, HP < 0);

            DamageCounter.HitBy(from, time, null, dmg);

            if (HP < 0 && Owner != null) Death(time);

            return dmg;
        }

        return 0;
    }

    public override bool HitByProjectile(Projectile projectile, RealmTime time) {
        if (stat)
            return false;
        if (HasConditionEffect(ConditionEffects.Invincible))
            return false;
        if (projectile.ProjectileOwner is Player &&
            !HasConditionEffect(ConditionEffects.Paused) &&
            !HasConditionEffect(ConditionEffects.Stasis)) {
            var dmg = DamageWithDefense(projectile.Damage, Defense, projectile.ProjDesc.ArmorPiercing);
            HP -= dmg;

            ApplyConditionEffect(projectile.ProjDesc.Effects);
            foreach (var player in Owner.Players.Values)
                if (player.Id != projectile.ProjectileOwner.Self.Id && player.DistSqr(this) < Player.RadiusSqr)
                    player.Client.SendDamage(Id, projectile.ConditionEffects, (ushort)dmg, HP < 0);// projectile.ProjectileId, projectile.ProjectileOwner.Self.Id);

            DamageCounter.HitBy(projectile.ProjectileOwner as Player, time, projectile, dmg);

            if (HP < 0 && Owner != null) Death(time);
            return true;
        }

        return false;
    }

    public override void Tick(RealmTime time) {
        if (pos == null)
            pos = new Position {X = X, Y = Y};

        if (!stat && HasConditionEffect(ConditionEffects.Bleeding)) {
            if (bleeding > 1) {
                HP -= (int) bleeding;
                bleeding -= (int) bleeding;
            }

            bleeding += 28 * (time.ElapsedMsDelta / 1000f);
        }

        base.Tick(time);
    }
}