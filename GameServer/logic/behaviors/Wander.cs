﻿using System.Xml.Linq;
using Shared;
using Shared.resources;
using GameServer.realm;

namespace GameServer.logic.behaviors;

internal class Wander : CycleBehavior {
    private float speed;

    public Wander(XElement e) {
        speed = e.ParseFloat("@speed");
    }

    public Wander(double speed) {
        this.speed = (float) speed;
    }

    //static Cooldown period = new Cooldown(500, 200);
    protected override void TickCore(Entity host, RealmTime time, ref object state) {
        WanderStorage storage;
        if (state == null) storage = new WanderStorage();
        else storage = (WanderStorage) state;

        Status = CycleStatus.NotStarted;

        if (host.HasConditionEffect(ConditionEffects.Paralyzed))
            return;

        Status = CycleStatus.InProgress;
        if (storage.RemainingDistance <= 0) {
            // old wander
            //storage.Direction = new Vector2(Random.Next(-1, 2), Random.Next(-1, 2));
            //storage.Direction.Normalize();
            //storage.RemainingDistance = period.Next(Random) / 1000f;
            //Status = CycleStatus.Completed;

            // creepylava's newer wander
            storage.Direction = new Vector2(Random.Next() % 2 == 0 ? -1 : 1, Random.Next() % 2 == 0 ? -1 : 1);
            storage.Direction.Normalize();
            storage.RemainingDistance = 600 / 1000f;
            Status = CycleStatus.Completed;
        }

        var dist = host.GetSpeed(speed) * (time.ElapsedMsDelta / 1000f);
        host.ValidateAndMove(host.X + storage.Direction.X * dist, host.Y + storage.Direction.Y * dist);

        storage.RemainingDistance -= dist;

        state = storage;
    }

    //State storage: direction & remain time
    private class WanderStorage {
        public Vector2 Direction;
        public float RemainingDistance;
    }
}