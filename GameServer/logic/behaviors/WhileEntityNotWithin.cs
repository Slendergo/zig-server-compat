using System.Xml.Linq;
using Shared;
using GameServer.realm;

namespace GameServer.logic.behaviors;

internal class WhileEntityNotWithin : Behavior {
    private Behavior[] children;
    private string entityName;
    private double range;

    public WhileEntityNotWithin(XElement e, IStateChildren[] behaviors) {
        children = new Behavior[behaviors.Length];
        var filledIdx = 0;
        for (var i = 0; i < behaviors.Length; i++) {
            var behav = behaviors[i];
            if (behav is Behavior behavior)
                children[filledIdx++] = behavior;
        }

        Array.Resize(ref children, filledIdx);

        entityName = e.ParseString("@entityName");
        range = e.ParseFloat("@range");
    }

    protected override void OnStateEntry(Entity host, RealmTime time, ref object state) {
        foreach (var behav in children)
            behav.OnStateEntry(host, time);
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state) {
        if (host.GetNearestEntityByName(range, entityName) != null)
            return;

        foreach (var behav in children)
            behav.Tick(host, time);
    }
}