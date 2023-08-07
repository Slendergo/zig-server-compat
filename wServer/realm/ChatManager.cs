using System;
using System.Linq;
using common;
using NLog;
using wServer.networking.packets.outgoing;
using wServer.realm.entities;
using wServer.realm.worlds;

namespace wServer.realm
{
    public class ChatManager : IDisposable
    {
        static Logger log = LogManager.GetCurrentClassLogger();

        RealmManager manager;
        public ChatManager(RealmManager manager)
        {
            this.manager = manager;
            manager.InterServer.AddHandler<ChatMsg>(Channel.Chat, HandleChat);
            manager.InterServer.NewServer += AnnounceNewServer;
            manager.InterServer.ServerQuit += AnnounceServerQuit;
        }

        private void AnnounceNewServer(object sender, EventArgs e)
        {
            var networkMsg = (InterServerEventArgs<NetworkMsg>)e;
            if (networkMsg.Content.Info.type == ServerType.Account)
                return;
            Announce($"A new server has come online: {networkMsg.Content.Info.name}", true);
        }

        private void AnnounceServerQuit(object sender, EventArgs e)
        {
            var networkMsg = (InterServerEventArgs<NetworkMsg>)e;
            if (networkMsg.Content.Info.type == ServerType.Account)
                return;
            Announce($"Server, {networkMsg.Content.Info.name}, is no longer online.", true);
        }

        public void Dispose()
        {
            manager.InterServer.NewServer -= AnnounceNewServer;
            manager.InterServer.ServerQuit -= AnnounceServerQuit;
        }

        public void Say(Player src, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            else
            {
                var tp = new Text()
                {
                    Name = (src.Client.Account.Admin ? "@" : "") + src.Name,
                    ObjectId = src.Id,
                    NumStars = src.Stars,
                    BubbleTime = 5,
                    Recipient = "",
                    Txt = text
                };

                SendTextPacket(src, tp, p => !p.Client.Account.IgnoreList.Contains(src.AccountId));
            }
        }

        public bool Local(Player src, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return true;

            var tp = new Text()
            {
                Name = (src.Client.Account.Admin ? "@" : "") + src.Name,
                ObjectId = src.Id,
                NumStars = src.Stars,
                BubbleTime = 5,
                Recipient = "",
                Txt = text
            };

            SendTextPacket(src, tp,
                p => !p.Client.Account.IgnoreList.Contains(src.AccountId) &&
                     p.DistSqr(src) < Player.RadiusSqr);
            return true;
        }

        private void SendTextPacket(Player src, Text tp, Predicate<Player> conditional)
        {
            src.Owner.BroadcastPacketConditional(tp, conditional);
            log.Info($"[{src.Owner.Name}({src.Owner.Id})] <{src.Name}> {tp.Txt}");
        }

        public void Mob(Entity entity, string text)
        {
            if (string.IsNullOrWhiteSpace(text) || entity.Owner == null)
                return;

            var world = entity.Owner;
            var name = entity.ObjectDesc.DisplayId;

            world.BroadcastPacket(new Text()
            {
                ObjectId = entity.Id,
                BubbleTime = 5,
                NumStars = -1,
                Name = $"#{name}",
                Txt = text
            }, null);
            log.Info($"[{world.Name}({world.Id})] <{name}> {text}");
        }

        public void Announce(string text, bool local = false)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            if (local)
            {
                foreach (var i in manager.Clients.Keys
                .Where(x => x.Player != null)
                .Select(x => x.Player))
                {
                    i.AnnouncementReceived(text);
                }
                return;
            }

            manager.InterServer.Publish(Channel.Chat, new ChatMsg()
            {
                Type = ChatType.Announce,
                Inst = manager.InstanceId,
                Text = text
            });
        }

        public bool SendInfo(int target, string text)
        {
            if (String.IsNullOrWhiteSpace(text))
                return true;

            manager.InterServer.Publish(Channel.Chat, new ChatMsg()
            {
                Type = ChatType.Info,
                Inst = manager.InstanceId,
                To = target,
                Text = text
            });
            return true;
        }


        public void Oryx(World world, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            world.BroadcastPacket(new Text()
            {
                BubbleTime = 0,
                NumStars = -1,
                Name = "#Oryx the Mad God",
                Txt = text
            }, null);
            log.Info("[{0}({1})] <Oryx the Mad God> {2}", world.Name, world.Id, text);
        }

        public bool Tell(Player src, string target, string text)
        {
            if (String.IsNullOrWhiteSpace(text))
                return true;

            int id = manager.Database.ResolveId(target);
            if (id == 0) return false;

            if (!manager.Database.AccountLockExists(id))
                return false;

            var acc = manager.Database.GetAccount(id);
            if (acc == null)
                return false;

            manager.InterServer.Publish(Channel.Chat, new ChatMsg()
            {
                Type = ChatType.Tell,
                Inst = manager.InstanceId,
                ObjId = src.Id,
                Stars = src.Stars,
                From = src.Client.Account.AccountId,
                To = id,
                Text = text,
                SrcIP = src.Client.IP
            });
            return true;
        }

        public bool Guild(Player src, string text, bool announce = false)
        {
            if (String.IsNullOrWhiteSpace(text))
                return true;

            manager.InterServer.Publish(Channel.Chat, new ChatMsg()
            {
                Type = (announce) ? ChatType.GuildAnnounce : ChatType.Guild,
                Inst = manager.InstanceId,
                ObjId = src.Id,
                Stars = src.Stars,
                From = src.Client.Account.AccountId,
                To = src.Client.Account.GuildId,
                Text = text
            });
            return true;
        }

        public bool GuildAnnounce(DbAccount acc, string text)
        {
            if (String.IsNullOrWhiteSpace(text))
                return true;

            manager.InterServer.Publish(Channel.Chat, new ChatMsg()
            {
                Type = ChatType.GuildAnnounce,
                Inst = manager.InstanceId,
                From = acc.AccountId,
                To = acc.GuildId,
                Text = text
            });
            return true;
        }

        void HandleChat(object sender, InterServerEventArgs<ChatMsg> e)
        {
            switch (e.Content.Type)
            {
                case ChatType.Tell:
                    {
                        string from = manager.Database.ResolveIgn(e.Content.From);
                        string to = manager.Database.ResolveIgn(e.Content.To);
                        foreach (var i in manager.Clients.Keys
                            .Where(x => x.Player != null)
                            .Where(x => !x.Account.IgnoreList.Contains(e.Content.From))
                            .Where(x => x.Account.AccountId == e.Content.From ||
                                        x.Account.AccountId == e.Content.To && (x.Account.IP == e.Content.SrcIP))
                            .Select(x => x.Player))
                        {
                            i.TellReceived(
                                e.Content.Inst == manager.InstanceId ? e.Content.ObjId : -1,
                                e.Content.Stars, e.Content.Admin, from, to, e.Content.Text);
                        }
                    }
                    break;
                case ChatType.Guild:
                    {
                        string from = manager.Database.ResolveIgn(e.Content.From);
                        foreach (var i in manager.Clients.Keys
                            .Where(x => x.Player != null)
                            .Where(x => !x.Account.IgnoreList.Contains(e.Content.From))
                            .Where(x => x.Account.GuildId > 0)
                            .Where(x => x.Account.GuildId == e.Content.To)
                            .Select(x => x.Player))
                        {
                            i.GuildReceived(
                                e.Content.Inst == manager.InstanceId ? e.Content.ObjId : -1,
                                e.Content.Stars, e.Content.Admin, from, e.Content.Text);
                        }
                    }
                    break;
                case ChatType.GuildAnnounce:
                    {
                        foreach (var i in manager.Clients.Keys
                            .Where(x => x.Player != null)
                            .Where(x => x.Account.GuildId > 0)
                            .Where(x => x.Account.GuildId == e.Content.To)
                            .Select(x => x.Player))
                        {
                            i.GuildReceived(-1, -1, 0, "", e.Content.Text);
                        }
                    }
                    break;
                case ChatType.Announce:
                    {
                        foreach (var i in manager.Clients.Keys
                            .Where(x => x.Player != null)
                            .Select(x => x.Player))
                        {
                            i.AnnouncementReceived(e.Content.Text);
                        }
                    }
                    break;
                case ChatType.Info:
                    {
                        var player = manager.Clients.Keys.Where(c => c.Account.AccountId == e.Content.To).FirstOrDefault();
                        player?.Player.SendInfo(e.Content.Text);
                    }
                    break;
            }
        }
    }
}
