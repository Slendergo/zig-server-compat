using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using common;
using common.resources;
using Newtonsoft.Json;
using wServer.networking;
using wServer.networking.packets.outgoing;
using wServer.realm.setpieces;
using wServer.realm.entities;
using wServer.realm.worlds;
using wServer.realm.worlds.logic;
using NLog;

namespace wServer.realm.commands
{
    class SpawnCommand : Command
    {
        static readonly Logger log = LogManager.GetCurrentClassLogger();

        private struct JsonSpawn
        {
            public string notif;
            public SpawnProperties[] spawns;
        }

        private struct SpawnProperties
        {
            public string name;
            public int? hp;
            public int? size;
            public int? count;
            public int[] x;
            public int[] y;
            public bool? target;
        }

        private const int Delay = 3; // in seconds

        public SpawnCommand() : base("spawn", reqAdmin: true) { }

        protected override bool Process(Player player, RealmTime time, string args)
        {
            args = args.Trim();
            return args.StartsWith("{") ?
                SpawnJson(player, args) :
                SpawnBasic(player, args);
        }

        private bool SpawnJson(Player player, string json)
        {
            var gameData = player.Manager.Resources.GameData;

            JsonSpawn props;
            try
            {
                props = JsonConvert.DeserializeObject<JsonSpawn>(json);
            }
            catch (Exception)
            {
                player.SendError("JSON not formatted correctly!");
                return false;
            }

            if (props.spawns != null)
                foreach (var spawn in props.spawns)
                {
                    if (spawn.name == null)
                    {
                        player.SendError("No mob specified. Every entry needs a name property.");
                        return false;
                    }

                    var objType = GetSpawnObjectType(gameData, spawn.name);
                    if (objType == null)
                    {
                        player.SendError("Unknown entity!");
                        return false;
                    }

                    var desc = gameData.ObjectDescs[objType.Value];

                    var hp = desc.MaxHP;
                    if (spawn.hp > hp && spawn.hp < int.MaxValue)
                        hp = spawn.hp.Value;

                    var size = desc.MinSize;
                    if (spawn.size >= 25 && spawn.size <= 500)
                        size = spawn.size.Value;

                    var count = 1;
                    if (spawn.count > count && spawn.count <= 500)
                        count = spawn.count.Value;

                    int[] x = null;
                    int[] y = null;

                    if (spawn.x != null)
                        x = new int[spawn.x.Length];

                    if (spawn.y != null)
                        y = new int[spawn.y.Length];

                    if (x != null)
                    {
                        for (int i = 0; i < x.Length && i < count; i++)
                        {
                            if (spawn.x[i] > 0 && spawn.x[i] <= player.Owner.Map.Width)
                            {
                                x[i] = spawn.x[i];
                            }

                        }

                    }

                    if (y != null)
                    {
                        for (int i = 0; i < y.Length && i < count; i++)
                        {
                            if (spawn.y[i] > 0 && spawn.y[i] <= player.Owner.Map.Height)
                            {
                                y[i] = spawn.y[i];
                            }

                        }

                    }

                    bool target = false;
                    if (spawn.target != null)
                        target = spawn.target.Value;

                    QueueSpawnEvent(player, count, objType.Value, hp, size, x, y, target);
                }

            if (props.notif != null)
            {
                NotifySpawn(player, props.notif);
            }


            return true;
        }

        private bool SpawnBasic(Player player, string args)
        {
            var gameData = player.Manager.Resources.GameData;

            // split argument
            var index = args.IndexOf(' ');
            int num;
            var name = args;
            if (args.IndexOf(' ') > 0 && int.TryParse(args.Substring(0, args.IndexOf(' ')), out num)) //multi
                name = args.Substring(index + 1);
            else
                num = 1;

            var objType = GetSpawnObjectType(gameData, name);
            if (objType == null)
            {
                player.SendError("Unknown entity!");
                return false;
            }

            if (num <= 0)
            {
                player.SendInfo($"Really? {num} {name}? I'll get right on that...");
                return false;
            }

            var id = player.Manager.Resources.GameData.ObjectTypeToId[objType.Value];

            NotifySpawn(player, id, num);
            QueueSpawnEvent(player, num, objType.Value);
            return true;
        }

        private ushort? GetSpawnObjectType(XmlData gameData, string name)
        {
            ushort objType;
            if (!gameData.IdToObjectType.TryGetValue(name, out objType) ||
                !gameData.ObjectDescs.ContainsKey(objType))
            {
                // no match found, try to get partial match
                var mobs = gameData.IdToObjectType
                    .Where(m => m.Key.ContainsIgnoreCase(name) && gameData.ObjectDescs.ContainsKey(m.Value))
                    .Select(m => gameData.ObjectDescs[m.Value]);

                if (!mobs.Any())
                    return null;

                var maxHp = mobs.Max(e => e.MaxHP);
                objType = mobs.First(e => e.MaxHP == maxHp).ObjectType;
            }

            return objType;
        }

        private void NotifySpawn(Player player, string mob, int? num = null)
        {
            var w = player.Owner;

            var notif = mob;
            if (num != null)
                notif = "Spawning " + ((num > 1) ? num + " " : "") + mob + "...";

            w.BroadcastPacket(new Notification
            {
                Color = new ARGB(0xffff0000),
                ObjectId = player.Id,
                Message = notif
            }, null);

            w.BroadcastPacket(new Text
            {
                Name = $"#{player.Name}",
                NumStars = player.Stars,
                BubbleTime = 0,
                Txt = notif
            }, null);
        }

        private void QueueSpawnEvent(
            Player player,
            int num,
            ushort mobObjectType, int? hp = null, int? size = null,
            int[] x = null, int[] y = null,
            bool? target = false)
        {
            var pX = player.X;
            var pY = player.Y;

            player.Owner.Timers.Add(new WorldTimer(Delay * 1000, (world, t) => // spawn mob in delay seconds
            {
                for (var i = 0; i < num && i < 500; i++)
                {
                    Entity entity;
                    try
                    {
                        entity = Entity.Resolve(world.Manager, mobObjectType);
                    }
                    catch (Exception e)
                    {
                        log.Error(e.ToString());
                        return;
                    }

                    entity.Spawned = true;

                    var enemy = entity as Enemy;
                    if (enemy != null)
                    {
                        if (hp != null)
                        {
                            enemy.HP = hp.Value;
                            enemy.MaximumHP = enemy.HP;
                        }

                        if (size != null)
                            enemy.SetDefaultSize(size.Value);

                        if (target == true)
                            enemy.AttackTarget = player;

                        enemy.ApplyConditionEffect(new ConditionEffect()
                        {
                            Effect = ConditionEffectIndex.Invisible,
                            DurationMS = -1
                        });
                    }

                    var sX = (x != null && i < x.Length) ? x[i] : pX;
                    var sY = (y != null && i < y.Length) ? y[i] : pY;

                    entity.Move(sX, sY);

                    if (!world.Deleted)
                        world.EnterWorld(entity);
                }
            }));
        }
    }


    class ClearSpawnsCommand : Command
    {
        public ClearSpawnsCommand() : base("clearspawn", reqAdmin: true, alias: "cs") { }

        protected override bool Process(Player player, RealmTime time, string args)
        {
            var iterations = 0;
            var lastKilled = -1;
            var removed = 0;
            while (removed != lastKilled)
            {
                lastKilled = removed;
                foreach (var entity in player.Owner.Enemies.Values.Where(e => e.Spawned == true))
                {
                    entity.Death(time);
                    removed++;
                }
                foreach (var entity in player.Owner.StaticObjects.Values.Where(e => e.Spawned == true))
                {
                    player.Owner.LeaveWorld(entity);
                    removed++;
                }
                if (++iterations >= 5)
                    break;
            }

            player.SendInfo($"{removed} spawned entities removed!");
            return true;
        }
    }

    class ClearGravesCommand : Command
    {
        public ClearGravesCommand() : base("cleargraves", reqAdmin: true, alias: "cgraves") { }

        protected override bool Process(Player player, RealmTime time, string args)
        {
            var removed = 0;
            foreach (var entity in player.Owner.StaticObjects.Values)
            {
                if (entity is Container || entity.ObjectDesc == null)
                    continue;

                if (entity.ObjectDesc.ObjectId.StartsWith("Gravestone") && entity.Dist(player) < 15)
                {
                    player.Owner.LeaveWorld(entity);
                    removed++;
                }
            }

            player.SendInfo($"{removed} gravestones removed!");
            return true;
        }
    }

    class ToggleEffCommand : Command
    {
        public ToggleEffCommand() : base("eff", reqAdmin: true) { }

        protected override bool Process(Player player, RealmTime time, string args)
        {
            ConditionEffectIndex effect;
            if (!Enum.TryParse(args, true, out effect))
            {
                player.SendError("Invalid effect!");
                return false;
            }

            var target = player;
            if ((target.ConditionEffects & (ConditionEffects)((ulong)1 << (int)effect)) != 0)
            {
                //remove
                target.ApplyConditionEffect(new ConditionEffect()
                {
                    Effect = effect,
                    DurationMS = 0
                });
            }
            else
            {
                //add
                target.ApplyConditionEffect(new ConditionEffect()
                {
                    Effect = effect,
                    DurationMS = -1
                });
            }
            return true;
        }
    }

    class GuildRankCommand : Command
    {
        public GuildRankCommand() : base("grank", reqAdmin: true) { }

        protected override bool Process(Player player, RealmTime time, string args)
        {
            if (player == null)
                return false;

            // verify argument
            var index = args.IndexOf(' ');
            if (string.IsNullOrWhiteSpace(args) || index == -1)
            {
                player.SendInfo("Usage: /grank <player name> <guild rank>");
                return false;
            }

            // get command args
            var playerName = args.Substring(0, index);
            var rank = args.Substring(index + 1).IsInt() ? args.Substring(index + 1).ToInt32() : RankNumberFromName(args.Substring(index + 1));
            if (rank == -1)
            {
                player.SendError("Unknown rank!");
                return false;
            }
            else if (rank % 10 != 0)
            {
                player.SendError("Valid ranks are multiples of 10!");
                return false;
            }

            // get player account
            if (Database.GuestNames.Contains(playerName, StringComparer.InvariantCultureIgnoreCase))
            {
                player.SendError("Cannot rank the unnamed...");
                return false;
            }
            var id = player.Manager.Database.ResolveId(playerName);
            var acc = player.Manager.Database.GetAccount(id);
            if (id == 0 || acc == null)
            {
                player.SendError("Account not found!");
                return false;
            }

            // change rank
            acc.GuildRank = rank;
            acc.FlushAsync();

            // send out success notifications
            player.SendInfo($"You changed the guildrank of player {acc.Name} to {rank}.");
            var target = player.Manager.Clients.Keys.SingleOrDefault(p => p.Account.AccountId == acc.AccountId);
            if (target?.Player == null) return true;
            target.Player.GuildRank = rank;
            target.Player.SendInfo("Your guild rank was changed");
            return true;
        }

        private int RankNumberFromName(string val)
        {
            switch (val.ToLower())
            {
                case "initiate":
                    return 0;
                case "member":
                    return 10;
                case "officer":
                    return 20;
                case "leader":
                    return 30;
                case "founder":
                    return 40;
            }
            return -1;
        }
    }

    class GimmeCommand : Command
    {
        public GimmeCommand() : base("gimme", reqAdmin: true, alias: "give") { }

        protected override bool Process(Player player, RealmTime time, string args)
        {
            var gameData = player.Manager.Resources.GameData;

            ushort objType;

            // allow both DisplayId and Id for query
            if (!gameData.DisplayIdToObjectType.TryGetValue(args, out objType))
            {
                if (!gameData.IdToObjectType.TryGetValue(args, out objType))
                {
                    player.SendError("Unknown item type!");
                    return false;
                }
            }

            if (!gameData.Items.ContainsKey(objType))
            {
                player.SendError("Not an item!");
                return false;
            }

            var item = gameData.Items[objType];
            var availableSlot = player.Inventory.GetAvailableInventorySlot(item);
            if (availableSlot != -1)
            {
                player.Inventory[availableSlot] = item;
                return true;
            }

            player.SendError("Not enough space in inventory!");
            return false;
        }
    }

    class TpPosCommand : Command
    {
        public TpPosCommand() : base("tpPos", reqAdmin: true, alias: "goto") { }

        protected override bool Process(Player player, RealmTime time, string args)
        {
            string[] coordinates = args.Split(' ');
            if (coordinates.Length != 2)
            {
                player.SendError("Invalid coordinates!");
                return false;
            }

            int x, y;
            if (!int.TryParse(coordinates[0], out x) ||
                !int.TryParse(coordinates[1], out y))
            {
                player.SendError("Invalid coordinates!");
                return false;
            }

            player.SetNewbiePeriod();
            player.TeleportPosition(time, x + 0.5f, y + 0.5f, true);
            return true;
        }
    }

    class SetpieceCommand : Command
    {
        public SetpieceCommand() : base("setpiece", reqAdmin: true) { }

        protected override bool Process(Player player, RealmTime time, string setPiece)
        {
            if (String.IsNullOrWhiteSpace(setPiece))
            {
                var type = typeof(ISetPiece);
                var types = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(s => s.GetTypes())
                    .Where(p => type.IsAssignableFrom(p) && !p.IsAbstract);
                var msg = types.Aggregate(
                    "Valid SetPieces: ", (c, p) => c + (p.Name) + ", ");
                player.SendInfo(msg.Substring(0, msg.Length - 2) + ".");
                return false;
            }

            if (!player.Owner.Name.Equals("Nexus"))
            {
                try
                {
                    ISetPiece piece = (ISetPiece)Activator.CreateInstance(Type.GetType(
                    "wServer.realm.setpieces." + setPiece, true, true));
                    piece.RenderSetPiece(player.Owner, new IntPoint((int)player.X + 1, (int)player.Y + 1));
                    return true;
                }
                catch (Exception)
                {
                    player.SendError("Invalid SetPiece.");
                    return false;
                }
            }
            else
            {
                player.SendInfo("/setpiece not allowed in Nexus.");
                return false;
            }
        }
    }
  
    class KillAllCommand : Command
    {
        public KillAllCommand() : base("killAll", reqAdmin: true, alias: "ka") { }

        protected override bool Process(Player player, RealmTime time, string args)
        {
            var iterations = 0;
            var lastKilled = -1;
            var killed = 0;
            while (killed != lastKilled)
            {
                lastKilled = killed;
                foreach (var i in player.Owner.Enemies.Values.Where(e =>
                    e.ObjectDesc != null && e.ObjectDesc.ObjectId != null
                    && e.ObjectDesc.Enemy && e.ObjectDesc.ObjectId != "Tradabad Nexus Crier"
                    && e.ObjectDesc.ObjectId.ContainsIgnoreCase(args)))
                {
                    i.Spawned = true;
                    i.Death(time);
                    killed++;
                }
                if (++iterations >= 5)
                    break;
            }

            player.SendInfo($"{killed} enemy killed!");
            return true;
        }
    }

    class KickCommand : Command
    {
        public KickCommand() : base("kick", reqAdmin: true) { }

        protected override bool Process(Player player, RealmTime time, string args)
        {
            foreach (var i in player.Manager.Clients.Keys)
            {
                if (i.Account.Name.EqualsIgnoreCase(args))
                {
                    i.Disconnect();
                    player.SendInfo("Player disconnected!");
                    return true;
                }
            }
            player.SendError($"Player '{args}' could not be found!");
            return false;
        }
    }

    class GetQuestCommand : Command
    {
        public GetQuestCommand() : base("getQuest", reqAdmin: true) { }

        protected override bool Process(Player player, RealmTime time, string args)
        {
            if (player.Quest == null)
            {
                player.SendError("Player does not have a quest!");
                return false;
            }
            player.SendInfo("Quest location: (" + player.Quest.X + ", " + player.Quest.Y + ")");
            return true;
        }
    }

    class OryxSayCommand : Command
    {
        public OryxSayCommand() : base("oryxSay", reqAdmin: true, alias: "osay") { }

        protected override bool Process(Player player, RealmTime time, string args)
        {
            player.Manager.Chat.Oryx(player.Owner, args);
            return true;
        }
    }

    class AnnounceCommand : Command
    {
        public AnnounceCommand() : base("announce", reqAdmin: true) { }

        protected override bool Process(Player player, RealmTime time, string args)
        {
            player.Manager.Chat.Announce(args);
            return true;
        }
    }

    class SummonCommand : Command
    {
        public SummonCommand() : base("summon", reqAdmin: true) { }

        protected override bool Process(Player player, RealmTime time, string args)
        {

            foreach (var i in player.Owner.Players)
            {
                if (i.Value.Name.EqualsIgnoreCase(args))
                {
                    i.Value.Teleport(time, player.Id, true);
                    i.Value.SendInfo($"You've been summoned by {player.Name}.");
                    player.SendInfo("Player summoned!");
                    return true;
                }
            }
            player.SendError($"Player '{args}' could not be found!");
            return false;
        }
    }

    class SummonAllCommand : Command
    {
        public SummonAllCommand() : base("summonall", reqAdmin: true) { }

        protected override bool Process(Player player, RealmTime time, string args)
        {
            foreach (var i in player.Owner.Players)
            {
                i.Value.Teleport(time, player.Id, true);
                i.Value.SendInfo($"You've been summoned by {player.Name}.");
            }

            player.SendInfo("All players summoned!");
            return true;
        }
    }

    class KillPlayerCommand : Command
    {
        public KillPlayerCommand() : base("killPlayer", reqAdmin: true) { }

        protected override bool Process(Player player, RealmTime time, string args)
        {
            foreach (var i in player.Manager.Clients.Keys)
            {
                if (i.Account.Name.EqualsIgnoreCase(args))
                {
                    i.Player.HP = 0;
                    i.Player.Death(player.Name);
                    player.SendInfo("Player killed!");
                    return true;
                }
            }
            player.SendError($"Player '{args}' could not be found!");
            return false;
        }
    }

    class SizeCommand : Command
    {
        public SizeCommand() : base("size", reqAdmin: true) { }

        protected override bool Process(Player player, RealmTime time, string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                player.SendError("Usage: /size <positive integer>. Using 0 will restore the default size for the sprite.");
                return false;
            }

            var size = Utils.GetInt(args);
            var min = 0;
            var max = 500;
            if (size < min && size != 0 || size > max)
            {
                player.SendError($"Invalid size. Size needs to be within the range: {min}-{max}. Use 0 to reset size to default.");
                return false;
            }

            var target = player;
            if (size == 0)
                target.RestoreDefaultSize();
            else
                target.Size = size;

            return true;
        }
    }

    class RebootCommand : Command
    {
        // Command actually closes the program.
        // An external program is used to monitor the world server existance.
        // If !exist it automatically restarts it.

        public RebootCommand() : base("reboot", reqAdmin: true) { }

        protected override bool Process(Player player, RealmTime time, string name)
        {
            var manager = player.Manager;
            var servers = manager.InterServer.GetServerList();

            // display help if no argument supplied
            if (string.IsNullOrEmpty(name))
            {
                var sb = new StringBuilder("Current servers available for rebooting:\n");
                for (var i = 0; i < servers.Length; i++)
                {
                    if (i != 0)
                        sb.Append(", ");

                    sb.Append($"{servers[i].name} [{servers[i].type}]");
                }

                player.SendInfo("Usage: /reboot < server name | $all | $wserver | $account >");
                player.SendInfo(sb.ToString());
                return true;
            }

            // attempt to find server match
            foreach (var server in servers)
            {
                if (!server.name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                RebootServer(player, 0, server.instanceId);
                player.SendInfo("Reboot command sent.");
                return true;
            }

            // no match found, attempt to match special cases
            switch (name.ToLower())
            {
                case "$all":
                    RebootServer(player, 29000, servers
                        .Select(s => s.instanceId)
                        .ToArray());
                    player.SendInfo("Reboot command sent.");
                    return true;
                case "$wserver":
                    RebootServer(player, 0, servers
                        .Where(s => s.type == ServerType.World)
                        .Select(s => s.instanceId)
                        .ToArray());
                    player.SendInfo("Reboot command sent.");
                    return true;
                case "$account":
                    RebootServer(player, 0, servers
                        .Where(s => s.type == ServerType.Account)
                        .Select(s => s.instanceId)
                        .ToArray());
                    player.SendInfo("Reboot command sent.");
                    return true;
            }

            player.SendInfo("Server not found.");
            return false;
        }

        private void RebootServer(Player issuer, int delay, params string[] instanceIds)
        {
            foreach (var instanceId in instanceIds)
            {
                issuer.Manager.InterServer.Publish(Channel.Control, new ControlMsg()
                {
                    Type = ControlType.Reboot,
                    TargetInst = instanceId,
                    Issuer = issuer.Name,
                    Delay = delay
                });
            }
        }
    }

    class ReSkinCommand : Command
    {
        public ReSkinCommand() : base("reskin", reqAdmin: true) { }

        protected override bool Process(Player player, RealmTime time, string args)
        {
            var skins = player.Manager.Resources.GameData.Skins
                .Where(d => d.Value.PlayerClassType == player.ObjectType)
                .Select(d => d.Key)
                .ToArray();

            if (String.IsNullOrEmpty(args))
            {
                var choices = skins.ToCommaSepString();
                player.SendError("Usage: /reskin <positive integer>");
                player.SendError("Choices: " + choices);
                return false;
            }

            var skin = (ushort)Utils.GetInt(args);

            if (skin != 0 && !skins.Contains(skin))
            {
                player.SendError("Error setting skin. Either the skin type doesn't exist or the skin is for another class.");
                return false;
            }

            var skinDesc = player.Manager.Resources.GameData.Skins[skin];
            var size = skinDesc.Size;

            player.SetDefaultSkin(skin);
            player.SetDefaultSize(size);
            return true;
        }
    }

    class MaxCommand : Command
    {
        public MaxCommand() : base("max", reqAdmin: true) { }

        protected override bool Process(Player player, RealmTime time, string args)
        {
            var pd = player.Manager.Resources.GameData.Classes[player.ObjectType];

            player.Stats.Base[0] = pd.Stats[0].MaxValue;
            player.Stats.Base[1] = pd.Stats[1].MaxValue;
            player.Stats.Base[2] = pd.Stats[2].MaxValue;
            player.Stats.Base[3] = pd.Stats[3].MaxValue;
            player.Stats.Base[4] = pd.Stats[4].MaxValue;
            player.Stats.Base[5] = pd.Stats[5].MaxValue;
            player.Stats.Base[6] = pd.Stats[6].MaxValue;
            player.Stats.Base[7] = pd.Stats[7].MaxValue;

            player.SendInfo("Your character stats have been maxed.");
            return true;
        }
    }

    class TpQuestCommand : Command
    {
        public TpQuestCommand() : base("tq", reqAdmin: true) { }

        protected override bool Process(Player player, RealmTime time, string args)
        {
            if (player.Quest == null)
            {
                player.SendError("Player does not have a quest!");
                return false;
            }

            player.SetNewbiePeriod();
            player.TeleportPosition(time, player.Quest.RealX, player.Quest.RealY, true);
            player.SendInfo("Teleported to Quest Location: (" + player.Quest.X + ", " + player.Quest.Y + ")");
            return true;
        }
    }

    class MuteCommand : Command
    {
        private static readonly Regex CmdParams = new Regex(@"^(\w+)( \d+)?$", RegexOptions.IgnoreCase);

        private readonly RealmManager _manager;

        public MuteCommand(RealmManager manager) : base("mute", reqAdmin: true)
        {
            _manager = manager;
            _manager.DbEvents.Expired += HandleUnMute;
        }

        protected override bool Process(Player player, RealmTime time, string args)
        {
            var match = CmdParams.Match(args);
            if (!match.Success)
            {
                player?.SendError("Usage: /mute <player name> <time out in minutes>\\n" +
                                 "Time parameter is optional. If left out player will be muted until unmuted.");
                return false;
            }

            // gather arguments
            var name = match.Groups[1].Value;
            var id = _manager.Database.ResolveId(name);
            var acc = _manager.Database.GetAccount(id);
            int timeout;
            if (string.IsNullOrEmpty(match.Groups[2].Value))
            {
                timeout = -1;
            }
            else
            {
                int.TryParse(match.Groups[2].Value, out timeout);
            }

            // run through checks
            if (id == 0 || acc == null)
            {
                player?.SendError("Account not found!");
                return false;
            }
            if (acc.IP == null)
            {
                player?.SendError("Account has no associated IP address. Player must login at least once before being muted.");
                return false;
            }
            if (acc.IP.Equals(player?.Client.Account.IP))
            {
                player?.SendError("Mute failed. That action would cause yourself to be muted (IPs are the same).");
                return false;
            }
            if (acc.Admin)
            {
                player?.SendError("Cannot mute other admins.");
                return false;
            }

            // mute player if currently connected
            foreach (var client in _manager.Clients.Keys
                        .Where(c => c.Player != null && c.IP.Equals(acc.IP) && !c.Player.Client.Account.Admin))
            {
                client.Player.Muted = true;
            }

            if (player != null)
            {
                if (timeout > 0)
                    _manager.Chat.SendInfo(id, "You have been muted by " + player.Name + " for " + timeout + " minutes.");
                else
                    _manager.Chat.SendInfo(id, "You have been muted by " + player.Name + ".");
            }

            // mute ip address
            if (timeout < 0)
            {
                _manager.Database.Mute(acc.IP);
                player?.SendInfo(name + " successfully muted indefinitely.");
            }
            else
            {
                _manager.Database.Mute(acc.IP, TimeSpan.FromMinutes(timeout));
                player?.SendInfo(name + " successfully muted for " + timeout + " minutes.");
            }

            return true;
        }

        private void HandleUnMute(object entity, DbEventArgs expired)
        {
            var key = expired.Message;

            if (!key.StartsWith("mutes:"))
                return;

            foreach (var client in _manager.Clients.Keys.Where(c =>
                c.Player != null && c.IP.Equals(key.Substring(6)) && !c.Player.Client.Account.Admin))
            {
                client.Player.Muted = false;
                client.Player.SendInfo("You are no longer muted. Please do not spam. Thank you.");
            }
        }
    }

    class UnMuteCommand : Command
    {
        public UnMuteCommand() : base("unmute", reqAdmin: true) { }

        protected override bool Process(Player player, RealmTime time, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                player.SendError("Usage: /unmute <player name>");
                return false;
            }

            // gather needed info
            var id = player.Manager.Database.ResolveId(name);
            var acc = player.Manager.Database.GetAccount(id);

            // run checks
            if (id == 0 || acc == null)
            {
                player.SendError("Account not found!");
                return false;
            }
            if (acc.IP == null)
            {
                player.SendError("Account has no associated IP address. Player must login at least once before being unmuted.");
                return false;
            }

            // unmute ip address
            player.Manager.Database.IsMuted(acc.IP).ContinueWith(t =>
            {
                if (!t.IsCompleted)
                {
                    player.SendInfo("Db access error while trying to unmute.");
                    return;
                }

                if (t.Result)
                {
                    player.Manager.Database.Mute(acc.IP, TimeSpan.FromSeconds(1));
                    player.SendInfo(name + " successfully unmuted.");
                }
                else
                {
                    player.SendInfo(name + " wasn't muted...");
                }
            });

            // expire event will handle unmuting of connected players
            return true;
        }
    }

    class BanAccountCommand : Command
    {
        public BanAccountCommand() : base("ban", reqAdmin: true) { }

        protected override bool Process(Player player, RealmTime time, string args)
        {
            BanInfo bInfo;
            if (args.StartsWith("{"))
            {
                bInfo = Utils.FromJson<BanInfo>(args);
            }
            else
            {
                bInfo = new BanInfo();

                // validate command
                var rgx = new Regex(@"^(\w+) (.+)$");
                var match = rgx.Match(args);
                if (!match.Success)
                {
                    player.SendError("Usage: /ban <account id or name> <reason>");
                    return false;
                }

                // get info from args
                bInfo.Name = match.Groups[1].Value;
                if (!int.TryParse(bInfo.Name, out bInfo.accountId))
                {
                    bInfo.accountId = player.Manager.Database.ResolveId(bInfo.Name);
                }
                bInfo.banReasons = match.Groups[2].Value;
                bInfo.banLiftTime = -1;
            }

            // run checks
            if (Database.GuestNames.Any(n => n.ToLower().Equals(bInfo.Name?.ToLower())))
            {
                player.SendError("If you specify a player name to ban, the name needs to be unique.");
                return false;
            }
            if (bInfo.accountId == 0)
            {
                player.SendError("Account not found...");
                return false;
            }
            if (string.IsNullOrWhiteSpace(bInfo.banReasons))
            {
                player.SendError("A reason must be provided.");
                return false;
            }
            var acc = player.Manager.Database.GetAccount(bInfo.accountId);

            // ban player + disconnect if currently connected
            player.Manager.Database.Ban(bInfo.accountId, bInfo.banReasons, bInfo.banLiftTime);
            var target = player.Manager.Clients.Keys
                .SingleOrDefault(c => c.Account != null && c.Account.AccountId == bInfo.accountId);
            target?.Disconnect();

            player.SendInfo(!string.IsNullOrEmpty(bInfo.Name) ?
                $"{bInfo.Name} successfully banned." :
                "Ban successful.");
            return true;
        }

        private class BanInfo
        {
            public int accountId;
            public string Name;
            public string banReasons;
            public int banLiftTime;
        }
    }

    class BanIPCommand : Command
    {
        public BanIPCommand() : base("banip", reqAdmin: true, alias: "ipban") { }

        protected override bool Process(Player player, RealmTime time, string args)
        {
            var manager = player.Manager;
            var db = manager.Database;

            // validate command
            var rgx = new Regex(@"^(\w+) (.+)$");
            var match = rgx.Match(args);
            if (!match.Success)
            {
                player.SendError("Usage: /banip <account id or name> <reason>");
                return false;
            }

            // get info from args
            int id;
            var idstr = match.Groups[1].Value;
            if (!int.TryParse(idstr, out id))
            {
                id = db.ResolveId(idstr);
            }
            var reason = match.Groups[2].Value;

            // run checks
            if (Database.GuestNames.Any(n => n.ToLower().Equals(idstr.ToLower())))
            {
                player.SendError("If you specify a player name to ban, the name needs to be unique.");
                return false;
            }
            if (id == 0)
            {
                player.SendError("Account not found...");
                return false;
            }
            if (string.IsNullOrWhiteSpace(reason))
            {
                player.SendError("A reason must be provided.");
                return false;
            }
            var acc = db.GetAccount(id);
            if (string.IsNullOrEmpty(acc.IP))
            {
                player.SendError("Failed to ip ban player. IP not logged...");
                return false;
            }
            if (player.AccountId != acc.AccountId && acc.IP.Equals(player.Client.Account.IP))
            {
                player.SendError("IP ban failed. That action would cause yourself to be banned (IPs are the same).");
                return false;
            }

            // ban
            db.Ban(acc.AccountId, reason);
            db.BanIp(acc.IP, reason);

            // disconnect currently connected
            var targets = manager.Clients.Keys.Where(c => c.IP.Equals(acc.IP));
            foreach (var t in targets)
                t.Disconnect();

            // send notification
            player.SendInfo($"Banned {acc.Name} (both account and ip).");
            return true;
        }
    }

    class UnBanAccountCommand : Command
    {
        public UnBanAccountCommand() : base("unban", reqAdmin: true) { }

        protected override bool Process(Player player, RealmTime time, string args)
        {
            var db = player.Manager.Database;

            // validate command
            var rgx = new Regex(@"^(\w+)$");
            if (!rgx.IsMatch(args))
            {
                player.SendError("Usage: /unban <account id or name>");
                return false;
            }

            // get info from args
            int id;
            if (!int.TryParse(args, out id))
                id = db.ResolveId(args);

            // run checks
            if (id == 0)
            {
                player.SendError("Account doesn't exist...");
                return false;
            }

            var acc = db.GetAccount(id);

            // unban
            var banned = db.UnBan(id);
            var ipBanned = acc.IP != null && db.UnBanIp(acc.IP);

            // send notification
            if (!banned && !ipBanned)
            {
                player.SendInfo($"{acc.Name} wasn't banned...");
                return true;
            }
            if (banned && ipBanned)
            {
                player.SendInfo($"Success! {acc.Name}'s account and IP no longer banned.");
                return true;
            }
            if (banned)
            {
                player.SendInfo($"Success! {acc.Name}'s account no longer banned.");
                return true;
            }

            player.SendInfo($"Success! {acc.Name}'s IP no longer banned.");
            return true;
        }
    }

    class ClearInvCommand : Command
    {
        public ClearInvCommand() : base("clearinv", reqAdmin: true) { }

        protected override bool Process(Player player, RealmTime time, string args)
        {
            for (int i = 4; i < 12; i++)
                player.Inventory[i] = null;
            player.SendInfo("Inventory Cleared.");
            return true;
        }
    }

    class CloseRealmCommand : Command
    {
        public CloseRealmCommand() : base("closerealm", reqAdmin: true) { }

        protected override bool Process(Player player, RealmTime time, string args)
        {
            var gw = player.Manager.Worlds[World.Realm] as Realm;

            if (gw == null)
            {
                player.SendError("An undefined error occurred.");
                return false;
            }

            if (gw.IsClosing())
            {
                player.SendError("Realm already closing.");
                return false;
            }

            gw.CloseRealm();
            return true;
        }
    }

    class QuakeCommand : Command
    {
        public QuakeCommand() : base("quake", reqAdmin: true) { }

        protected override bool Process(Player player, RealmTime time, string worldName)
        {
            var worldProtoData = player.Manager.Resources.Worlds.Data;

            if (String.IsNullOrWhiteSpace(worldName))
            {
                var msg = worldProtoData.Aggregate(
                    "Valid World Names: ", (c, p) => c + ((!p.Value.setpiece) ? (p.Key + ", ") : ""));
                player.SendInfo(msg.Substring(0, msg.Length - 2) + ".");
                return false;
            }

            if (player.Owner is Nexus)
            {
                player.SendError("Cannot use /quake in Nexus.");
                return false;
            }

            var worldNameProper =
                player.Manager.Resources.Worlds.Data.FirstOrDefault(
                    p => p.Key.Equals(worldName, StringComparison.InvariantCultureIgnoreCase)).Key;

            ProtoWorld proto;
            if (worldNameProper == null || (proto = worldProtoData[worldNameProper]).setpiece)
            {
                player.SendError("Invalid world.");
                return false;
            }

            World world;
            if (proto.persist)
                world = player.Manager.Worlds[proto.id];
            else
            {
                DynamicWorld.TryGetWorld(proto, player.Client, out world);
                world = player.Manager.AddWorld(world ?? new World(proto));
            }

            player.Owner.QuakeToWorld(world);
            return true;
        }
    }

    class VisitCommand : Command
    {
        public VisitCommand() : base("visit", reqAdmin: true) { }

        protected override bool Process(Player player, RealmTime time, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                player.SendInfo("Usage: /visit <player name>");
                return true;
            }

            var target = player.Manager.Clients.Keys
                .SingleOrDefault(c => c.Account != null &&
                                      c.Account.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

            if (target?.Player?.Owner == null)
            {
                player.SendError("Player not found!");
                return false;
            }

            var owner = target.Player.Owner;
            player.Client.Reconnect(new Reconnect()
            {
                Host = "",
                GameId = owner.Id,
                Name = owner.SBName
            });
            return true;
        }
    }

    class LinkCommand : Command
    {
        public LinkCommand() : base("link", reqAdmin: true) { }

        protected override bool Process(Player player, RealmTime time, string args)
        {
            if (player?.Owner == null)
                return false;

            var world = player.Owner;
            if (world.Id < 0 || (!player.Client.Account.Admin && !(world is Test)))
            {
                player.SendError("Forbidden.");
                return false;
            }

            if (!player.Manager.Monitor.AddPortal(world.Id))
            {
                player.SendError("Link already exists.");
                return false;
            }

            return true;
        }
    }

    class UnLinkCommand : Command
    {
        public UnLinkCommand() : base("unlink", reqAdmin: true) { }

        protected override bool Process(Player player, RealmTime time, string args)
        {
            if (player?.Owner == null)
                return false;

            var world = player.Owner;
            if (world.Id < 0 || (!player.Client.Account.Admin && !(world is Test)))
            {
                player.SendError("Forbidden.");
                return false;
            }

            if (!player.Manager.Monitor.RemovePortal(player.Owner.Id))
                player.SendError("Link not found.");
            else
                player.SendInfo("Link removed.");

            return true;
        }
    }

    class Level20Command : Command
    {
        public Level20Command() : base("level20", reqAdmin: true, alias: "l20") { }

        protected override bool Process(Player player, RealmTime time, string args)
        {
            if (player.Level < 20)
            {
                player.Experience = Player.GetLevelExp(20);
                player.Level = 20;
                player.CalculateFame();
                return true;
            }

            return false;
        }
    }

    class RenameCommand : Command
    {
        public RenameCommand() : base("rename", reqAdmin: true) { }

        protected override bool Process(Player player, RealmTime time, string args)
        {
            var index = args.IndexOf(' ');
            if (string.IsNullOrWhiteSpace(args) || index == -1)
            {
                player.SendInfo("Usage: /rename <player name> <new player name>");
                return false;
            }

            var playerName = args.Substring(0, index);
            var newPlayerName = args.Substring(index + 1);

            var id = player.Manager.Database.ResolveId(playerName);
            if (id == 0)
            {
                player.SendError("Player account not found!");
                return false;
            }

            if (newPlayerName.Length < 3 || newPlayerName.Length > 15 || !newPlayerName.All(char.IsLetter) ||
                Database.GuestNames.Contains(newPlayerName, StringComparer.InvariantCultureIgnoreCase))
            {
                player.SendError("New name is invalid. Must be between 3-15 char long and contain only letters.");
                return false;
            }

            string lockToken = null;
            var key = Database.NAME_LOCK;
            var db = player.Manager.Database;

            try
            {
                while ((lockToken = db.AcquireLock(key)) == null) ;

                if (db.Conn.HashExists("names", newPlayerName.ToUpperInvariant()))
                {
                    player.SendError("Name already taken");
                    return false;
                }

                var acc = db.GetAccount(id);
                if (acc == null)
                {
                    player.SendError("Account doesn't exist.");
                    return false;
                }

                using (var l = db.Lock(acc))
                    if (db.LockOk(l))
                    {
                        while (!db.RenameIGN(acc, newPlayerName, lockToken)) ;
                        player.SendInfo("Rename successful.");
                    }
                    else
                        player.SendError("Account in use.");
            }
            finally
            {
                if (lockToken != null)
                    db.ReleaseLock(key, lockToken);
            }

            return true;
        }
    }

    class UnnameCommand : Command
    {
        public UnnameCommand() : base("unname", reqAdmin: true) { }

        protected override bool Process(Player player, RealmTime time, string args)
        {
            if (string.IsNullOrWhiteSpace(args))
            {
                player.SendInfo("Usage: /unname <player name>");
                return false;
            }

            var playerName = args;

            var id = player.Manager.Database.ResolveId(playerName);
            if (id == 0)
            {
                player.SendError("Player account not found!");
                return false;
            }

            string lockToken = null;
            var key = Database.NAME_LOCK;
            var db = player.Manager.Database;

            try
            {
                while ((lockToken = db.AcquireLock(key)) == null) ;

                var acc = db.GetAccount(id);
                if (acc == null)
                {
                    player.SendError("Account doesn't exist.");
                    return false;
                }

                using (var l = db.Lock(acc))
                    if (db.LockOk(l))
                    {
                        while (!db.UnnameIGN(acc, lockToken)) ;
                        player.SendInfo("Account succesfully unnamed.");
                    }
                    else
                        player.SendError("Account in use.");
            }
            finally
            {
                if (lockToken != null)
                    db.ReleaseLock(key, lockToken);
            }

            return true;
        }
    }

    class CompactLOHCommand : Command
    {
        public CompactLOHCommand() : base("compactLOH", reqAdmin: true, listCommand: false) { }

        protected override bool Process(Player player, RealmTime time, string name)
        {
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
            return true;
        }
    }
}
