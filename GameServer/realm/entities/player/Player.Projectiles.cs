using Shared.resources;

namespace GameServer.realm.entities.player;

public partial class Player {
    internal Projectile PlayerShootProjectile(
        byte id, ProjectileDesc desc, ushort objType,
        long startTime, Position position, float angle) {
        projectileId = id;
        var dmg = Stats.GetClientDamage(desc.MinDamage, desc.MaxDamage, true);
        return CreateProjectile(desc, objType, dmg,
            C2STime(startTime), position, angle);
    }
}