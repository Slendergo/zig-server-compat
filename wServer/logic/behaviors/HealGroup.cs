using System.Xml.Linq;
using common;
using common.resources;
using wServer.realm;
using wServer.realm.entities;

namespace wServer.logic.behaviors;

internal class HealGroup : Behavior {
    private int? amount;
    private Cooldown coolDown;

    private string group;
    //State storage: cooldown timer

    private double range;

    public HealGroup(XElement e) {
        range = e.ParseFloat("@range");
        group = e.ParseString("@group");
        amount = e.ParseNInt("@amount");
        coolDown = new Cooldown().Normalize(e.ParseInt("@cooldown", 1000));
    }

    public HealGroup(double range, string group, Cooldown coolDown = new(), int? healAmount = null) {
        this.range = (float) range;
        this.group = group;
        this.coolDown = coolDown.Normalize();
        amount = healAmount;
    }

    protected override void OnStateEntry(Entity host, RealmTime time, ref object state) {
        state = 0;
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state) {
        var cool = (int) state;

        if (cool <= 0) {
            if (host.HasConditionEffect(ConditionEffects.Stunned)) return;

            foreach (var entity in host.GetNearestEntitiesByGroup(range, group).OfType<Enemy>()) {
                var newHp = entity.ObjectDesc.MaxHP;
                if (amount != null) {
                    var newHealth = (int) amount + entity.HP;
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

            cool = coolDown.Next(Random);
        }
        else {
            cool -= time.ElapsedMsDelta;
        }

        state = cool;
    }
}