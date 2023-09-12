using common.resources;
using System.Net.NetworkInformation;
using wServer.logic;
using wServer.networking.packets.outgoing;
using wServer.realm.worlds;

namespace wServer.realm.entities;

public class Enemy : Character
{
    public bool isPet; // TODO quick hack for backwards compatibility
    bool stat;
    public Enemy ParentEntity;

    private readonly SV<int> defense;

    public int Defense
    {
        get => defense.GetValue(); 
        set => defense.SetValue(value);
    }

    DamageCounter counter;
    public Enemy(RealmManager manager, ushort objType)
        : base(manager, objType)
    {
        stat = ObjectDesc.MaxHP == 0;
        counter = new DamageCounter(this);
        defense = new SV<int>(this, StatsType.Defense, ObjectDesc.Defense);
    }

    public DamageCounter DamageCounter { get { return counter; } }

    public TerrainType Terrain { get; set; }

    Position? pos;
    public Position SpawnPoint { get { return pos ?? new Position() { X = X, Y = Y }; } }

    public override void Init(World owner)
    {
        base.Init(owner);
        if (ObjectDesc.StunImmune)
            ApplyConditionEffect(new ConditionEffect()
            {
                Effect = ConditionEffectIndex.StunImmune,
                DurationMS = -1
            });
        if (ObjectDesc.StasisImmune)
            ApplyConditionEffect(new ConditionEffect()
            {
                Effect = ConditionEffectIndex.StasisImmune,
                DurationMS = -1
            });
    }

    protected override void ExportStats(IDictionary<StatsType, object> stats)
    {
        stats[StatsType.Defense] = Defense;
        base.ExportStats(stats);
    }

    public event EventHandler<BehaviorEventArgs> OnDeath;

    public void Death(RealmTime time)
    {
        counter.Death(time);
        if (CurrentState != null)
            CurrentState.OnDeath(new BehaviorEventArgs(this, time));
        OnDeath?.Invoke(this, new BehaviorEventArgs(this, time));
        Owner.LeaveWorld(this);
    }

    public int Damage(Player from, RealmTime time, int dmg, bool noDef, params ConditionEffect[] effs)
    {
        if (stat) return 0;
        if (HasConditionEffect(ConditionEffects.Invincible))
            return 0;
        if (!HasConditionEffect(ConditionEffects.Paused) &&
            !HasConditionEffect(ConditionEffects.Stasis))
        {
            dmg = DamageWithDefense(dmg, Defense, noDef);
            if (dmg > HP)
                dmg = HP;

            HP -= dmg;

            ApplyConditionEffect(effs);
            Owner.BroadcastPacketNearby(new Damage()
            {
                TargetId = this.Id,
                Effects = 0,
                DamageAmount = (ushort)dmg,
                Kill = HP < 0,
                BulletId = 0,
                ObjectId = from.Id
            }, this, null);

            counter.HitBy(from, time, null, dmg);

            if (HP < 0 && Owner != null)
            {
                Death(time);
            }

            return dmg;
        }
        return 0;
    }
    public override bool HitByProjectile(Projectile projectile, RealmTime time)
    {
        if (stat) return false;
        if (HasConditionEffect(ConditionEffects.Invincible))
            return false;
        if (projectile.ProjectileOwner is Player &&
            !HasConditionEffect(ConditionEffects.Paused) &&
            !HasConditionEffect(ConditionEffects.Stasis))
        {
            var dmg = DamageWithDefense(projectile.Damage, Defense, projectile.ProjDesc.ArmorPiercing);
            HP -= dmg;

            Console.WriteLine(dmg);

            ApplyConditionEffect(projectile.ProjDesc.Effects);
            Owner.BroadcastPacketNearby(new Damage()
            {
                TargetId = this.Id,
                Effects = projectile.ConditionEffects,
                DamageAmount = (ushort)dmg,
                Kill = HP < 0,
                BulletId = projectile.ProjectileId,
                ObjectId = projectile.ProjectileOwner.Self.Id
            }, this, (projectile.ProjectileOwner as Player));

            counter.HitBy(projectile.ProjectileOwner as Player, time, projectile, dmg);

            if (HP < 0 && Owner != null)
            {
                Death(time);
            }
            return true;
        }
        return false;
    }

    float bleeding = 0;
    public override void Tick(RealmTime time)
    {
        if (pos == null)
            pos = new Position() { X = X, Y = Y };

        if (!stat && HasConditionEffect(ConditionEffects.Bleeding))
        {
            if (bleeding > 1)
            {
                HP -= (int)bleeding;
                bleeding -= (int)bleeding;
            }
            bleeding += 28 * (time.ElaspedMsDelta / 1000f);
        }
        base.Tick(time);
    }
}