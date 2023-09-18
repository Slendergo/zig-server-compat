using System.Xml.Linq;
using common;
using common.resources;
using wServer.realm;
using wServer.realm.entities;

namespace wServer.logic.behaviors;

internal class HealSelf : Behavior {
    private readonly int? _amount;
    //State storage: cooldown timer

    private Cooldown _coolDown;

    public HealSelf(XElement e) {
        _amount = e.ParseNInt("@amount");
        _coolDown = new Cooldown().Normalize(e.ParseInt("@cooldown", 1000));
    }

    public HealSelf(Cooldown coolDown = new(), int? amount = null) {
        _coolDown = coolDown.Normalize();
        _amount = amount;
    }

    protected override void OnStateEntry(Entity host, RealmTime time, ref object state) {
        state = 0;
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state) {
        var cool = (int) state;

        if (cool <= 0) {
            if (host.HasConditionEffect(ConditionEffects.Stunned))
                return;

            if (host is not Character entity)
                return;

            var newHp = entity.ObjectDesc.MaxHP;
            if (_amount != null) {
                var newHealth = (int) _amount + entity.HP;
                if (newHp > newHealth)
                    newHp = newHealth;
            }

            if (newHp != entity.HP) {
                var n = newHp - entity.HP;
                entity.HP = newHp;

                foreach (var player in host.Owner.Players.Values)
                    if (player.DistSqr(host) < Player.RadiusSqr) {
                        player.Client.SendShowEffect(EffectType.Potion, entity.Id, new Position(), new Position(),
                            new ARGB(0xffffffff));
                        player.Client.SendShowEffect(EffectType.Trail, host.Id,
                            new Position {X = entity.X, Y = entity.Y}, new Position(), new ARGB(0xffffffff));
                        player.Client.SendNotification(entity.Id, "+" + n, new ARGB(0xff00ff00));
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