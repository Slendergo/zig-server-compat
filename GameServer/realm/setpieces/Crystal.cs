using GameServer.realm.worlds;

namespace GameServer.realm.setpieces;

internal class Crystal : ISetPiece {
    public int Size => 5;

    public void RenderSetPiece(World world, IntPoint pos) {
        var Crystal = Entity.Resolve(world.Manager, "Mysterious Crystal");
        Crystal.Move(pos.X + 2.5f, pos.Y + 2.5f);
        world.EnterWorld(Crystal);
    }
}