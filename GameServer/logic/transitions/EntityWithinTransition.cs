using System.Xml.Linq;
using Shared;
using GameServer.realm;

namespace GameServer.logic.transitions;

internal class EntityWithinTransition : Transition {
    //State storage: none

    private readonly double _dist;
    private readonly string _entity;

    public EntityWithinTransition(XElement e)
        : base(e.ParseString("@targetState", "root")) {
        _dist = e.ParseFloat("@dist");
        _entity = e.ParseString("@entity");
    }

    public EntityWithinTransition(double dist, string entity, string targetState)
        : base(targetState) {
        _dist = dist;
        _entity = entity;
    }

    protected override bool TickCore(Entity host, RealmTime time, ref object state) {
        return host.GetNearestEntityByName(_dist, _entity) != null;
    }
}