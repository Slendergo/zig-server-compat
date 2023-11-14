using Shared.resources;
using NLog;
using Shared;

namespace GameServer.realm.entities.player;

public enum PlayerShootStatus {
    OK,
    ITEM_MISMATCH,
    COOLDOWN_STILL_ACTIVE,
    NUM_PROJECTILE_MISMATCH,
    CLIENT_TOO_SLOW,
    CLIENT_TOO_FAST
}

public class TimeCop {
    private readonly int _capacity;
    private readonly int[] _clientDeltaLog;
    private readonly int[] _serverDeltaLog;
    private int _clientElapsed;
    private int _count;
    private int _index;
    private int _lastClientTime;
    private int _lastServerTime;
    private int _serverElapsed;

    public TimeCop(int capacity = 20) {
        _capacity = capacity;
        _clientDeltaLog = new int[_capacity];
        _serverDeltaLog = new int[_capacity];
    }

    public void Push(int clientTime, int serverTime) {
        var dtClient = 0;
        var dtServer = 0;
        if (_count != 0) {
            dtClient = clientTime - _lastClientTime;
            dtServer = serverTime - _lastServerTime;
        }

        _count++;
        _index = (_index + 1) % _capacity;
        _clientElapsed += dtClient - _clientDeltaLog[_index];
        _serverElapsed += dtServer - _serverDeltaLog[_index];
        _clientDeltaLog[_index] = dtClient;
        _serverDeltaLog[_index] = dtServer;
        _lastClientTime = clientTime;
        _lastServerTime = serverTime;
    }

    public int LastClientTime() {
        return _lastClientTime;
    }

    public int LastServerTime() {
        return _lastServerTime;
    }

    /*
        a return value of 1 means client time is in sync with server time
        less than 1 means client time is slower than server time
        greater than 1 means client time is faster than server
    */
    public float TimeDiff() {
        if (_count < _capacity)
            return 1;

        return (float) _clientElapsed / _serverElapsed;
    }
}

partial class Player {
    private const float MaxTimeDiff = 1.08f;
    private const float MinTimeDiff = 0.92f;

    private long LastAttackTime = -1;
    private int Shots;

    public PlayerShootStatus ValidatePlayerShoot(Item item, long time) {
        if (item != Inventory[0])
            return PlayerShootStatus.ITEM_MISMATCH;

        //start

        if (time == LastAttackTime) {
            if (++Shots > item.NumProjectiles)
                return PlayerShootStatus.NUM_PROJECTILE_MISMATCH;
        }
        else {
            var attackPeriod = (int) (1.0 / Stats.GetAttackFrequency() * 1.0 / item.RateOfFire);
            if (time < LastAttackTime + attackPeriod)
                return PlayerShootStatus.COOLDOWN_STILL_ACTIVE;
            LastAttackTime = time;
            Shots = 1;
        }

        //end

        return PlayerShootStatus.OK;
    }

    public bool IsNoClipping() {
        if (Owner == null || !TileOccupied(X, Y) && !TileFullOccupied(X, Y))
            return false;

        SLog.Info($"{Name} is walking on an occupied tile.");
        return true;
    }
}