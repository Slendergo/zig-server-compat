using System.Xml.Linq;
using Shared;
using GameServer.realm;

namespace GameServer.logic.transitions;

internal class EntityExistsTransition : Transition {
    //State storage: none

    private readonly double _dist;
    private readonly ushort _target;

    public EntityExistsTransition(XElement e)
        : base(e.ParseString("@targetState", "root")) {
        _dist = e.ParseFloat("@dist");
        _target = Behavior.GetObjType(e.ParseString("@target"));
    }

    public EntityExistsTransition(string target, double dist, string targetState)
        : base(targetState) {
        _dist = dist;
        _target = Behavior.GetObjType(target);
    }

    protected override bool TickCore(Entity host, RealmTime time, ref object state) {
        return host.GetNearestEntity(_dist, _target) != null;
    }
}