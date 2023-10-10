using System.Xml.Linq;
using GameServer.realm;
using GameServer.realm.entities;
using GameServer.realm.entities.player;

namespace GameServer.logic.behaviors;

internal class WhileWatched : CycleBehavior {
    private Behavior[] children;

    public WhileWatched(XElement e, IStateChildren[] behaviors) {
        children = new Behavior[behaviors.Length];
        var filledIdx = 0;
        for (var i = 0; i < behaviors.Length; i++) {
            var behav = behaviors[i];
            if (behav is Behavior behavior)
                children[filledIdx++] = behavior;
        }

        Array.Resize(ref children, filledIdx);
    }

    protected override void OnStateEntry(Entity host, RealmTime time, ref object state) {
        foreach (var player in host.GetNearestEntities(Player.RADIUS, null, true).OfType<Player>())
            if (player.ClientEntities.Contains(host)) {
                foreach (var behav in children) {
                    behav.OnStateEntry(host, time);
                    Status = behav is CycleBehavior behavior ? behavior.Status : CycleStatus.InProgress;
                }
                return;
            }

        Status = CycleStatus.NotStarted;
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state) {
        foreach (var player in host.GetNearestEntities(Player.RADIUS, null, true).OfType<Player>())
            if (player.ClientEntities.Contains(host)) {
                foreach (var behav in children) {
                    behav.Tick(host, time);
                    Status = behav is CycleBehavior behavior ? behavior.Status : CycleStatus.InProgress;
                }

                return;
            }

        Status = CycleStatus.NotStarted;
    }

    protected override void OnStateExit(Entity host, RealmTime time, ref object state) {
        foreach (var player in host.GetNearestEntities(Player.RADIUS, null, true).OfType<Player>())
            if (player.ClientEntities.Contains(host)) {
                foreach (var behav in children) {
                    behav.OnStateExit(host, time);
                    Status = behav is CycleBehavior behavior ? behavior.Status : CycleStatus.InProgress;
                }

                return;
            }

        Status = CycleStatus.NotStarted;
    }
}