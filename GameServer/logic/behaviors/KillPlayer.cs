using System.Xml.Linq;
using Shared;
using GameServer.realm;
using GameServer.realm.entities;
using GameServer.realm.entities.player;

namespace GameServer.logic.behaviors;

internal class KillPlayer : Behavior {
    private readonly bool _killAll;
    private readonly string _killMessage;
    private Cooldown _coolDown;

    public KillPlayer(XElement e) {
        _killMessage = e.ParseString("@killMessage");
        _coolDown = new Cooldown().Normalize(e.ParseInt("@cooldown", 1000));
        _killAll = e.ParseBool("@killAll");
    }

    public KillPlayer(string killMessage, Cooldown coolDown = new(), bool killAll = false) {
        _coolDown = coolDown.Normalize();
        _killMessage = killMessage;
        _killAll = killAll;
    }

    protected override void OnStateEntry(Entity host, RealmTime time, ref object state) {
        state = _coolDown.Next(Random);
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state) {
        if (host.AttackTarget == null || host.AttackTarget.Owner == null)
            return;

        var cool = (int) state;

        if (cool <= 0) {
            // death strike
            if (_killAll)
                foreach (var plr in host.Owner.Players.Values)
                    Kill(host, plr);
            else
                Kill(host, host.AttackTarget);

            // send kill message
            if (_killMessage != null)
                foreach (var player in host.Owner.Players.Values)
                    if (player.DistSqr(host) < Player.RADIUS_SQR)
                        player.SendEnemy(host.ObjectDesc.DisplayId ?? host.ObjectDesc.ObjectId, _killMessage);

            cool = _coolDown.Next(Random);
        }
        else {
            cool -= time.ElapsedMsDelta;
        }

        state = cool;
    }

    private void Kill(Entity host, Player player) {
        foreach (var otherPlayer in host.Owner.Players.Values)
            if (otherPlayer.DistSqr(host) < Player.RADIUS_SQR)
                otherPlayer.Client.SendShowEffect(EffectType.Flashing, host.Id,
                    new Position {X = player.X, Y = player.Y}, new Position(), new ARGB(0xffffffff));

        // kill player
        player.Death(host.ObjectDesc.DisplayId);
    }
}