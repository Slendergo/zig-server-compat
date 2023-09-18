using System.Xml.Linq;
using common;
using common.resources;
using wServer.realm;
using wServer.realm.entities;

namespace wServer.logic.behaviors;

internal class HealPlayer : Behavior {
    private Cooldown _coolDown;
    private int _healAmount;
    private double _range;

    public HealPlayer(XElement e) {
        _range = e.ParseFloat("@range");
        _healAmount = e.ParseInt("@amount", 100);
        _coolDown = new Cooldown().Normalize(e.ParseInt("@cooldown", 1000));
    }

    public HealPlayer(double range, Cooldown coolDown = new(), int healAmount = 100) {
        _range = range;
        _coolDown = coolDown.Normalize();
        _healAmount = healAmount;
    }

    protected override void OnStateEntry(Entity host, RealmTime time, ref object state) {
        state = 0;
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state) {
        var cool = (int) state;

        if (cool <= 0) {
            foreach (var entity in host.GetNearestEntities(_range, null, true).OfType<Player>()) {
                if (entity.Owner == null)
                    continue;

                if ((host.AttackTarget != null && host.AttackTarget != entity) ||
                    entity.HasConditionEffect(ConditionEffects.Sick))
                    continue;
                var maxHp = entity.Stats[0];
                var newHp = Math.Min(entity.HP + _healAmount, maxHp);

                if (newHp != entity.HP) {
                    var n = newHp - entity.HP;
                    entity.HP = newHp;

                    foreach (var player in entity.Owner.Players.Values)
                        if (player.DistSqr(host) < Player.RadiusSqr) {
                            player.Client.SendShowEffect(EffectType.Potion, entity.Id, new Position(), new Position(),
                                new ARGB(0xffffffff));
                            player.Client.SendShowEffect(EffectType.Trail, host.Id,
                                new Position {X = entity.X, Y = entity.Y}, new Position(), new ARGB(0xffffffff));
                            player.Client.SendNotification(entity.Id, "+" + n, new ARGB(0xff00ff00));
                        }
                }
            }

            cool = _coolDown.Next(Random);
        }
        else {
            cool -= time.ElapsedMsDelta;
        }

        state = cool;
    }
}