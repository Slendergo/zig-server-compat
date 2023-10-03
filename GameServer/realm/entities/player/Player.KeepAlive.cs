using System.Collections.Concurrent;
using GameServer.realm.worlds.logic;

namespace GameServer.realm.entities.player;

public partial class Player {
    private const int PingPeriod = 3000;
    public const int DcThresold = 12000;
    private readonly ConcurrentQueue<long> _clientTimeLog = new();
    private readonly ConcurrentQueue<int> _move = new();
    private readonly ConcurrentQueue<long> _serverTimeLog = new();

    private readonly ConcurrentQueue<long> _shootAckTimeout = new();
    private readonly ConcurrentQueue<long> _updateAckTimeout = new();

    private int _cnt;

    private long _latSum;

    private long _pingTime = -1;
    private long _pongTime = -1;

    private long _sum;

    public long LastClientTime = -1;
    public long LastServerTime = -1;
    public long TimeMap { get; private set; }
    public int Latency { get; private set; }

    private bool KeepAlive(RealmTime time) {
        if (_pingTime == -1) {
            _pingTime = time.TotalElapsedMs - PingPeriod;
            _pongTime = time.TotalElapsedMs;
        }

        // check for disconnect timeout
        if (time.TotalElapsedMs - _pongTime > DcThresold) {
            Client.Disconnect("Connection timeout. (KeepAlive)");
            return false;
        }

        // check for shootack timeout
        if (_shootAckTimeout.TryPeek(out var timeout))
            if (time.TotalElapsedMs > timeout) {
                Client.Disconnect("Connection timeout. (ShootAck)");
                return false;
            }

        // check for updateack timeout
        if (_updateAckTimeout.TryPeek(out timeout))
            if (time.TotalElapsedMs > timeout) {
                Client.Disconnect("Connection timeout. (UpdateAck)");
                return false;
            }

        if (time.TotalElapsedMs - _pingTime < PingPeriod)
            return true;

        // send ping
        _pingTime = time.TotalElapsedMs;
        Client.SendPing((int) time.TotalElapsedMs);
        return UpdateOnPing();
    }

    public void Pong(RealmTime time, int serial, long pongTime) {
        _cnt++;

        _sum += time.TotalElapsedMs - pongTime;
        TimeMap = _sum / _cnt;

        _latSum += (time.TotalElapsedMs - serial) / 2;
        Latency = (int) _latSum / _cnt;

        _pongTime = time.TotalElapsedMs;
    }

    private bool UpdateOnPing() {
        // renew account lock
        try {
            if (!Manager.Database.RenewLock(Client.Account))
                Client.Disconnect("RenewLock failed. (Pong)");
        }
        catch {
            Client.Disconnect("RenewLock failed. (Timeout)");
            return false;
        }

        // save character
        if (!(Owner is Test)) {
            SaveToCharacter();
            Client.Character.FlushAsync();
        }

        return true;
    }

    public long C2STime(long clientTime) {
        return clientTime + TimeMap;
    }

    public long S2CTime(long serverTime) {
        return serverTime - TimeMap;
    }

    public void AwaitShootAck(long serverTime) {
        _shootAckTimeout.Enqueue(serverTime + DcThresold);
    }

    public void ShootAckReceived() {
        long ignored;
        if (!_shootAckTimeout.TryDequeue(out ignored)) Client.Disconnect("One too many ShootAcks");
    }

    public void AwaitUpdateAck(long serverTime) {
        _updateAckTimeout.Enqueue(serverTime + DcThresold);
    }

    public void UpdateAckReceived() {
        if (!_updateAckTimeout.TryDequeue(out _))
            Client.Disconnect("One too many UpdateAcks");
    }
    
    public void AwaitMove(int tickId) {
        _move.Enqueue(tickId);
    }

    public void MoveReceived(RealmTime time, int moveTickId, long moveTime) {
        if (!_move.TryDequeue(out var tickId)) {
            Client.Disconnect("One too many MovePackets");
            return;
        }

        if (tickId != moveTickId) {
            Client.Disconnect("[NewTick -> Move] TickIds don't match");
            return;
        }

        if (moveTickId > TickId) {
            Client.Disconnect("[NewTick -> Move] Invalid tickId");
            return;
        }

        var lastClientTime = LastClientTime;
        var lastServerTime = LastServerTime;
        LastClientTime = moveTime;
        LastServerTime = time.TotalElapsedMs;

        if (lastClientTime == -1)
            return;

        _clientTimeLog.Enqueue(moveTime - lastClientTime);
        _serverTimeLog.Enqueue((int) (time.TotalElapsedMs - lastServerTime));

        if (_clientTimeLog.Count < 30)
            return;

        if (_clientTimeLog.Count > 30) {
            _clientTimeLog.TryDequeue(out _);
            _serverTimeLog.TryDequeue(out _);
        }

        // calculate average
        var clientDeltaAvg = _clientTimeLog.Sum() / _clientTimeLog.Count;
        var serverDeltaAvg = _serverTimeLog.Sum() / _serverTimeLog.Count;
        var dx = clientDeltaAvg > serverDeltaAvg
            ? clientDeltaAvg - serverDeltaAvg
            : serverDeltaAvg - clientDeltaAvg;
        if (dx > 15)
            Log.Debug(
                $"TickId: {tickId}, Client Delta: {_clientTimeLog.Sum() / _clientTimeLog.Count}, Server Delta: {_serverTimeLog.Sum() / _serverTimeLog.Count}");
    }
}