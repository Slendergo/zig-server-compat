using System.Xml.Linq;
using Shared;
using GameServer.realm;

namespace GameServer.logic.behaviors;

internal class OrderOnce : Behavior {
    private readonly ushort _children;
    //State storage: none

    private readonly double _range;
    private readonly string _targetStateName;
    private State _targetState;

    public OrderOnce(XElement e) {
        _range = e.ParseFloat("@range");
        _children = GetObjType(e.ParseString("@children"));
        _targetStateName = e.ParseString("@targetState");
    }

    public OrderOnce(double range, string children, string targetState) {
        _range = range;
        _children = GetObjType(children);
        _targetStateName = targetState;
    }

    private static State FindState(State state, string name) {
        if (state.Name == name)
            return state;

        return state.States
            .Select(i => FindState(i, name))
            .FirstOrDefault(s => s != null);
    }

    protected override void OnStateEntry(Entity host, RealmTime time, ref object state) {
        if (_targetState == null)
            _targetState = FindState(host.Manager.Behaviors.Definitions[_children].Item1, _targetStateName);

        foreach (var i in host.GetNearestEntities(_range, _children))
            if (!i.CurrentState.Is(_targetState))
                i.SwitchTo(_targetState);
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state) { }
}