using GameServer.realm.worlds;

namespace GameServer.realm.setpieces;

internal class CubeGod : ISetPiece {
    public int Size => 5;

    public void RenderSetPiece(World world, IntPoint pos) {
        var cube = Entity.Resolve(world.Manager, "Cube God");
        cube.Move(pos.X + 2.5f, pos.Y + 2.5f);
        world.EnterWorld(cube);
    }
}