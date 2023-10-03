using Shared.resources;

namespace GameServer.realm.entities.player;

public partial class Player {
    private long l;

    private void HandleOceanTrenchGround(RealmTime time) {
        try {
            if (time.TotalElapsedMs - l <= 100 || Owner?.IdName != "OceanTrench") return;

            if (!(Owner?.StaticObjects.Where(i => i.Value.ObjectType == 0x0731).Count(i =>
                    (X - i.Value.X) * (X - i.Value.X) + (Y - i.Value.Y) * (Y - i.Value.Y) < 1) > 0)) {
                if (OxygenBar == 0)
                    HP -= 10;
                else
                    OxygenBar -= 2;

                if (HP <= 0)
                    Death("suffocation");
            }
            else {
                if (OxygenBar < 100)
                    OxygenBar += 8;
                if (OxygenBar > 100)
                    OxygenBar = 100;
            }

            l = time.TotalElapsedMs;
        }
        catch (Exception ex) {
            Log.Error(ex);
        }
    }

    public void ForceGroundHit(float x, float y, long timeHit) {
        if (HasConditionEffect(ConditionEffects.Paused) ||
            HasConditionEffect(ConditionEffects.Invincible))
            return;

        var tile = Owner.Map[(int) x, (int) y];
        var objDesc = tile.ObjectType == 0 ? null : Manager.Resources.GameData.ObjectDescs[tile.ObjectType];
        var tileDesc = Manager.Resources.GameData.Tiles[tile.TileId];
        if (!tileDesc.Damaging || objDesc != null && objDesc.ProtectFromGroundDamage)
            return;

        var dmg = (int) Client.Random.NextIntRange((uint) tileDesc.MinDamage, (uint) tileDesc.MaxDamage);
        HP -= dmg;

        foreach (var player in Owner.Players.Values)
            if (player.Id != Id && player.DistSqr(this) < RadiusSqr)
                player.Client.SendDamage(Id, 0, (ushort)dmg, HP <= 0);

        if (HP <= 0) Death(tileDesc.ObjectId, tile: tile);
    }
}