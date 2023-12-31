﻿namespace GameServer.realm.entities.player;

partial class Player {
    // todo add any more if needed

    public void CleanupReconnect() 
    {
        ClientEntities.Dispose();
        ClientStatics.Clear();
        
        ToAdd.Clear();
        ToRemove.Clear();
        TilesToAdd.Clear();

        StatUpdates.Clear();
        UpdateStatuses = null;

        TickId = 0;
        _pingTime = -1;
        _pongTime = -1;
        LastClientTime = -1;
        LastServerTime = -1;

        _move.Clear();
        _shootAckTimeout.Clear();
        _updateAckTimeout.Clear();
        _clientTimeLog.Clear();
        _serverTimeLog.Clear();
    }
}