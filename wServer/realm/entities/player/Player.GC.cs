using common.resources;
using NLog;

namespace wServer.realm.entities;

partial class Player
{
    // todo add any more if needed

    public void CleanupReconnect()
    {
        TickId = 0;
    }
}