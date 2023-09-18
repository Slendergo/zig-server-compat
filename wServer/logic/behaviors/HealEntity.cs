using System.Xml.Linq;
using common;
using common.resources;
using wServer.realm;
using wServer.realm.entities;

namespace wServer.logic.behaviors;

internal class HealEntity : Behavior {
    private readonly int? _amount;

    private readonly string _name;
    //State storage: cooldown timer

    private readonly double _range;
    private Cooldown _coolDown;

    public HealEntity(XElement e) {
        _range = e.ParseFloat("@range");
        _name = e.ParseString("@name");
        _amount = e.ParseNInt("@amount");
        _coolDown = new Cooldown().Normalize(e.ParseInt("@cooldown", 1000));
    }

    public HealEntity(double range, string name = null, int? healAmount = null, Cooldown coolDown = new()) {
        _range = (float) range;
        _name = name;
        _coolDown = coolDown.Normalize();
        _amount = healAmount;
    }

    protected override void OnStateEntry(Entity host, RealmTime time, ref object state) {
        state = 0;
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state) {
        var cool = (int) state;

        if (cool <= 0) {
            if (host.HasConditionEffect(ConditionEffects.Stunned)) return;

            foreach (var entity in host.GetNearestEntitiesByName(_range, _name).OfType<Enemy>()) {
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
            }

            cool = _coolDown.Next(Random);
        }
        else {
            cool -= time.ElapsedMsDelta;
        }

        state = cool;
    }
}