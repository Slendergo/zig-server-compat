using System.Xml.Linq;
using Shared;
using GameServer.realm;

namespace GameServer.logic.transitions;

internal class NoPlayerWithinTransition : Transition {
    //State storage: none

    private double dist;

    public NoPlayerWithinTransition(XElement e)
        : base(e.ParseString("@targetState", "root")) {
        dist = e.ParseInt("@dist");
    }

    public NoPlayerWithinTransition(double dist, string targetState)
        : base(targetState) {
        this.dist = dist;
    }

    protected override bool TickCore(Entity host, RealmTime time, ref object state) {
        return host.GetNearestEntity(dist, null) == null;
    }
}