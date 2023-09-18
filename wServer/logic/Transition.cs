using wServer.realm;

namespace wServer.logic;

public abstract class Transition : IStateChildren {
    [ThreadStatic] private static Random _rand;

    protected readonly string[] TargetStates;
    protected int SelectedState;

    public Transition(params string[] targetStates) {
        TargetStates = targetStates;
    }

    public State[] TargetState { get; private set; }

    protected static Random Random => _rand ?? (_rand = new Random());

    public bool Tick(Entity host, RealmTime time) {
        host.StateStorage.TryGetValue(this, out var state);

        var ret = TickCore(host, time, ref state);
        if (ret)
            host.SwitchTo(TargetState[SelectedState]);

        if (state == null)
            host.StateStorage.Remove(this);
        else
            host.StateStorage[this] = state;
        return ret;
    }

    protected abstract bool TickCore(Entity host, RealmTime time, ref object state);

    internal void Resolve(IDictionary<string, State> states) {
        var numStates = TargetStates.Length;
        TargetState = new State[numStates];
        for (var i = 0; i < numStates; i++)
            TargetState[i] = states[TargetStates[i]];
    }
}