using common;
using NLog;
using wServer.realm.entities;
using wServer.realm.worlds;

namespace wServer.realm;

public class ChatManager : IDisposable {
    private static Logger log = LogManager.GetCurrentClassLogger();

    private RealmManager manager;

    public ChatManager(RealmManager manager) {
        this.manager = manager;
        manager.InterServer.AddHandler<ChatMsg>(Channel.Chat, HandleChat);
        manager.InterServer.NewServer += AnnounceNewServer;
        manager.InterServer.ServerQuit += AnnounceServerQuit;
    }

    public void Dispose() {
        manager.InterServer.NewServer -= AnnounceNewServer;
        manager.InterServer.ServerQuit -= AnnounceServerQuit;
    }

    private void AnnounceNewServer(object sender, EventArgs e) {
        var networkMsg = (InterServerEventArgs<NetworkMsg>) e;
        if (networkMsg.Content.Info.type == ServerType.Account)
            return;
        Announce($"A new server has come online: {networkMsg.Content.Info.name}", true);
    }

    private void AnnounceServerQuit(object sender, EventArgs e) {
        var networkMsg = (InterServerEventArgs<NetworkMsg>) e;
        if (networkMsg.Content.Info.type == ServerType.Account)
            return;
        Announce($"Server, {networkMsg.Content.Info.name}, is no longer online.", true);
    }

    public static void Say(Player src, string text) {
        if (string.IsNullOrWhiteSpace(text))
            return;

        var name = (src.Client.Account.Admin ? "@" : "") + src.Name;

        foreach (var p in src.Owner.Players.Values)
            if (!p.Client.Account.IgnoreList.Contains(src.AccountId))
                p.Client.SendText($"#{name}", src.Id, src.Stars, 5, string.Empty, text);

        log.Info($"[{src.Owner.IdName}({src.Owner.Id})] <{src.Name}> {text}");
    }

    public static bool Local(Player src, string text) {
        if (string.IsNullOrWhiteSpace(text))
            return true;

        var name = (src.Client.Account.Admin ? "@" : "") + src.Name;

        foreach (var p in src.Owner.Players.Values)
            if (!p.Client.Account.IgnoreList.Contains(src.AccountId) && p.DistSqr(src) < Player.RadiusSqr)
                p.Client.SendText($"#{name}", src.Id, src.Stars, 5, string.Empty, text);

        log.Info($"[{src.Owner.IdName}({src.Owner.Id})] <{src.Name}> {text}");
        return true;
    }

    public void Mob(Entity entity, string text) {
        if (string.IsNullOrWhiteSpace(text) || entity.Owner == null)
            return;

        var world = entity.Owner;
        var name = entity.ObjectDesc.DisplayId;

        foreach (var player in world.Players.Values)
            player.Client.SendText($"#{name}", entity.Id, -1, 5, string.Empty, text);

        log.Info($"[{world.IdName}({world.Id})] <{name}> {text}");
    }

    public void Announce(string text, bool local = false) {
        if (string.IsNullOrWhiteSpace(text))
            return;

        if (local) {
            foreach (var i in manager.Clients.Keys
                         .Where(x => x.Player != null)
                         .Select(x => x.Player))
                i.AnnouncementReceived(text);
            return;
        }

        manager.InterServer.Publish(Channel.Chat, new ChatMsg {
            Type = ChatType.Announce,
            Inst = manager.InstanceId,
            Text = text
        });
    }

    public bool SendInfo(int target, string text) {
        if (string.IsNullOrWhiteSpace(text))
            return true;

        manager.InterServer.Publish(Channel.Chat, new ChatMsg {
            Type = ChatType.Info,
            Inst = manager.InstanceId,
            To = target,
            Text = text
        });
        return true;
    }


    public static void Oryx(World world, string text) {
        if (string.IsNullOrWhiteSpace(text))
            return;

        foreach (var player in world.Players.Values)
            player.Client.SendText("#Oryx the Mad God", 0, -1, 5, string.Empty, text);

        log.Info("[{0}({1})] <Oryx the Mad God> {2}", world.IdName, world.Id, text);
    }

    public bool Tell(Player src, string target, string text) {
        if (string.IsNullOrWhiteSpace(text))
            return true;

        var id = manager.Database.ResolveId(target);
        if (id == 0) return false;

        if (!manager.Database.AccountLockExists(id))
            return false;

        var acc = manager.Database.GetAccount(id);
        if (acc == null)
            return false;

        manager.InterServer.Publish(Channel.Chat, new ChatMsg {
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

    public bool Guild(Player src, string text, bool announce = false) {
        if (string.IsNullOrWhiteSpace(text))
            return true;

        manager.InterServer.Publish(Channel.Chat, new ChatMsg {
            Type = announce ? ChatType.GuildAnnounce : ChatType.Guild,
            Inst = manager.InstanceId,
            ObjId = src.Id,
            Stars = src.Stars,
            From = src.Client.Account.AccountId,
            To = src.Client.Account.GuildId,
            Text = text
        });
        return true;
    }

    public bool GuildAnnounce(DbAccount acc, string text) {
        if (string.IsNullOrWhiteSpace(text))
            return true;

        manager.InterServer.Publish(Channel.Chat, new ChatMsg {
            Type = ChatType.GuildAnnounce,
            Inst = manager.InstanceId,
            From = acc.AccountId,
            To = acc.GuildId,
            Text = text
        });
        return true;
    }

    private void HandleChat(object sender, InterServerEventArgs<ChatMsg> e) {
        switch (e.Content.Type) {
            case ChatType.Tell: {
                var from = manager.Database.ResolveIgn(e.Content.From);
                var to = manager.Database.ResolveIgn(e.Content.To);
                foreach (var i in manager.Clients.Keys
                             .Where(x => x.Player != null)
                             .Where(x => !x.Account.IgnoreList.Contains(e.Content.From))
                             .Where(x => x.Account.AccountId == e.Content.From ||
                                         (x.Account.AccountId == e.Content.To && x.Account.IP == e.Content.SrcIP))
                             .Select(x => x.Player))
                    i.TellReceived(
                        e.Content.Inst == manager.InstanceId ? e.Content.ObjId : -1,
                        e.Content.Stars, from, to, e.Content.Text);
            }
                break;
            case ChatType.Guild: {
                var from = manager.Database.ResolveIgn(e.Content.From);
                foreach (var i in manager.Clients.Keys
                             .Where(x => x.Player != null)
                             .Where(x => !x.Account.IgnoreList.Contains(e.Content.From))
                             .Where(x => x.Account.GuildId > 0)
                             .Where(x => x.Account.GuildId == e.Content.To)
                             .Select(x => x.Player))
                    i.GuildReceived(
                        e.Content.Inst == manager.InstanceId ? e.Content.ObjId : -1,
                        e.Content.Stars, from, e.Content.Text);
            }
                break;
            case ChatType.GuildAnnounce: {
                foreach (var i in manager.Clients.Keys
                             .Where(x => x.Player != null)
                             .Where(x => x.Account.GuildId > 0)
                             .Where(x => x.Account.GuildId == e.Content.To)
                             .Select(x => x.Player))
                    i.GuildReceived(-1, -1, "", e.Content.Text);
            }
                break;
            case ChatType.Announce: {
                foreach (var i in manager.Clients.Keys
                             .Where(x => x.Player != null)
                             .Select(x => x.Player))
                    i.AnnouncementReceived(e.Content.Text);
            }
                break;
            case ChatType.Info: {
                var player = manager.Clients.Keys.Where(c => c.Account.AccountId == e.Content.To).FirstOrDefault();
                player?.Player.SendInfo(e.Content.Text);
            }
                break;
        }
    }
}