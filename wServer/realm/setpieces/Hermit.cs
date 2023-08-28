using wServer.realm.worlds;

namespace wServer.realm.setpieces;

class Hermit : ISetPiece
{
    public int Size { get { return 32; } }

    public void RenderSetPiece(World world, IntPoint pos)
    {
        SetPieces.RenderSetpiece(world, pos, "hermit.pmap");
    }
}