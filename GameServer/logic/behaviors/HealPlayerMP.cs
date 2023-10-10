using System.Xml.Linq;
using Shared;
using Shared.resources;
using GameServer.realm;
using GameServer.realm.entities;
using GameServer.realm.entities.player;

namespace GameServer.logic.behaviors;

internal class HealPlayerMP : Behavior {
    private Cooldown _coolDown;
    private int _healAmount;
    private double _range;

    public HealPlayerMP(XElement e) {
        _range = e.ParseFloat("@range");
        _healAmount = e.ParseInt("@amount");
        _coolDown = new Cooldown().Normalize(e.ParseInt("@cooldown", 1000));
    }

    public HealPlayerMP(double range, Cooldown coolDown = new(), int healAmount = 100) {
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
                if (host.AttackTarget != null && host.AttackTarget != entity ||
                    entity.HasConditionEffect(ConditionEffects.Quiet))
                    continue;
                var maxMp = entity.Stats[1];
                var newMp = Math.Min(entity.MP + _healAmount, maxMp);

                if (newMp != entity.MP) {
                    var n = newMp - entity.MP;
                    entity.MP = newMp;

                    foreach (var player in host.Owner.Players.Values)
                        if (player.DistSqr(host) < Player.RADIUS_SQR) {
                            player.Client.SendShowEffect(EffectType.Potion, entity.Id, new Position(), new Position(),
                                new ARGB(0xffffffff));
                            player.Client.SendShowEffect(EffectType.Trail, host.Id,
                                new Position {X = entity.X, Y = entity.Y}, new Position(), new ARGB(0xffffffff));
                            player.Client.SendNotification(entity.Id, "+" + n, new ARGB(0xff3366ff));
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