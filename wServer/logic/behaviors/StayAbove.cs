﻿using System.Xml.Linq;
using common;
using common.resources;
using Mono.Game;
using wServer.realm;

namespace wServer.logic.behaviors;

internal class StayAbove : CycleBehavior {
    private int altitude;
    //State storage: none

    private float speed;

    public StayAbove(XElement e) {
        speed = e.ParseFloat("@speed");
        altitude = e.ParseInt("@altitude");
    }

    public StayAbove(double speed, int altitude) {
        this.speed = (float) speed;
        this.altitude = altitude;
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state) {
        Status = CycleStatus.NotStarted;

        if (host.HasConditionEffect(ConditionEffects.Paralyzed))
            return;

        var map = host.Owner.Map;
        var tile = map[(int) host.X, (int) host.Y];
        if (tile.Elevation != 0 && tile.Elevation < altitude) {
            Vector2 vect;
            vect = new Vector2(map.Width / 2 - host.X, map.Height / 2 - host.Y);
            vect.Normalize();
            var dist = host.GetSpeed(speed) * (time.ElapsedMsDelta / 1000f);
            host.ValidateAndMove(host.X + vect.X * dist, host.Y + vect.Y * dist);

            Status = CycleStatus.InProgress;
        }
        else {
            Status = CycleStatus.Completed;
        }
    }
}