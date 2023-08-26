using wServer.realm.worlds;

namespace wServer.realm.setpieces;

class GhostShip : ISetPiece
{
    public int Size { get { return 40; } }

    public void RenderSetPiece(World world, IntPoint pos)
    {
        SetPieces.RenderSetpiece(world, pos, "ghost_ship.map");
    }
}