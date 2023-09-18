﻿using System.Xml.Linq;
using common;
using common.resources;
using Mono.Game;
using wServer.realm;

namespace wServer.logic.behaviors;

internal class Swirl : CycleBehavior {
    private float acquireRange;
    private float radius;

    private float speed;
    private bool targeted;

    public Swirl(XElement e) {
        speed = e.ParseFloat("@speed", 1);
        radius = e.ParseFloat("@radius", 8);
        acquireRange = e.ParseFloat("@acquireRange", 10);
        targeted = e.ParseBool("@targeted", true);
    }

    public Swirl(double speed = 1, double radius = 8, double acquireRange = 10, bool targeted = true) {
        this.speed = (float) speed;
        this.radius = (float) radius;
        this.acquireRange = (float) acquireRange;
        this.targeted = targeted;
    }

    protected override void OnStateEntry(Entity host, RealmTime time, ref object state) {
        state = new SwirlState {
            Center = targeted ? Vector2.Zero : new Vector2(host.X, host.Y),
            Acquired = !targeted
        };
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state) {
        var s = (SwirlState) state;

        Status = CycleStatus.NotStarted;

        if (host.HasConditionEffect(ConditionEffects.Paralyzed))
            return;

        var period = (int) (1000 * radius / host.GetSpeed(speed) * (2 * Math.PI));
        if (!s.Acquired &&
            s.RemainingTime <= 0 &&
            targeted) {
            var entity = host.GetNearestEntity(acquireRange, null);
            if (entity != null && entity.X != host.X && entity.Y != host.Y) {
                //find circle which pass through host and player pos
                var l = entity.Dist(host);
                var hx = (host.X + entity.X) / 2;
                var hy = (host.Y + entity.Y) / 2;
                var c = Math.Sqrt(Math.Abs(radius * radius - l * l) / 4);
                s.Center = new Vector2(
                    (float) (hx + c * (host.Y - entity.Y) / l),
                    (float) (hy + c * (entity.X - host.X) / l));

                s.RemainingTime = period;
                s.Acquired = true;
            }
            else {
                s.Acquired = false;
            }
        }
        else if (s.RemainingTime <= 0 || (s.RemainingTime - period > 200 && host.GetNearestEntity(2, null) != null)) {
            if (targeted) {
                s.Acquired = false;
                var entity = host.GetNearestEntity(acquireRange, null);
                if (entity != null)
                    s.RemainingTime = 0;
                else
                    s.RemainingTime = 5000;
            }
            else {
                s.RemainingTime = 5000;
            }
        }
        else {
            s.RemainingTime -= time.ElapsedMsDelta;
        }

        double angle;
        if (host.Y == s.Center.Y && host.X == s.Center.X) //small offset
            angle = Math.Atan2(host.Y - s.Center.Y + (Random.NextDouble() * 2 - 1),
                host.X - s.Center.X + (Random.NextDouble() * 2 - 1));
        else
            angle = Math.Atan2(host.Y - s.Center.Y, host.X - s.Center.X);

        var spd = host.GetSpeed(speed) * (s.Acquired ? 1 : 0.2);
        var angularSpd = spd / radius;
        angle += angularSpd * (time.ElapsedMsDelta / 1000f);

        var x = s.Center.X + Math.Cos(angle) * radius;
        var y = s.Center.Y + Math.Sin(angle) * radius;
        var vect = new Vector2((float) x, (float) y) - new Vector2(host.X, host.Y);
        vect.Normalize();
        vect *= (float) spd * (time.ElapsedMsDelta / 1000f);

        host.ValidateAndMove(host.X + vect.X, host.Y + vect.Y);

        Status = CycleStatus.InProgress;

        state = s;
    }

    //State storage: swirl state
    private class SwirlState {
        public bool Acquired;
        public Vector2 Center;
        public int RemainingTime;
    }
}