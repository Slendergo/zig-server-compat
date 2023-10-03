using System.Xml.Linq;
using Shared;
using GameServer.realm;

namespace GameServer.logic.transitions;

internal class GroupNotExistTransition : Transition {
    //State storage: none

    private readonly double _dist;
    private readonly string _group;

    public GroupNotExistTransition(XElement e)
        : base(e.ParseString("@targetState", "root")) {
        _dist = e.ParseFloat("@dist");
        _group = e.ParseString("@group");
    }

    public GroupNotExistTransition(double dist, string targetState, string group)
        : base(targetState) {
        _dist = dist;
        _group = group;
    }

    protected override bool TickCore(Entity host, RealmTime time, ref object state) {
        if (string.IsNullOrWhiteSpace(_group))
            return false;

        return host.GetNearestEntityByGroup(_dist, _group) == null;
    }
}