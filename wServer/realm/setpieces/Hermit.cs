using wServer.realm.worlds;

namespace wServer.realm.setpieces;

internal class Hermit : ISetPiece {
    public int Size => 32;

    public void RenderSetPiece(World world, IntPoint pos) {
        SetPieces.RenderSetpiece(world, pos, "hermit.pmap");
    }
}