using wServer.realm;
using wServer.realm.entities;
using wServer.networking.packets;
using wServer.networking.packets.incoming;

namespace wServer.networking.handlers;

public class PlayerMessage
{
    public Player Player { get; private set; }
    public string Message { get; private set; }
    public long Time { get; private set; }

    public PlayerMessage(Player player, RealmTime time, string msg)
    {
        Player = player;
        Message = msg;
        Time = time.TotalElapsedMs;
    }
}

class PlayerTextHandler : PacketHandlerBase<PlayerText>
{
    public override C2SPacketId C2SId => C2SPacketId.PlayerText;

    protected override void HandlePacket(Client client, PlayerText packet)
    {
        client.Manager.Logic.AddPendingAction(t => Handle(client.Player, t, packet.Text));
    }

    void Handle(Player player, RealmTime time, string text)
    {
        if (player?.Owner == null || text.Length > 512)
            return;

        var manager = player.Manager;

        // check for commands before other checks
        if (text[0] == '/')
            _ = manager.Commands.Execute(player, time, text);
        else
        {
            if (!player.NameChosen)
            {
                player.SendErrorText("Please choose a name before chatting.");
                return;
            }

            if (player.Muted)
            {
                player.SendErrorText("Muted. You can not talk at this time.");
                return;
            }

            // save message for mob behaviors
            player.Owner.ChatReceived(player, text);

            ChatManager.Say(player, text);
        }
    }
}