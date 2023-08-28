using wServer.realm.entities;
using wServer.networking.packets;
using wServer.networking.packets.incoming;
using wServer.realm.worlds.logic;
using common;
using wServer.realm.worlds;

namespace wServer.networking.handlers;

class UsePortalHandler : PacketHandlerBase<UsePortal>
{
    private readonly int[] _realmPortals = { 0x0704, 0x070e, 0x071c, 0x703, 0x070d, 0x0d40 };

    public override C2SPacketId C2SId => C2SPacketId.UsePortal;

    protected override void HandlePacket(Client client, UsePortal packet)
    {
        //client.Manager.Logic.AddPendingAction(t => Handle(client, packet));
        Handle(client, packet);
    }

    private void Handle(Client client, UsePortal packet)
    {
        var player = client.Player;
        if (player?.Owner == null || IsTest(client))
            return;

        var entity = player.Owner.GetEntity(packet.ObjectId);
        if (entity == null)
            return;

        HandlePortal(player, entity as Portal);
    }

    private void HandlePortal(Player player, Portal portal)
    {
        if (portal == null || !portal.Usable)
            return;

        using (TimedLock.Lock(portal.CreateWorldLock))
        {
            var world = portal.WorldInstance;

            // special portal case lookup
            if (world == null && _realmPortals.Contains(portal.ObjectType))
            {
                world = player.Manager.GetRandomRealm();
                if (world == null)
                    return;
            }

            if (world is RealmOfTheMadGod && !player.Manager.Resources.GameData.ObjectTypeToId[portal.ObjectDesc.ObjectType].Contains("Cowardice"))
                player.FameCounter.CompleteDungeon(player.Owner.IdName);

            if (world != null)
            {
                player.Client.Reconnect(world.IdName, world.Id);
                return;
            }

            // dynamic case lookup
            if (portal.CreateWorldTask == null || portal.CreateWorldTask.IsCompleted)
                portal.CreateWorldTask = Task.Factory.StartNew(() => portal.CreateWorld(player))
                    .ContinueWith(e => Log.Error(e.Exception.InnerException.ToString()), TaskContinuationOptions.OnlyOnFaulted);
            portal.WorldInstanceSet += player.Reconnect;
        }
    }
}