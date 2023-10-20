using System.Collections.Concurrent;
using Shared.resources;
using GameServer.realm.entities.player;
using GameServer.realm.worlds;

namespace GameServer.realm.entities;

// todo make this not bad
// shouldnt be global server state it should be per player depending on state of client
// might be less memory efficient but will make more sense when your implementing proper hit detection

public class Projectile 
{
    private readonly HashSet<Entity> _hit = new();

    public Entity Owner { get; set; }
    public ushort Container { get; set; }
    public ProjectileDesc ProjDesc { get; }
    public long CreationTime { get; set; }
    private bool _used { get; set; }

    public byte ProjectileId { get; set; }
    public Position StartPos { get; set; }
    public float Angle { get; set; }
    public int Damage { get; set; }

    private readonly World World;

    // max value here as the type of projectile doesnt matter at all, projectiles shouldnt even be a entity base class i need to rewrite this - Slendergo
    public Projectile(World world, ProjectileDesc desc)
    {
        World = world;
        ProjDesc = desc;
    }

    public void Tick(RealmTime time) 
    {
        var elapsed = time.TotalElapsedMs - CreationTime;
        if (elapsed > ProjDesc.LifetimeMS) 
            Destroy();
    }

    public Position GetPosition(long elapsed) {
        var x = (double) StartPos.X;
        var y = (double) StartPos.Y;

        var dist = elapsed * ProjDesc.Speed / 10000.0;
        var period = ProjectileId % 2 == 0 ? 0 : Math.PI;

        if (ProjDesc.Wavy) {
            var theta = Angle + Math.PI * 64 * Math.Sin(period + 6 * Math.PI * (elapsed / 1000.0));
            x += dist * Math.Cos(theta);
            y += dist * Math.Sin(theta);
        }
        else if (ProjDesc.Parametric) {
            var theta = (double) elapsed / ProjDesc.LifetimeMS * 2 * Math.PI;
            var a = Math.Sin(theta) * (ProjectileId % 2 != 0 ? 1 : -1);
            var b = Math.Sin(theta * 2) * (ProjectileId % 4 < 2 ? 1 : -1);
            var c = Math.Sin(Angle);
            var d = Math.Cos(Angle);
            x += (a * d - b * c) * ProjDesc.Magnitude;
            y += (a * c + b * d) * ProjDesc.Magnitude;
        }
        else {
            if (ProjDesc.Boomerang) {
                var d = ProjDesc.LifetimeMS * ProjDesc.Speed / 10000.0 / 2;
                if (dist > d)
                    dist = d - (dist - d);
            }

            x += dist * Math.Cos(Angle);
            y += dist * Math.Sin(Angle);
            if (ProjDesc.Amplitude != 0) {
                var d = ProjDesc.Amplitude *
                        Math.Sin(
                            period + (double) elapsed / ProjDesc.LifetimeMS * ProjDesc.Frequency * 2 * Math.PI);
                x += d * Math.Cos(Angle + Math.PI / 2);
                y += d * Math.Sin(Angle + Math.PI / 2);
            }
        }

        return new Position {X = (float) x, Y = (float) y};
    }

    public void ForceHit(Entity entity, RealmTime time) {
        if (!ProjDesc.MultiHit && _used && !(entity is Player))
            return;

        if (_hit.Add(entity))
            entity.HitByProjectile(this, time);

        _used = true;
    }

    private bool Destroyed;

    public void Destroy()
    {
        if(Destroyed)
            return;
        Destroyed = true;

        if (World == null)
            Console.WriteLine("Failed to remove projectile World is Null, Possible Memroy Leak Occuring");

        if (!World.RemoveProjectile(this))
            Console.WriteLine("Failed to remove projectile not found in world, Possible Memroy Leak Occuring");

        Owner.Projectiles[ProjectileId] = null;
    }
}