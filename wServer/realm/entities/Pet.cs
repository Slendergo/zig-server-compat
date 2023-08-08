namespace wServer.realm.entities;

public class Pet : Entity, IPlayer
{
    public Player PlayerOwner { get; set; }

    public Pet(RealmManager manager, Player player, ushort objType) : base(manager, objType)
    {
        PlayerOwner = player;
    }

    public void Damage(int dmg, Entity src) { }

    public bool IsVisibleToEnemy()
    {
        return false;
    }

}