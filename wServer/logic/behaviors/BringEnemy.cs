using common;
using System.Xml.Linq;
using wServer.realm;
using wServer.realm.entities;

namespace wServer.logic.behaviors;

class BringEnemy : Behavior
{
    string name;
    double range;

    public BringEnemy(XElement e)
    {
        range = e.ParseFloat("@range");
        name = e.ParseString("@name");
    }

    public BringEnemy(string name, double range)
    {
        this.name = name;
        this.range = range;
    }

    protected override void OnStateEntry(Entity host, RealmTime time, ref object state)
    {
        foreach (var entity in host.GetNearestEntitiesByName(range, name).OfType<Enemy>())
            entity.Move(host.X, host.Y);
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state)
    {
    }
}