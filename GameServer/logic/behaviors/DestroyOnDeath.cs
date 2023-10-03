using System.Xml.Linq;
using Shared;
using GameServer.realm;

namespace GameServer.logic.behaviors;

internal class DestroyOnDeath : Behavior {
    private readonly string _target;

    public DestroyOnDeath(XElement e) {
        _target = e.ParseString("@target");
    }

    public DestroyOnDeath(string target) {
        _target = target;
    }

    protected internal override void Resolve(State parent) {
        parent.Death += (sender, e) => {
            var owner = e.Host.Owner;
            var entities = e.Host.GetNearestEntitiesByName(250, _target);

            if (entities != null)
                foreach (var ent in entities)
                    owner.LeaveWorld(ent);
        };
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state) { }
}