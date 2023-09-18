using wServer.realm.worlds;

namespace wServer.realm.setpieces;

internal class KageKami : ISetPiece {
    public int Size => 65;

    public void RenderSetPiece(World world, IntPoint pos) {
        SetPieces.RenderSetpiece(world, pos, "kage_kami.pmap");
    }
}