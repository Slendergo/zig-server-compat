using System.Xml.Linq;
using common;
using wServer.logic;
using wServer.realm;

namespace wServer.logic.behaviors;

internal class Duration : Behavior
{
    private Behavior[] children;
    private int duration;

    public Duration(XElement e, IStateChildren[] behaviors)
    {
        children = new Behavior[behaviors.Length];
        var filledIdx = 0;
        for (var i = 0; i < behaviors.Length; i++)
        {
            var behav = behaviors[i];
            if (behav is Behavior behavior)
                children[filledIdx++] = behavior;
        }

        Array.Resize(ref children, filledIdx);

        duration = e.ParseInt("@duration");
    }

    protected override void OnStateEntry(Entity host, RealmTime time, ref object state)
    {
        foreach (var behav in children)
            behav.OnStateEntry(host, time);
        state = 0;
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state)
    {
        var timeElapsed = (int)state;
        if (timeElapsed <= duration)
        {
            foreach (var behav in children)
                behav.Tick(host, time);
            timeElapsed += time.ElaspedMsDelta;
        }

        state = timeElapsed;
    }
}