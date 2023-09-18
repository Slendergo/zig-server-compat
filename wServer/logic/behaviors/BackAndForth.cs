﻿using System.Xml.Linq;
using common;
using common.resources;
using wServer.realm;

namespace wServer.logic.behaviors;

internal class BackAndForth : CycleBehavior {
    private int distance;
    //State storage: remaining distance

    private float speed;

    public BackAndForth(XElement e) {
        speed = e.ParseFloat("@speed");
        distance = e.ParseInt("@distance", 5);
    }

    public BackAndForth(double speed, int distance = 5) {
        this.speed = (float) speed;
        this.distance = distance;
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state) {
        float dist;
        if (state == null) dist = distance;
        else dist = (float) state;

        Status = CycleStatus.NotStarted;

        if (host.HasConditionEffect(ConditionEffects.Paralyzed))
            return;

        var moveDist = host.GetSpeed(speed) * (time.ElapsedMsDelta / 1000f);
        if (dist > 0) {
            Status = CycleStatus.InProgress;
            host.ValidateAndMove(host.X + moveDist, host.Y);
            dist -= moveDist;
            if (dist <= 0) {
                dist = -distance;
                Status = CycleStatus.Completed;
            }
        }
        else {
            Status = CycleStatus.InProgress;
            host.ValidateAndMove(host.X - moveDist, host.Y);
            dist += moveDist;
            if (dist >= 0) {
                dist = distance;
                Status = CycleStatus.Completed;
            }
        }

        state = dist;
    }
}