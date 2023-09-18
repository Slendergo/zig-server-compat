using System.Xml.Linq;
using common;

namespace wServer.realm.entities;

public class StaticObject : Entity {
    private readonly SV<int> _hp;

    private readonly SV<int> defense;

    public StaticObject(RealmManager manager, ushort objType, int? life, bool stat, bool dying, bool hittestable)
        : base(manager, objType) {
        _hp = new SV<int>(this, StatsType.HP, 0, dying);
        defense = new SV<int>(this, StatsType.Defense, ObjectDesc.Defense);
        if (Vulnerable = life.HasValue)
            HP = life.Value;
        Dying = dying;
        Static = stat;
        Hittestable = hittestable;
    }

    //Stats
    public bool Vulnerable { get; }
    public bool Static { get; private set; }
    public bool Hittestable { get; private set; }
    public bool Dying { get; }

    public int HP {
        get => _hp.GetValue();
        set => _hp.SetValue(value);
    }

    public int Defense {
        get => defense.GetValue();
        set => defense.SetValue(value);
    }

    public static int? GetHP(XElement elem) {
        var n = elem.Element("MaxHitPoints");
        if (n != null)
            return Utils.GetInt(n.Value);
        return null;
    }

    protected override void ExportStats(IDictionary<StatsType, object> stats) {
        stats[StatsType.HP] = !Vulnerable ? int.MaxValue : HP;
        stats[StatsType.Defense] = Defense;
        base.ExportStats(stats);
    }

    public override bool HitByProjectile(Projectile projectile, RealmTime time) {
        if (!Vulnerable || projectile.ProjectileOwner is not Player)
            return true;

        var dmg = DamageWithDefense(projectile.Damage, Defense, projectile.ProjDesc.ArmorPiercing);
        HP -= dmg;

        foreach (var player in Owner.Players.Values)
            if (player.Id != projectile.ProjectileOwner.Self.Id && player.DistSqr(this) < Player.RadiusSqr)
                player.Client.SendDamage(Id, 0, (ushort) dmg, !CheckHP(), projectile.ProjectileId,
                    projectile.ProjectileOwner.Self.Id);

        return true;
    }

    protected bool CheckHP() {
        if (HP <= 0) {
            var x = (int) (X - 0.5);
            var y = (int) (Y - 0.5);
            if (Owner?.Map?.Contains(x, y) ?? false) {
                var tile = Owner.Map[x, y];
                tile.ObjectType = 0;
                tile.UpdateCount++;
            }

            Owner?.LeaveWorld(this);
            return false;
        }

        return true;
    }

    public override void Tick(RealmTime time) {
        if (Vulnerable) {
            if (Dying)
                HP -= time.ElapsedMsDelta;

            _ = CheckHP();
        }

        base.Tick(time);
    }
}