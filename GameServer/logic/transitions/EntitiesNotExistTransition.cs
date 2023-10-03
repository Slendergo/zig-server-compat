using System.Xml.Linq;
using Shared;
using GameServer.realm;

namespace GameServer.logic.transitions;

internal class EntitiesNotExistsTransition : Transition {
    //State storage: none

    private readonly double _dist;
    private readonly ushort[] _targets;

    public EntitiesNotExistsTransition(XElement e)
        : base(e.ParseString("@targetState", "root")) {
        _dist = e.ParseFloat("@dist");
        _targets = e.ParseStringArray("@targets", ',', new string[0]).Select(x => Behavior.GetObjType(x)).ToArray();
    }

    public EntitiesNotExistsTransition(double dist, string targetState, params string[] targets)
        : base(targetState) {
        _dist = dist;

        if (targets.Length <= 0)
            return;

        _targets = targets
            .Select(Behavior.GetObjType)
            .ToArray();
    }

    protected override bool TickCore(Entity host, RealmTime time, ref object state) {
        if (_targets == null)
            return false;

        return _targets.All(t => host.GetNearestEntity(_dist, t) == null);
    }
}