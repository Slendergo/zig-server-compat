using wServer.realm.worlds;
using wServer.realm.worlds.parser;

namespace wServer.realm.setpieces;

class KageKami : ISetPiece
{
    public int Size { get { return 65; } }

    public void RenderSetPiece(World world, IntPoint pos)
    {
        SetPieces.RenderSetpiece(world, pos, "kage_kami.pmap");
    }
}