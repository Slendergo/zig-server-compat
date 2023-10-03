using System.Xml.Linq;
using Shared;
using GameServer.realm;

namespace GameServer.logic.transitions;

internal class NoEntityWithinTransition : Transition {
    //State storage: none

    private readonly int _dist;

    public NoEntityWithinTransition(XElement e)
        : base(e.ParseString("@targetState", "root")) {
        _dist = e.ParseInt("@dist");
    }

    public NoEntityWithinTransition(int dist, string targetState)
        : base(targetState) {
        _dist = dist;
    }

    protected override bool TickCore(Entity host, RealmTime time, ref object state) {
        return !host.AnyEnemyNearby(_dist) && !host.AnyPlayerNearby(_dist);
    }
}