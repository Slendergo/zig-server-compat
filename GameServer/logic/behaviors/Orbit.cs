using System.Xml.Linq;
using Shared;
using Shared.resources;
using GameServer.realm;

namespace GameServer.logic.behaviors;

internal class Orbit : CycleBehavior {
    private float acquireRange;
    private bool? orbitClockwise;
    private float radius;
    private float radiusVariance;

    private float speed;
    private float speedVariance;
    private ushort? target;

    public Orbit(XElement e) {
        speed = e.ParseFloat("@speed");
        radius = e.ParseFloat("@radius");
        acquireRange = e.ParseFloat("@acquireRange", 10);
        var targetString = e.ParseString("@target");
        target = targetString == null ? null : GetObjType(targetString);
        speedVariance = e.ParseNFloat("@speedVariance") ?? speed * 0.1f;
        radiusVariance = e.ParseNFloat("@radiusVariance") ?? speed * 0.1f;
        orbitClockwise = e.ParseBool("@orbitClockwise");
    }

    public Orbit(double speed, double radius, double acquireRange = 10,
        string target = null, double? speedVariance = null, double? radiusVariance = null,
        bool? orbitClockwise = false) {
        this.speed = (float) speed;
        this.radius = (float) radius;
        this.acquireRange = (float) acquireRange;
        this.target = target == null ? null : GetObjType(target);
        this.speedVariance = (float) (speedVariance ?? speed * 0.1);
        this.radiusVariance = (float) (radiusVariance ?? speed * 0.1);
        this.orbitClockwise = orbitClockwise;
    }

    protected override void OnStateEntry(Entity host, RealmTime time, ref object state) {
        int orbitDir;
        if (orbitClockwise == null)
            orbitDir = Random.Next(1, 3) == 1 ? 1 : -1;
        else
            orbitDir = (bool) orbitClockwise ? 1 : -1;

        state = new OrbitState {
            Speed = speed + speedVariance * (float) (Random.NextDouble() * 2 - 1),
            Radius = radius + radiusVariance * (float) (Random.NextDouble() * 2 - 1),
            Direction = orbitDir
        };
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state) {
        var s = (OrbitState) state;

        Status = CycleStatus.NotStarted;

        if (host.HasConditionEffect(ConditionEffects.Paralyzed))
            return;

        var entity = host.AttackTarget ?? host.GetNearestEntity(acquireRange, target);

        if (entity != null) {
            double angle;
            if (host.Y == entity.Y && host.X == entity.X) //small offset
                angle = Math.Atan2(host.Y - entity.Y + (Random.NextDouble() * 2 - 1),
                    host.X - entity.X + (Random.NextDouble() * 2 - 1));
            else
                angle = Math.Atan2(host.Y - entity.Y, host.X - entity.X);
            var angularSpd = s.Direction * host.GetSpeed(s.Speed) / s.Radius;
            angle += angularSpd * (time.ElapsedMsDelta / 1000f);

            var x = entity.X + Math.Cos(angle) * s.Radius;
            var y = entity.Y + Math.Sin(angle) * s.Radius;
            var vect = new Vector2((float) x, (float) y) - new Vector2(host.X, host.Y);
            vect.Normalize();
            vect *= host.GetSpeed(s.Speed) * (time.ElapsedMsDelta / 1000f);

            host.ValidateAndMove(host.X + vect.X, host.Y + vect.Y);

            Status = CycleStatus.InProgress;
        }

        state = s;
    }

    //State storage: orbit state
    private class OrbitState {
        public int Direction;
        public float Radius;
        public float Speed;
    }
}