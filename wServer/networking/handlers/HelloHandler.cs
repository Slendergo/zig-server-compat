using common;
using NLog;
using wServer.networking.packets;
using wServer.networking.packets.incoming;
using wServer.networking.packets.outgoing;
using wServer.realm;
using wServer.realm.entities;

namespace wServer.networking.handlers;

class HelloHandler : PacketHandlerBase<Hello>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public override C2SPacketId C2SId => C2SPacketId.Hello;

    protected override void HandlePacket(Client client, Hello packet)
    {
        //client.Manager.Logic.AddPendingAction(t => Handle(client, packet));
        Handle(client, packet);
    }

    private void Handle(Client client, Hello packet)
    {
        // validate connection eligibility and get acc info
        var acc = VerifyConnection(client, packet);
        if (acc == null)
            return;

        // log ip
        client.Manager.Database.LogAccountByIp(client.IP, acc.AccountId);
        acc.IP = client.IP;
        acc.FlushAsync();

        client.Account = acc;
        if (packet.CreateCharacter) {
            var status = client.Manager.Database.CreateCharacter(acc, packet.SkinType, packet.CharacterType,
                out var character);
            switch (status) {
                case CreateStatus.ReachCharLimit:
                    client.SendFailure("Too many characters");
                    return;
                case CreateStatus.SkinUnavailable:
                    client.SendFailure("Skin unavailable");
                    return;
                case CreateStatus.Locked:
                    client.SendFailure("Class locked");
                    return;
            }

            client.Character = character;
            client.Player = new Player(client);
            packet.CharId = (short) character.CharId;
        }
        
        ConnectManager.Connect(client, packet.GameId, packet.CharId);
    }

    private static DbAccount VerifyConnection(Client client, Hello packet)
    {
        var version = client.Manager.Config.serverSettings.version;
        if (!version.Equals(packet.BuildVersion))
        {
            client.SendFailure(version, Failure.ClientUpdateNeeded);
            return null;
        }

        var s1 = client.Manager.Database.Verify(packet.GUID, packet.Password, out var acc);
        if (s1 is LoginStatus.AccountNotExists or LoginStatus.InvalidCredentials)
        {
            client.SendFailure("Bad Login");
            return null;
        }

        if (acc.Banned)
        {
            client.SendFailure("Account banned.");
            Log.Info("{0} ({1}) tried to log in. Account Banned.",
                acc.Name, client.IP);
            return null;
        }

        if (client.Manager.Database.IsIpBanned(client.IP))
        {
            client.SendFailure("IP banned.");
            Log.Info("{0} ({1}) tried to log in. IP Banned.",
                acc.Name, client.IP);
            return null;
        }

        if (!acc.Admin && client.Manager.Config.serverInfo.adminOnly)
        {
            client.SendFailure("Admin Only Server");
            return null;
        }

        return acc;
    }
}