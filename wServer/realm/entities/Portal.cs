using common;
using common.resources;
using wServer.realm.worlds;
using wServer.realm.worlds.logic;

namespace wServer.realm.entities;

public class Portal : StaticObject
{
    public Portal(RealmManager manager, ushort objType, int? life)
        : base(manager, ValidatePortal(manager, objType), life, false, true, false)
    {
        _usable = new SV<bool>(this, StatsType.PortalUsable, true);
        Locked = manager.Resources.GameData.Portals[ObjectType].Locked;
    }

    private readonly SV<bool> _usable;

    public bool Usable
    {
        get { return _usable.GetValue(); }
        set { _usable.SetValue(value); }
    }

    public bool Locked { get; private set; }

    public readonly object CreateWorldLock = new();
    public Task CreateWorldTask { get; set; }
    public World WorldInstance { get; set; }
    public event EventHandler<World> WorldInstanceSet;

    private static ushort ValidatePortal(RealmManager manager, ushort objType)
    {
        var portals = manager.Resources.GameData.Portals;
        if (!portals.ContainsKey(objType))
        {
            Log.Warn($"Portal {objType.To4Hex()} does not exist. Using Portal of Cowardice.");
            objType = 0x0703; // default to Portal of Cowardice
        }

        return objType;
    }

    protected override void ExportStats(IDictionary<StatsType, object> stats)
    {
        stats[StatsType.PortalUsable] = Usable;
        base.ExportStats(stats);
    }

    public override bool HitByProjectile(Projectile projectile, RealmTime time)
    {
        return false;
    }

    public void CreateWorld(Player player)
    {
        World world = null;
        foreach (var p in Program.Resources.GameData.WorldTemplates.Values.Where(p => p.Portals.Contains(ObjectId)))
        {
            if(p.Specialized == SpeicalizedDungeonType.GuildHall)
            {
                if (string.IsNullOrEmpty(player.Guild))
                {
                    player.SendErrorText("You are not in a guild.");
                    return;
                }

                foreach (var w in Manager.Worlds.Values)
                {
                    if (w is not GuildHall || (w as GuildHall).GuildId != player.Client.Account.GuildId)
                        continue;
                    player.Client.Reconnect(w.IdName, w.Id);
                    return;
                }
            }

            var newWorld = player.Manager.CreateNewWorld(p, player.Client);
            if (!p.Instanced)
                world = newWorld;
            break;
        }

        // get nexus if failed to make a world 
        if(world == null)
        {
            world = Manager.GetWorld(World.Nexus);
            player.SendErrorText("Unable to find world, sent to nexus");
        }

        WorldInstance = world;
        WorldInstanceSet?.Invoke(this, world);
    }
}