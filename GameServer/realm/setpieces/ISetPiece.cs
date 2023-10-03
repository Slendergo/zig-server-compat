using GameServer.realm.worlds;

namespace GameServer.realm.setpieces;

internal interface ISetPiece {
    int Size { get; }
    void RenderSetPiece(World world, IntPoint pos);
}