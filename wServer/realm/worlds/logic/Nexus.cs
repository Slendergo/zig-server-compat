using common.resources;
using wServer.networking;

namespace wServer.realm.worlds.logic;

class Nexus : World
{
    public Nexus(ProtoWorld proto, Client client = null) : base(proto)
    {
    }

    protected override void Init()
    {
        base.Init();

        var monitor = Manager.Monitor;
        foreach (var i in Manager.Worlds.Values)
        {
            if (i is Realm)
            {
                monitor.AddPortal(i.Id);
                continue;
            }

            if (i.Id >= 0)
                continue;
        }
    }
}