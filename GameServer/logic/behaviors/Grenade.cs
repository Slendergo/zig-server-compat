using System.Xml.Linq;
using Shared;
using Shared.resources;
using GameServer.realm;
using GameServer.realm.entities;
using GameServer.realm.entities.player;

namespace GameServer.logic.behaviors;

internal class Grenade : Behavior {
    private uint color;
    private Cooldown coolDown;
    private int damage;
    private ConditionEffectIndex effect;
    private int effectDuration;
    private double? fixedAngle;

    private float radius;
    //State storage: cooldown timer

    private double range;

    public Grenade(XElement e) {
        radius = e.ParseFloat("@radius");
        damage = e.ParseInt("@damage");
        range = e.ParseInt("@range", 5);
        fixedAngle = (float?) (e.ParseNFloat("@fixedAngle") * Math.PI / 180);
        coolDown = new Cooldown().Normalize(e.ParseInt("@cooldown", 1000));
        effect = e.ParseConditionEffect("@effect");
        effectDuration = e.ParseInt("@effectDuration");
        color = e.ParseUInt("@color", true, 0xffff0000);
    }

    public Grenade(double radius, int damage, double range = 5,
        double? fixedAngle = null, Cooldown coolDown = new(), ConditionEffectIndex effect = 0, int effectDuration = 0,
        uint color = 0xffff0000) {
        this.radius = (float) radius;
        this.damage = damage;
        this.range = range;
        this.fixedAngle = fixedAngle * Math.PI / 180;
        this.coolDown = coolDown.Normalize();
        this.effect = effect;
        this.effectDuration = effectDuration;
        this.color = color;
    }

    protected override void OnStateEntry(Entity host, RealmTime time, ref object state) {
        state = 0;
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state) {
        var cool = (int) state;

        if (cool <= 0) {
            if (host.HasConditionEffect(ConditionEffects.Stunned))
                return;

            var player = host.AttackTarget ?? host.GetNearestEntity(range, true);
            if (player != null || fixedAngle != null) {
                Position target;
                if (fixedAngle != null)
                    target = new Position {
                        X = (float) (range * Math.Cos(fixedAngle.Value)) + host.X,
                        Y = (float) (range * Math.Sin(fixedAngle.Value)) + host.Y
                    };
                else
                    target = new Position {
                        X = player.X,
                        Y = player.Y
                    };
                
                foreach (var otherPlayer in host.Owner.Players.Values)
                    if (otherPlayer.DistSqr(host) < Player.RadiusSqr)
                        otherPlayer.Client.SendShowEffect(EffectType.Throw, host.Id, target, new Position(),
                            new ARGB(color));

                host.Owner.Timers.Add(new WorldTimer(1500, (world, t) => {
                    foreach (var otherPlayer in host.Owner.Players.Values)
                        if (otherPlayer.DistSqr(host) < Player.RadiusSqr) 
                            otherPlayer.Client.SendAoe(target, radius, (ushort) damage, 0, 0, host.ObjectType);

                    world.AOE(target, radius, true, p => {
                        (p as IPlayer).Damage(damage, host);
                        if (!p.HasConditionEffect(ConditionEffects.Invincible) &&
                            !p.HasConditionEffect(ConditionEffects.Stasis))
                            p.ApplyConditionEffect(effect, effectDuration);
                    });
                }));
            }

            cool = coolDown.Next(Random);
        }
        else {
            cool -= time.ElapsedMsDelta;
        }

        state = cool;
    }
}