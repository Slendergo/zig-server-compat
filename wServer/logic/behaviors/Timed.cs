using System.Xml.Linq;
using common;
using wServer.realm;

namespace wServer.logic.behaviors;

//replacement for simple timed transition in sequence
internal class Timed : CycleBehavior {
    //State storage: time

    private Behavior[] behaviors;
    private int period;

    public Timed(XElement e, IStateChildren[] children) {
        var behaviors = new List<Behavior>();
        foreach (var child in children)
            if (child is Behavior bh)
                behaviors.Add(bh);

        this.behaviors = behaviors.ToArray();
        period = e.ParseInt("@period");
    }

    public Timed(int period, params Behavior[] behaviors) {
        this.behaviors = behaviors;
        this.period = period;
    }

    protected override void OnStateEntry(Entity host, RealmTime time, ref object state) {
        foreach (var behavior in behaviors)
            behavior.OnStateEntry(host, time);
        state = period;
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state) {
        var period = (int) state;

        foreach (var behavior in behaviors) {
            behavior.Tick(host, time);
            Status = CycleStatus.InProgress;

            period -= time.ElapsedMsDelta;
            if (period <= 0) {
                period = this.period;
                Status = CycleStatus.Completed;
                //......- -
                if (behavior is Prioritize)
                    host.StateStorage[behavior] = -1;
            }
        }

        state = period;
    }
}