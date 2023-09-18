using System.Xml.Linq;
using common;
using wServer.realm;

namespace wServer.logic.behaviors;

internal class Decay : Behavior {
    //State storage: timer

    private int time;

    public Decay(XElement e) {
        time = e.ParseInt("@time", 10000);
    }

    public Decay(int time = 10000) {
        this.time = time;
    }

    protected override void OnStateEntry(Entity host, RealmTime time, ref object state) {
        state = this.time;
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state) {
        var cool = (int) state;

        if (cool <= 0)
            host.Owner.LeaveWorld(host);
        else
            cool -= time.ElapsedMsDelta;

        state = cool;
    }
}