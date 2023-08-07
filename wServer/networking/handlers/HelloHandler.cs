using common;
using NLog;
using wServer.networking.packets;
using wServer.networking.packets.incoming;
using wServer.networking.packets.outgoing;
using wServer.realm;

namespace wServer.networking.handlers
{
    class HelloHandler : PacketHandlerBase<Hello>
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public override PacketId ID => PacketId.HELLO;

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
            {
                return;
            }

            // log ip
            client.Manager.Database.LogAccountByIp(client.IP, acc.AccountId);
            acc.IP = client.IP;
            acc.FlushAsync();

            client.Account = acc;
            client.Manager.Logic.AddPendingAction(t =>
                client.Manager.ConMan.Add(new ConInfo(client, packet)));
        }

        private DbAccount VerifyConnection(Client client, Hello packet)
        {
            var version = client.Manager.Config.serverSettings.version;
            if (!version.Equals(packet.BuildVersion))
            {
                client.SendFailure(version, Failure.ClientUpdateNeeded);
                return null;
            }

            DbAccount acc;
            var s1 = client.Manager.Database.Verify(packet.GUID, packet.Password, out acc);
            if (s1 == LoginStatus.AccountNotExists)
            {
                var s2 = client.Manager.Database.Register(packet.GUID, packet.Password, true, out acc);
                if (s2 != RegisterStatus.OK)
                {
                    client.SendFailure("Bad Login", Failure.MessageWithDisconnect);
                    return null;
                }
            }
            else if (s1 == LoginStatus.InvalidCredentials)
            {
                client.SendFailure("Bad Login", Failure.MessageWithDisconnect);
                return null;
            }

            if (acc.Banned)
            {
                client.SendFailure("Account banned.", Failure.MessageWithDisconnect);
                Log.Info("{0} ({1}) tried to log in. Account Banned.",
                    acc.Name, client.IP);
                return null;
            }

            if (client.Manager.Database.IsIpBanned(client.IP))
            {
                client.SendFailure("IP banned.", Failure.MessageWithDisconnect);
                Log.Info("{0} ({1}) tried to log in. IP Banned.",
                    acc.Name, client.IP);
                return null;
            }

            if (!acc.Admin && client.Manager.Config.serverInfo.adminOnly)
            {
                client.SendFailure("Admin Only Server", Failure.MessageWithDisconnect);
                return null;
            }

            return acc;
        }
    }
}
