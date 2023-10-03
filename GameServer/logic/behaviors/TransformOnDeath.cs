using System.Xml.Linq;
using Shared;
using GameServer.realm;
using GameServer.realm.entities;

namespace GameServer.logic.behaviors;

internal class TransformOnDeath : Behavior {
    private int max;
    private int min;
    private float probability;
    private ushort target;

    public TransformOnDeath(XElement e) {
        target = GetObjType(e.ParseString("@target"));
        min = e.ParseInt("@min", 1);
        max = e.ParseInt("@max", 1);
        probability = e.ParseFloat("@probability", 1);
    }

    public TransformOnDeath(string target, int min = 1, int max = 1, double probability = 1) {
        this.target = GetObjType(target);
        this.min = min;
        this.max = max;
        this.probability = (float) probability;
    }

    protected internal override void Resolve(State parent) {
        parent.Death += (sender, e) => {
            if (e.Host.CurrentState.Is(parent) &&
                Random.NextDouble() < probability) {
                if (Entity.Resolve(e.Host.Manager, target) is Portal
                    && e.Host.Owner.IdName.Contains("Arena"))
                    return;
                var count = Random.Next(min, max + 1);
                for (var i = 0; i < count; i++) {
                    var entity = Entity.Resolve(e.Host.Manager, target);

                    entity.Move(e.Host.X, e.Host.Y);

                    if (e.Host is Enemy && entity is Enemy && (e.Host as Enemy).Spawned)
                        (entity as Enemy).Spawned = true;

                    e.Host.Owner.EnterWorld(entity);
                }
            }
        };
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state) { }
}