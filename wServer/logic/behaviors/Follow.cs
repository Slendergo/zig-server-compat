using System.Xml.Linq;
using common;
using common.resources;
using Mono.Game;
using wServer.realm;

namespace wServer.logic.behaviors;

internal class Follow : CycleBehavior {
    private float acquireRange;
    private Cooldown coolDown;
    private int duration;
    private float range;

    private float speed;

    public Follow(XElement e) {
        speed = e.ParseFloat("@speed");
        acquireRange = e.ParseFloat("@acquireRange", 10);
        range = e.ParseFloat("@range", 4);
        duration = e.ParseInt("@duration");
        coolDown = new Cooldown().Normalize(e.ParseInt("@cooldown"));
    }

    public Follow(double speed, double acquireRange = 10, double range = 6,
        int duration = 0, Cooldown coolDown = new()) {
        this.speed = (float) speed;
        this.acquireRange = (float) acquireRange;
        this.range = (float) range;
        this.duration = duration;
        this.coolDown = coolDown.Normalize(duration == 0 ? 0 : 1000);
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state) {
        FollowState s;
        if (state == null) s = new FollowState();
        else s = (FollowState) state;

        Status = CycleStatus.NotStarted;

        if (host.HasConditionEffect(ConditionEffects.Paralyzed))
            return;

        //var pet = host as Pet;
        //if (pet != null)
        //    host.AttackTarget = pet.PlayerOwner;

        var player = host.AttackTarget ?? host.GetNearestEntity(acquireRange, null);

        Vector2 vect;
        switch (s.State) {
            case F.DontKnowWhere:
                if (player != null && s.RemainingTime <= 0) {
                    s.State = F.Acquired;
                    if (duration > 0)
                        s.RemainingTime = duration;
                    goto case F.Acquired;
                }

                if (s.RemainingTime > 0) s.RemainingTime -= time.ElapsedMsDelta;

                break;
            case F.Acquired:
                if (player == null) {
                    s.State = F.DontKnowWhere;
                    s.RemainingTime = 0;
                    break;
                }

                if (s.RemainingTime <= 0 && duration > 0) {
                    s.State = F.DontKnowWhere;
                    s.RemainingTime = coolDown.Next(Random);
                    Status = CycleStatus.Completed;
                    break;
                }

                if (s.RemainingTime > 0)
                    s.RemainingTime -= time.ElapsedMsDelta;

                vect = new Vector2(player.X - host.X, player.Y - host.Y);
                if (vect.Length() > range) {
                    Status = CycleStatus.InProgress;
                    vect.X -= Random.Next(-2, 2) / 2f;
                    vect.Y -= Random.Next(-2, 2) / 2f;
                    vect.Normalize();
                    var dist = host.GetSpeed(speed) * (time.ElapsedMsDelta / 1000f);
                    host.ValidateAndMove(host.X + vect.X * dist, host.Y + vect.Y * dist);
                }
                else {
                    Status = CycleStatus.Completed;
                    s.State = F.Resting;
                    s.RemainingTime = 0;
                }

                break;
            case F.Resting:
                if (player == null) {
                    s.State = F.DontKnowWhere;
                    if (duration > 0)
                        s.RemainingTime = duration;
                    break;
                }

                Status = CycleStatus.Completed;
                vect = new Vector2(player.X - host.X, player.Y - host.Y);
                if (vect.Length() > range + 1) {
                    s.State = F.Acquired;
                    s.RemainingTime = duration;
                    goto case F.Acquired;
                }

                break;
        }

        state = s;
    }

    //State storage: follow state
    private class FollowState {
        public int RemainingTime;
        public F State;
    }

    private enum F {
        DontKnowWhere,
        Acquired,
        Resting
    }
}