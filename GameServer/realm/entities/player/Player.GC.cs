namespace GameServer.realm.entities.player;

partial class Player {
    // todo add any more if needed

    public void CleanupReconnect() {
        _clientEntities.Dispose();
        _clientStatic.Clear();
        _newObjects = null;
        _newStatics.Clear();
        _removedObjects = null;
        _statUpdates.Clear();
        _tiles = null;
        _updateStatuses = null;

        TickId = 0;
        _pingTime = -1;
        _pongTime = -1;
        LastClientTime = -1;
        LastServerTime = -1;
        _shootAckTimeout.Clear();
        _updateAckTimeout.Clear();
        _move.Clear();
        _clientTimeLog.Clear();
        _serverTimeLog.Clear();
    }
}