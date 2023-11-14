using Shared;
using Shared.resources;
using GameServer.realm.entities.player;
using GameServer.realm.worlds;
using GameServer.realm.worlds.logic;

namespace GameServer.realm.entities;

public class Portal : StaticObject
{
    private readonly SV<bool> _usable;

    public readonly object CreateWorldLock = new();

    public Portal(RealmManager manager, ushort objType, int? life)
        : base(manager, ValidatePortal(manager, objType), life, false, true, false)
    {
        _usable = new SV<bool>(this, StatsType.PortalUsable, true);
        Locked = manager.Resources.GameData.Portals[ObjectType].Locked;
    }

    public bool Usable
    {
        get => _usable.GetValue();
        set => _usable.SetValue(value);
    }

    public bool Locked { get; private set; }
    public Task CreateWorldTask { get; set; }
    public World WorldInstance { get; set; }
    public event EventHandler<World> WorldInstanceSet;

    private static ushort ValidatePortal(RealmManager manager, ushort objType)
    {
        var portals = manager.Resources.GameData.Portals;
        if (!portals.ContainsKey(objType))
        {
            SLog.Warn($"Portal {objType.To4Hex()} does not exist. Using Portal of Cowardice.");
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
        var isInstanced = false;

        World world = null;
        foreach (var p in Program.Resources.GameData.WorldTemplates.Values.Where(p => p.Portals.Contains(ObjectId)))
        {
            if (p.Specialized == SpecializedDungeonType.GuildHall)
            {
                if (string.IsNullOrEmpty(player.Guild))
                {
                    player.SendErrorText("You are not in a guild.");
                    return;
                }

                foreach (var w in Manager.Worlds.Values)
                {
                    if (w is not GuildHall hall || hall.GuildId != player.Client.Account.GuildId)
                        continue;

                    player.Client.Reconnect(hall.IdName, hall.Id);
                    return;
                }
            }

            isInstanced = p.Instanced;
            world = player.Manager.CreateNewWorld(p, player.Client);

            break;
        }

        // get nexus if failed to make a world 
        if (world == null)
        {
            world = Manager.GetWorld(World.Nexus);
            player.SendErrorText("Unable to find world, sent to nexus");
            SLog.Warn($"Unable to find world for: {ObjectId}");
        }

        if (!isInstanced)
            WorldInstance = world;
        WorldInstanceSet?.Invoke(this, world);
    }
}