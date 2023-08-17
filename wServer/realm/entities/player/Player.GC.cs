using common.resources;
using NLog;
using System.Collections.Concurrent;

namespace wServer.realm.entities;

partial class Player
{
    // todo add any more if needed

    public void CleanupReconnect()
    {
        TickId = 0;
        _pingTime = -1;
        _pongTime = -1;
        LastClientTime = -1;
        LastServerTime = -1;
        _shootAckTimeout.Clear();
        _updateAckTimeout.Clear();
        _gotoAckTimeout.Clear();
        _move.Clear();
        _clientTimeLog.Clear();
        _serverTimeLog.Clear();
    }
}