using System.Xml.Linq;
using Shared;
using GameServer.realm;

namespace GameServer.logic.transitions;

internal class TimedRandomTransition : Transition {
    private readonly bool _randomized;
    //State storage: cooldown timer

    private readonly int _time;

    public TimedRandomTransition(XElement e)
        : base(e.ParseStringArray("@targetStates", ',', new[] {"root"})) {
        _time = e.ParseInt("@time");
        _randomized = e.ParseBool("@randomizedTime");
    }

    public TimedRandomTransition(int time, bool randomizedTime = false, params string[] states)
        : base(states) {
        _time = time;
        _randomized = randomizedTime;
    }

    protected override bool TickCore(Entity host, RealmTime time, ref object state) {
        int cool;

        if (state == null)
            cool = _randomized ? Random.Next(_time) : _time;
        else
            cool = (int) state;

        if (cool <= 0) {
            state = _time;
            SelectedState = Random.Next(TargetStates.Length);
            return true;
        }

        cool -= time.ElapsedMsDelta;
        state = cool;
        return false;
    }
}