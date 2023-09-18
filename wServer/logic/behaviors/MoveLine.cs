using System.Xml.Linq;
using common;
using common.resources;
using Mono.Game;
using wServer.realm;

namespace wServer.logic.behaviors;

internal class MoveLine : CycleBehavior {
    private readonly float _direction;
    private readonly float _speed;

    public MoveLine(XElement e) {
        _speed = e.ParseFloat("@speed");
        _direction = e.ParseFloat("@direction") * (float) Math.PI / 180;
    }

    public MoveLine(double speed, double direction = 0) {
        _speed = (float) speed;
        _direction = (float) direction * (float) Math.PI / 180;
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state) {
        Status = CycleStatus.NotStarted;
        if (host.HasConditionEffect(ConditionEffects.Paralyzed))
            return;

        Status = CycleStatus.InProgress;
        var vect = new Vector2((float) Math.Cos(_direction), (float) Math.Sin(_direction));
        var dist = host.GetSpeed(_speed) * time.ElapsedMsDelta / 1000f;
        host.ValidateAndMove(host.X + vect.X * dist, host.Y + vect.Y * dist);

        // Varanus, is this a proper CycleBehavior? There is no CycleStatus.Completed...
    }
}