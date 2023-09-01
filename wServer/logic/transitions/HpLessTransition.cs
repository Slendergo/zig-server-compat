using common;
using System.Xml.Linq;
using wServer.realm;
using wServer.realm.entities;

namespace wServer.logic.transitions;

class HpLessTransition : Transition
{
    //State storage: none

    double threshold;

    public HpLessTransition(XElement e)
    : base(e.ParseString("@targetState", "root"))
    {
        threshold = e.ParseFloat("@threshold");
    }

    public HpLessTransition(double threshold, string targetState)
        : base(targetState)
    {
        this.threshold = threshold;
    }

    protected override bool TickCore(Entity host, RealmTime time, ref object state)
    {
        return ((double)(host as Enemy).HP / host.ObjectDesc.MaxHP) < threshold;
    }
}