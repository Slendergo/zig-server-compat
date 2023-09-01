using common;
using System.Xml.Linq;
using wServer.realm;

namespace wServer.logic.transitions;

class PlayerWithinTransition : Transition
{
    //State storage: none

    private readonly double _dist;
    private readonly bool _seeInvis;

    public PlayerWithinTransition(XElement e)
    : base(e.ParseString("@targetState", "root"))
    {
        _dist = e.ParseFloat("@dist");
        _seeInvis = e.ParseBool("@seeInvis");
    }

    public PlayerWithinTransition(double dist, string targetState, bool seeInvis = false)
        : base(targetState)
    {
        _dist = dist;
        _seeInvis = seeInvis;
    }

    protected override bool TickCore(Entity host, RealmTime time, ref object state)
    {
        return host.GetNearestEntity(_dist, null, _seeInvis) != null;
    }
}