using wServer.realm.worlds;

namespace wServer.realm.setpieces;

internal class LordoftheLostLands : ISetPiece {
    public int Size => 5;

    public void RenderSetPiece(World world, IntPoint pos) {
        var Lord = Entity.Resolve(world.Manager, "Lord of the Lost Lands");
        Lord.Move(pos.X + 2.5f, pos.Y + 2.5f);
        world.EnterWorld(Lord);
    }
}