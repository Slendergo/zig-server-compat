using System.Xml.Linq;
using Shared;
using Shared.resources;
using GameServer.realm;
using GameServer.realm.entities;
using GameServer.realm.entities.player;

namespace GameServer.logic.behaviors;

internal class Shoot : CycleBehavior {
    private readonly float _angleOffset;
    private readonly int _coolDownOffset;
    private readonly int _count;
    private readonly float? _defaultAngle;
    private readonly float? _fixedAngle;
    private readonly float _predictive;

    private readonly int _projectileIndex;
    //State storage: cooldown timer

    private readonly float _radius;
    private readonly float? _rotateAngle;
    private readonly float _shootAngle;
    private readonly bool _shootLowHp;
    private Cooldown _coolDown;

    private int _rotateCount;

    public Shoot(XElement e) {
        _radius = e.ParseFloat("@radius", 5);
        _count = e.ParseInt("@count", 1);
        _shootAngle = _count == 1 ? 0 : (float) ((e.ParseNFloat("@shootAngle") ?? 360.0 / _count) * Math.PI / 180);
        _projectileIndex = e.ParseInt("@projectileIndex");
        _fixedAngle = (float?) (e.ParseNFloat("@fixedAngle") * Math.PI / 180);
        _rotateAngle = (float?) (e.ParseNFloat("@rotateAngle") * Math.PI / 180);
        _angleOffset = (float) (e.ParseFloat("@angleOffset") * Math.PI / 180);
        _defaultAngle = (float?) (e.ParseNFloat("@defaultAngle") * Math.PI / 180);
        _predictive = e.ParseFloat("@predictive");
        _coolDownOffset = e.ParseInt("@coolDownOffset");
        _coolDown = new Cooldown().Normalize(e.ParseInt("@cooldown", 1000));
        _shootLowHp = e.ParseBool("@shootLowHp");
    }

    public Shoot(
        double radius,
        int count = 1,
        double? shootAngle = null,
        int projectileIndex = 0,
        double? fixedAngle = null,
        double? rotateAngle = null,
        double angleOffset = 0,
        double? defaultAngle = null,
        double predictive = 0,
        int coolDownOffset = 0,
        Cooldown coolDown = new(),
        bool shootLowHp = false) {
        _radius = (float) radius;
        _count = count;
        _shootAngle = count == 1 ? 0 : (float) ((shootAngle ?? 360.0 / count) * Math.PI / 180);
        _projectileIndex = projectileIndex;
        _fixedAngle = (float?) (fixedAngle * Math.PI / 180);
        _rotateAngle = (float?) (rotateAngle * Math.PI / 180);
        _angleOffset = (float) (angleOffset * Math.PI / 180);
        _defaultAngle = (float?) (defaultAngle * Math.PI / 180);
        _predictive = (float) predictive;
        _coolDownOffset = coolDownOffset;
        _coolDown = coolDown.Normalize();
        _shootLowHp = shootLowHp;
    }

    protected override void OnStateEntry(Entity host, RealmTime time, ref object state) {
        state = _coolDownOffset;
    }

    private static float Predict(Entity host, Entity target, ProjectileDesc desc) {
        /*
        // new test prediction method
        const float msPast = 400;
        const long ticks = 4; // ticks equivalent to 400 ms
        var history = target.TryGetHistory(ticks);
        if (history == null)
            return 0;

        var position = new Vector2(target.X - host.X, target.Y - host.Y);
        var pastPosition = new Vector2(history.Value.X - host.X, history.Value.Y - host.Y);
        var relativeVelocity = Vector2.Subtract(position / msPast, pastPosition / msPast);
        var bulletSpeed = desc.Speed / 10000f;

        var futureHitTime = CollisionTime(position, relativeVelocity, bulletSpeed);
        if (futureHitTime > 0f)
        {
            var relativePosX = target.X + futureHitTime * relativeVelocity.X - host.X;
            var relativePosY = target.Y + futureHitTime * relativeVelocity.Y - host.Y;

            return (float) Math.Atan2(relativePosY, relativePosX) -
                   (float) Math.Atan2(position.Y, position.X);
        }

        return 0;
        */

        // trying prod prediction

        const int PREDICT_NUM_TICKS = 4; // magic determined by experiement
        var history = target.TryGetHistory(1);

        if (history == null) return (float) Math.Atan2(target.Y - host.Y, target.X - host.X);

        var targetX = target.X + PREDICT_NUM_TICKS *
            (target.X - history.Value.X);
        var targetY = target.Y + PREDICT_NUM_TICKS *
            (target.Y - history.Value.Y);

        var angle = (float) Math.Atan2(targetY - host.Y, targetX - host.X);

        return angle;
    }

    private static float CollisionTime(Vector2 position, Vector2 relativeV, float bulletV) {
        var a = Vector2.Dot(relativeV, relativeV) - bulletV * bulletV;
        var b = 2f * Vector2.Dot(relativeV, position);
        var c = Vector2.Dot(position, position);
        var det = b * b - 4f * a * c;

        if (det > 0f)
            return 2f * c / ((float) Math.Sqrt(det) - b);

        return -1f;
    }


    protected override void TickCore(Entity host, RealmTime time, ref object state) {
        var cool = (int?) state ??
                   -1; // <-- crashes server due to state being null... patched now but should be looked at.
        Status = CycleStatus.NotStarted;

        if (cool <= 0) {
            if (host.HasConditionEffect(ConditionEffects.Stunned))
                return;

            var count = _count;
            if (host.HasConditionEffect(ConditionEffects.Dazed))
                count = (int) Math.Ceiling(_count / 2.0);

            Entity player;
            if (host.AttackTarget != null)
                player = host.AttackTarget;
            else
                player = _shootLowHp ? host.GetLowestHpEntity(_radius, null) : host.GetNearestEntity(_radius, null);

            if (player != null || _defaultAngle != null || _fixedAngle != null) {
                var desc = host.ObjectDesc.Projectiles[_projectileIndex];

                float a;

                if (_fixedAngle != null) {
                    a = (float) _fixedAngle;
                }
                else if (player != null) {
                    if (_predictive != 0 && _predictive > Random.NextDouble())
                        a = Predict(host, player, desc);
                    else
                        a = (float) Math.Atan2(player.Y - host.Y, player.X - host.X);
                }
                else if (_defaultAngle != null) {
                    a = (float) _defaultAngle;
                }
                else {
                    a = 0;
                }

                a += _angleOffset + (_rotateAngle != null ? (float) _rotateAngle * _rotateCount : 0);
                _rotateCount++;

                var dmg = Random.Next(desc.MinDamage, desc.MaxDamage);
                var startAngle = a - _shootAngle * (count - 1) / 2;
                byte prjId = 0;
                var prjPos = new Position {X = host.X, Y = host.Y};
                var prjs = new Projectile[count];
                for (var i = 0; i < count; i++) {
                    var prj = host.CreateProjectile(desc, host.ObjectType, dmg, time.TotalElapsedMs, prjPos, startAngle + _shootAngle * i);
                    host.Owner.AddProjectile(prj);

                    if (i == 0)
                        prjId = prj.ProjectileId;

                    prjs[i] = prj;
                }

                foreach (var otherPlayer in host.Owner.Players.Values)
                    if (otherPlayer.DistSqr(host) < Player.RadiusSqr)
                        otherPlayer.Client.SendEnemyShoot(prjId, host.Id, (byte) desc.BulletType, prjPos,
                            startAngle, (short) dmg, (byte) count, _shootAngle);
            }

            cool = _coolDown.Next(Random);
            Status = CycleStatus.Completed;
        }
        else {
            cool -= time.ElapsedMsDelta;
            Status = CycleStatus.InProgress;
        }

        state = cool;
    }
}