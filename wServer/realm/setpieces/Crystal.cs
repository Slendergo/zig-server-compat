using wServer.realm.worlds;

namespace wServer.realm.setpieces;

class Crystal : ISetPiece
{
    public int Size { get { return 5; } }

    public void RenderSetPiece(World world, IntPoint pos)
    {
        Entity Crystal = Entity.Resolve(world.Manager, "Mysterious Crystal");
        Crystal.Move(pos.X + 2.5f, pos.Y + 2.5f);
        world.EnterWorld(Crystal);
    }
}