using Shared.resources;

namespace GameServer.realm.worlds.logic;

public sealed class OryxCastle : World {
    private int PlayersEntering;

    public OryxCastle(RealmManager manager, WorldTemplateData template)
        : base(manager, template) { }

    public void SetPlayers(int playersEntering) {
        PlayersEntering = playersEntering;
    }

    public override KeyValuePair<IntPoint, TileRegion>[] GetSpawnPoints() {
        if (PlayersEntering < 20)
            return Map.Regions.Where(t => t.Value == TileRegion.Spawn).Take(1).ToArray();
        if (PlayersEntering < 40)
            return Map.Regions.Where(t => t.Value == TileRegion.Spawn).Take(2).ToArray();
        if (PlayersEntering < 60)
            return Map.Regions.Where(t => t.Value == TileRegion.Spawn).Take(3).ToArray();
        return Map.Regions.Where(t => t.Value == TileRegion.Spawn).ToArray();
    }
}