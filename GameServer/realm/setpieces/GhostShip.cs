using GameServer.realm.worlds;

namespace GameServer.realm.setpieces;

internal class GhostShip : ISetPiece {
    public int Size => 40;

    public void RenderSetPiece(World world, IntPoint pos) {
        SetPieces.RenderSetpiece(world, pos, "ghost_ship.pmap");
    }
}