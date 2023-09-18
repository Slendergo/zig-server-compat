﻿using common;
using NLog;
using wServer.realm;

namespace wServer.logic;

public abstract class Behavior : IStateChildren {
    protected static readonly Logger Log = LogManager.GetCurrentClassLogger();

    [ThreadStatic] private static Random rand;

    protected static Random Random {
        get {
            if (rand == null) rand = new Random();
            return rand;
        }
    }

    public void Tick(Entity host, RealmTime time) {
        if (!host.StateStorage.TryGetValue(this, out var state))
            state = null;

        TickCore(host, time, ref state);

        if (state == null)
            host.StateStorage.Remove(this);
        else
            host.StateStorage[this] = state;
    }

    protected abstract void TickCore(Entity host, RealmTime time, ref object state);

    public void OnStateEntry(Entity host, RealmTime time) {
        if (!host.StateStorage.TryGetValue(this, out var state))
            state = null;

        OnStateEntry(host, time, ref state);

        if (state == null)
            host.StateStorage.Remove(this);
        else
            host.StateStorage[this] = state;
    }

    protected virtual void OnStateEntry(Entity host, RealmTime time, ref object state) { }

    public void OnStateExit(Entity host, RealmTime time) {
        if (!host.StateStorage.TryGetValue(this, out var state))
            state = null;

        OnStateExit(host, time, ref state);

        if (state == null)
            host.StateStorage.Remove(this);
        else
            host.StateStorage[this] = state;
    }

    protected virtual void OnStateExit(Entity host, RealmTime time, ref object state) { }

    protected internal virtual void Resolve(State parent) { }

    public static ushort GetObjType(string id) {
        if (BehaviorDb.InitGameData.IdToObjectType.TryGetValue(id, out var ret))
            return ret;

        ret = BehaviorDb.InitGameData.IdToObjectType["Pirate"];
        Log.Warn($"Object type '{id}' not found. Using Pirate ({ret.To4Hex()}).");
        return ret;
    }
}