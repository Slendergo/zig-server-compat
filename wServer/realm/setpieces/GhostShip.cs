using wServer.realm.worlds;

namespace wServer.realm.setpieces;

internal class GhostShip : ISetPiece {
    public int Size => 40;

    public void RenderSetPiece(World world, IntPoint pos) {
        SetPieces.RenderSetpiece(world, pos, "ghost_ship.pmap");
    }
}