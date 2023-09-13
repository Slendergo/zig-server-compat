using common.resources;
using wServer.realm.entities;
using wServer.networking.packets;
using wServer.networking.packets.incoming;
using wServer.networking.packets.outgoing;
using NLog;

namespace wServer.networking.handlers;

class PlayerShootHandler : PacketHandlerBase<PlayerShoot>
{
    public override C2SPacketId C2SId => C2SPacketId.PlayerShoot;
    private static readonly Logger CheatLog = LogManager.GetCurrentClassLogger();

    protected override void HandlePacket(Client client, PlayerShoot packet)
    {
        // fix for a handle on a null player
        if(client?.Player == null)
            return;
        
        //client.Manager.Logic.AddPendingAction(t => Handle(client.Player, packet, t));
        Handle(client.Player, packet);
    }

    private void Handle(Player player, PlayerShoot packet)
    {
        if (!player.Manager.Resources.GameData.Items.TryGetValue(packet.ContainerType, out var item))
        {
            CheatLog.Info($"Attempted to use invalid item in PlayerShoot ({player.Name}:{player.AccountId}): Not found");
            player.Client.Random.NextInt();
            return;
        }

        if (item != player.Inventory[0])
        {
            CheatLog.Info($"Attempted to use invalid item in PlayerShoot ({player.Name}:{player.AccountId}): Wrong slot");
            player.Client.Random.NextInt();
            return;
        }
        
        var prjDesc = item.Projectiles[0]; //Assume only one

        // validate
        var result = player.ValidatePlayerShoot(item, packet.Time);
        if (result != PlayerShootStatus.OK)
        {
            CheatLog.Info($"PlayerShoot validation failure ({player.Name}:{player.AccountId}): {result}");
            player.Client.Random.NextInt();
            return;
        }


        // create projectile and show other players
        var prj = player.PlayerShootProjectile(
            packet.BulletId, prjDesc, item.ObjectType,
            packet.Time, packet.StartingPos, packet.Angle);

        player.Owner.EnterWorld(prj);

        player.Owner.BroadcastPacketNearby(new AllyShoot()
        {
            OwnerId = player.Id,
            Angle = packet.Angle,
            ContainerType = packet.ContainerType,
            BulletId = packet.BulletId
        }, player, player);
        player.FameCounter.Shoot(prj);
    }
}