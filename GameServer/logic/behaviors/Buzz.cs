using System.Xml.Linq;
using Shared;
using Shared.resources;
using GameServer.realm;

namespace GameServer.logic.behaviors;

internal class Buzz : CycleBehavior {
    private Cooldown coolDown;
    private float dist;


    private float speed;

    public Buzz(XElement e) {
        speed = e.ParseFloat("@speed", 2);
        dist = e.ParseFloat("@dist", 0.5f);
        coolDown = new Cooldown().Normalize(e.ParseInt("@cooldown", 1));
    }

    public Buzz(double speed = 2, double dist = 0.5, Cooldown coolDown = new()) {
        this.speed = (float) speed;
        this.dist = (float) dist;
        this.coolDown = coolDown.Normalize(1);
    }

    protected override void OnStateEntry(Entity host, RealmTime time, ref object state) {
        state = new BuzzStorage();
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state) {
        var storage = (BuzzStorage) state;

        Status = CycleStatus.NotStarted;

        if (host.HasConditionEffect(ConditionEffects.Paralyzed))
            return;

        if (storage.RemainingTime > 0) {
            storage.RemainingTime -= time.ElapsedMsDelta;
            Status = CycleStatus.NotStarted;
        }
        else {
            Status = CycleStatus.InProgress;
            if (storage.RemainingDistance <= 0) {
                do {
                    storage.Direction = new Vector2(Random.Next(-1, 2), Random.Next(-1, 2));
                } while (storage.Direction.X == 0 && storage.Direction.Y == 0);

                storage.Direction.Normalize();
                storage.RemainingDistance = this.dist;
                Status = CycleStatus.Completed;
            }

            var dist = host.GetSpeed(speed) * (time.ElapsedMsDelta / 1000f);
            host.ValidateAndMove(host.X + storage.Direction.X * dist, host.Y + storage.Direction.Y * dist);

            storage.RemainingDistance -= dist;
        }

        state = storage;
    }

    //State storage: direction & remain
    private class BuzzStorage {
        public Vector2 Direction;
        public float RemainingDistance;
        public int RemainingTime;
    }
}