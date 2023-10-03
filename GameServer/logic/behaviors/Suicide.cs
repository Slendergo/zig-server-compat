using System.Xml.Linq;
using GameServer.realm;
using GameServer.realm.entities;

namespace GameServer.logic.behaviors;

internal class Suicide : Behavior {
    //State storage: timer

    public Suicide(XElement e) { }

    public Suicide() { }

    protected override void TickCore(Entity host, RealmTime time, ref object state) {
        if (!(host is Enemy))
            throw new NotSupportedException("Use Decay instead");
        (host as Enemy).Death(time);
    }
}