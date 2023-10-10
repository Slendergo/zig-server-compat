using Shared.resources;
using GameServer.realm.entities.player;

namespace GameServer.realm.entities;

internal class Trap : StaticObject {
    private const int LIFETIME = 10;
    private int dmg;
    private int duration;
    private ConditionEffectIndex effect;
    private int p;

    private Player player;
    private float radius;

    private int t;

    public Trap(Player player, float radius, int dmg, ConditionEffectIndex eff, float effDuration)
        : base(player.Manager, 0x0711, LIFETIME * 1000, true, true, false) {
        this.player = player;
        this.radius = radius;
        this.dmg = dmg;
        effect = eff;
        duration = (int) (effDuration * 1000);
    }

    public override void Tick(RealmTime time) {
        if (t / 500 == p) {
            foreach (var otherPlayer in Owner.Players.Values)
                if (otherPlayer.DistSqr(this) < Player.RADIUS_SQR)
                    otherPlayer.Client.SendShowEffect(EffectType.Trap, Id, new Position {X = radius / 2},
                        new Position(), new ARGB(0xff9000ff));

            p++;
            if (p == LIFETIME * 2) {
                Explode(time);
                return;
            }
        }

        t += time.ElapsedMsDelta;

        var monsterNearby = false;
        this.AOE(radius / 2, false, enemy => monsterNearby = true);
        if (monsterNearby)
            Explode(time);

        base.Tick(time);
    }

    private void Explode(RealmTime time) {
        foreach (var otherPlayer in Owner.Players.Values)
            if (otherPlayer.DistSqr(this) < Player.RADIUS_SQR)
                otherPlayer.Client.SendShowEffect(EffectType.AreaBlast, Id, new Position {X = radius}, new Position(),
                    new ARGB(0xff9000ff));

        this.AOE(radius, false, enemy => {
            (enemy as Enemy).Damage(player, time, dmg, false, new ConditionEffect {
                Effect = effect,
                DurationMS = duration
            });
        });
        Owner.LeaveWorld(this);
    }
}