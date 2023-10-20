using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Shared;
using Shared.resources;
using GameServer.realm;
using GameServer.realm.entities;
using GameServer.realm.entities.player;
using GameServer.realm.entities.vendors;
using GameServer.realm.worlds.logic;
using NLog;
using static GameServer.PacketUtils;

namespace GameServer;

public enum BuyResultType {
    Success = 0,
    NotEnoughGold = 1,
    NotEnoughFame = 2,
    NotEnoughGuildFame = 3
}

public enum C2SPacketId : byte {
    Unknown = 0,
    AcceptTrade = 1,
    AoeAck = 2,
    Buy = 3,
    CancelTrade = 4,
    ChangeGuildRank = 5,
    ChangeTrade = 6,
    ChooseName = 8,
    CreateGuild = 10,
    EditAccountList = 11,
    EnemyHit = 12,
    Escape = 13,
    GroundDamage = 15,
    GuildInvite = 16,
    GuildRemove = 17,
    Hello = 18,
    InvDrop = 19,
    InvSwap = 20,
    JoinGuild = 21,
    Move = 23,
    OtherHit = 24,
    PlayerHit = 25,
    PlayerShoot = 26,
    PlayerText = 27,
    Pong = 28,
    RequestTrade = 29,
    Reskin = 30,
    ShootAck = 32,
    SquareHit = 33,
    Teleport = 34,
    UpdateAck = 35,
    UseItem = 36,
    UsePortal = 37
}

public enum S2CPacketId : byte {
    Unknown = 0,
    AccountList = 1,
    AllyShoot = 2,
    Aoe = 3,
    BuyResult = 4,
    ClientStat = 5,
    CreateSuccess = 6,
    Damage = 7,
    Death = 8,
    EnemyShoot = 9,
    Failure = 10,
    File = 11,
    GlobalNotification = 12,
    GoTo = 13,
    GuildResult = 14,
    InvResult = 15,
    InvitedToGuild = 16,
    MapInfo = 17,
    NameResult = 18,
    NewTick = 19,
    Notification = 20,
    Pic = 21,
    Ping = 22,
    PlaySound = 23,
    QuestObjId = 24,
    Reconnect = 25,
    ServerPlayerShoot = 26,
    ShowEffect = 27,
    Text = 28,
    TradeAccepted = 29,
    TradeChanged = 30,
    TradeDone = 31,
    TradeRequested = 32,
    TradeStart = 33,
    Update = 34
}

public class Client {
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly Random InvRand = new();
    private readonly int[] _realmPortals = { 0x0704, 0x070e, 0x071c, 0x703, 0x070d, 0x0d40 };

    internal readonly object DcLock = new();

    private readonly Memory<byte> ReceiveMem;
    private readonly Memory<byte> SendMem;
    private Server _server;

    public DbAccount Account;
    public DbChar Character;

    public int Id;
    public string IP;
    public Player Player;

    public WRandom SeededRandom;
    public uint Seed;

    private object SendLock = new();

    public Socket Socket;

    public Client(Server server, RealmManager manager) {
        _server = server;
        Manager = manager;
        ReceiveMem = GC.AllocateArray<byte>(RECV_BUFFER_LEN, true).AsMemory();
        SendMem = GC.AllocateArray<byte>(SEND_BUFFER_LEN, true).AsMemory();
    }

    public RealmManager Manager { get; }
    public bool Reconnecting { get; private set; }
    
    public void Reset(Socket socket) {
        Account = null;
        Character = null;
        Player = null;
        SeededRandom = null;
        Socket = socket;
        Reconnecting = false;
        try {
            IP = ((IPEndPoint) socket.RemoteEndPoint).Address.ToString();
        }
        catch (Exception) {
            IP = "";
        }

        Log.Trace("Received client @ {0}.", IP);
        Receive();
    }

    public void Reconnect(string name, int gameId) {
        if (Account == null) {
            Disconnect("Tried to reconnect an client with a null account...");
            return;
        }

        Reconnecting = true;
        Log.Trace("Reconnecting client ({0}) @ {1} to {2}...", Account.Name, IP, name);
        ConnectManager.Reconnect(this, gameId);
        Reconnecting = false;
    }

    public void Disconnect(string reason = "") {
        lock (DcLock) {
            SendFailure(0, reason);

            if (!string.IsNullOrEmpty(reason))
                Log.Warn("Disconnecting client ({0}) @ {1}... {2}", Account?.Name ?? " ", IP, reason);

            if (Account != null)
                try {
                    Save();
                }
                catch (Exception e) {
                    var msg = $"{e.Message}\n{e.StackTrace}";
                    Log.Error(msg);
                }

            Manager.Disconnect(this);
            _server.Disconnect(this);
        }
    }

    private void Save() // only when disconnect
    {
        var acc = Account;

        if (Character == null || Player == null || Player.Owner is Test) {
            Manager.Database.ReleaseLock(acc);
            return;
        }

        Player.SaveToCharacter();
        Player.Owner?.LeaveWorld(Player);
        acc.RefreshLastSeen();
        acc.FlushAsync();
        Manager.Database.SaveCharacter(acc, Character, Player.FameCounter.ClassStats, true)
            .ContinueWith(t => Manager.Database.ReleaseLock(acc));
    }

    private async void Receive() {
        if (!Socket.Connected)
            return;

        try {
            while (Socket.Connected) {
                var len = await Socket.ReceiveAsync(ReceiveMem);
                if (len > 0)
                    ProcessPacket(len);
            }
        }
        catch (Exception e) {
            Disconnect();
            if (e is not SocketException se || se.SocketErrorCode != SocketError.ConnectionReset &&
                se.SocketErrorCode != SocketError.Shutdown)
                Log.Error($"Could not receive data from {Account?.Name ?? "[unconnected]"} ({IP}): {e}");
        }
    }


    private void ProcessPacket(int len) {
        var ptr = 0;
        ref var spanRef = ref MemoryMarshal.GetReference(ReceiveMem.Span);
        while (ptr < len) {
            var packetLen = ReadUShort(ref ptr, ref spanRef, len);
            var nextPacketPtr = ptr + packetLen - 2;
            var packetId = (C2SPacketId) ReadByte(ref ptr, ref spanRef, nextPacketPtr);

            if (Reconnecting)
                Console.WriteLine($"Handling: {packetId} while Reconnect is in process");

            switch (packetId) {
                case C2SPacketId.AcceptTrade:
                    ProcessAcceptTrade(ReadBoolArray(ref ptr, ref spanRef, nextPacketPtr),
                        ReadBoolArray(ref ptr, ref spanRef, nextPacketPtr));

                    break;
                case C2SPacketId.AoeAck:
                    ProcessAoeAck(ReadLong(ref ptr, ref spanRef, nextPacketPtr), ReadFloat(ref ptr, ref spanRef, nextPacketPtr),
                        ReadFloat(ref ptr, ref spanRef, nextPacketPtr));

                    break;
                case C2SPacketId.Buy:
                    ProcessBuy(ReadInt(ref ptr, ref spanRef, nextPacketPtr));
                    break;
                case C2SPacketId.CancelTrade:
                    ProcessCancelTrade();
                    break;
                case C2SPacketId.ChangeGuildRank:
                    ProcessChangeGuildRank(ReadString(ref ptr, ref spanRef, nextPacketPtr),
                        ReadInt(ref ptr, ref spanRef, nextPacketPtr));

                    break;
                case C2SPacketId.ChangeTrade:
                    ProcessChangeTrade(ReadBoolArray(ref ptr, ref spanRef, nextPacketPtr));
                    break;
                case C2SPacketId.ChooseName:
                    ProcessChooseName(ReadString(ref ptr, ref spanRef, nextPacketPtr));
                    break;
                case C2SPacketId.CreateGuild:
                    ProcessCreateGuild(ReadString(ref ptr, ref spanRef, nextPacketPtr));
                    break;
                case C2SPacketId.EditAccountList:
                    ProcessEditAccountList(ReadInt(ref ptr, ref spanRef, nextPacketPtr),
                        ReadBool(ref ptr, ref spanRef, nextPacketPtr),
                        ReadInt(ref ptr, ref spanRef, nextPacketPtr));

                    break;
                case C2SPacketId.EnemyHit:
                    ProcessEnemyHit(ReadLong(ref ptr, ref spanRef, nextPacketPtr),
                        ReadByte(ref ptr, ref spanRef, nextPacketPtr),
                        ReadInt(ref ptr, ref spanRef, nextPacketPtr), ReadBool(ref ptr, ref spanRef, nextPacketPtr));

                    break;
                case C2SPacketId.Escape:
                    ProcessEscape();
                    break;
                case C2SPacketId.GroundDamage:
                    ProcessGroundDamage(ReadLong(ref ptr, ref spanRef, nextPacketPtr),
                        ReadFloat(ref ptr, ref spanRef, nextPacketPtr),
                        ReadFloat(ref ptr, ref spanRef, nextPacketPtr));

                    break;
                case C2SPacketId.GuildInvite:
                    ProcessGuildInvite(ReadString(ref ptr, ref spanRef, nextPacketPtr));
                    break;
                case C2SPacketId.GuildRemove:
                    ProcessGuildRemove(ReadString(ref ptr, ref spanRef, nextPacketPtr));
                    break;
                case C2SPacketId.Hello: {
                    var buildVer = ReadString(ref ptr, ref spanRef, nextPacketPtr);
                    var gameId = ReadInt(ref ptr, ref spanRef, nextPacketPtr);
                    var guid = ReadString(ref ptr, ref spanRef, nextPacketPtr);
                    var pwd = ReadString(ref ptr, ref spanRef, nextPacketPtr);
                    var chrId = ReadShort(ref ptr, ref spanRef, nextPacketPtr);
                    var createChar = ReadBool(ref ptr, ref spanRef, nextPacketPtr);
                    var charType = (ushort) (createChar ? (ushort) ReadShort(ref ptr, ref spanRef, nextPacketPtr) : 0);
                    var skinType = (ushort) (createChar ? (ushort) ReadShort(ref ptr, ref spanRef, nextPacketPtr) : 0);
                    ProcessHello(buildVer, gameId, guid, pwd, chrId, createChar, charType, skinType);
                    break;
                }
                case C2SPacketId.InvDrop:
                    ProcessInvDrop(ReadInt(ref ptr, ref spanRef, nextPacketPtr),
                        ReadByte(ref ptr, ref spanRef, nextPacketPtr),
                        ReadInt(ref ptr, ref spanRef, nextPacketPtr));

                    break;
                case C2SPacketId.InvSwap:
                    ProcessInvSwap(ReadLong(ref ptr, ref spanRef, nextPacketPtr),
                        ReadFloat(ref ptr, ref spanRef, nextPacketPtr),
                        ReadFloat(ref ptr, ref spanRef, nextPacketPtr), ReadInt(ref ptr, ref spanRef, nextPacketPtr),
                        ReadByte(ref ptr, ref spanRef, nextPacketPtr), ReadInt(ref ptr, ref spanRef, nextPacketPtr),
                        ReadInt(ref ptr, ref spanRef, nextPacketPtr), ReadByte(ref ptr, ref spanRef, nextPacketPtr),
                        ReadInt(ref ptr, ref spanRef, nextPacketPtr));

                    break;
                case C2SPacketId.JoinGuild:
                    ProcessJoinGuild(ReadString(ref ptr, ref spanRef, nextPacketPtr));
                    break;
                case C2SPacketId.Move: {
                    var tickId = ReadInt(ref ptr, ref spanRef, nextPacketPtr);
                    var time = ReadLong(ref ptr, ref spanRef, nextPacketPtr);
                    var x = ReadFloat(ref ptr, ref spanRef, nextPacketPtr);
                    var y = ReadFloat(ref ptr, ref spanRef, nextPacketPtr);
                    ProcessMove(tickId, time, x, y, ReadMoveRecordArray(ref ptr, ref spanRef, nextPacketPtr));
                    break;
                }
                case C2SPacketId.OtherHit:
                    ProcessOtherHit(ReadLong(ref ptr, ref spanRef, nextPacketPtr),
                        ReadByte(ref ptr, ref spanRef, nextPacketPtr),
                        ReadInt(ref ptr, ref spanRef, nextPacketPtr), ReadInt(ref ptr, ref spanRef, nextPacketPtr));

                    break;
                case C2SPacketId.PlayerHit:
                    ProcessPlayerHit(ReadByte(ref ptr, ref spanRef, nextPacketPtr),
                        ReadInt(ref ptr, ref spanRef, nextPacketPtr));

                    break;
                case C2SPacketId.PlayerShoot:
                    ProcessPlayerShoot(ReadLong(ref ptr, ref spanRef, nextPacketPtr),
                        ReadByte(ref ptr, ref spanRef, nextPacketPtr),
                        ReadUShort(ref ptr, ref spanRef, nextPacketPtr), ReadFloat(ref ptr, ref spanRef, nextPacketPtr),
                        ReadFloat(ref ptr, ref spanRef, nextPacketPtr), ReadFloat(ref ptr, ref spanRef, nextPacketPtr));

                    break;
                case C2SPacketId.PlayerText:
                    ProcessPlayerText(ReadString(ref ptr, ref spanRef, nextPacketPtr));
                    break;
                case C2SPacketId.Pong:
                    ProcessPong(ReadInt(ref ptr, ref spanRef, nextPacketPtr),
                        ReadLong(ref ptr, ref spanRef, nextPacketPtr));

                    break;
                case C2SPacketId.RequestTrade:
                    ProcessRequestTrade(ReadString(ref ptr, ref spanRef, nextPacketPtr));
                    break;
                case C2SPacketId.Reskin:
                    ProcessReskin((ushort) ReadInt(ref ptr, ref spanRef, nextPacketPtr));
                    break;
                case C2SPacketId.ShootAck:
                    ProcessShootAck(ReadLong(ref ptr, ref spanRef, nextPacketPtr));
                    break;
                case C2SPacketId.SquareHit:
                    ProcessSquareHit(ReadLong(ref ptr, ref spanRef, nextPacketPtr),
                        ReadByte(ref ptr, ref spanRef, nextPacketPtr),
                        ReadInt(ref ptr, ref spanRef, nextPacketPtr));

                    break;
                case C2SPacketId.Teleport:
                    ProcessTeleport(ReadInt(ref ptr, ref spanRef, nextPacketPtr));
                    break;
                case C2SPacketId.UpdateAck:
                    ProcessUpdateAck();
                    break;
                case C2SPacketId.UseItem:
                    ProcessUseItem(ReadLong(ref ptr, ref spanRef, nextPacketPtr),
                        ReadInt(ref ptr, ref spanRef, nextPacketPtr),
                        ReadByte(ref ptr, ref spanRef, nextPacketPtr),
                        (ushort) ReadInt(ref ptr, ref spanRef, nextPacketPtr),
                        ReadFloat(ref ptr, ref spanRef, nextPacketPtr), ReadFloat(ref ptr, ref spanRef, nextPacketPtr),
                        ReadByte(ref ptr, ref spanRef, nextPacketPtr));

                    break;
                case C2SPacketId.UsePortal:
                    ProcessUsePortal(ReadInt(ref ptr, ref spanRef, nextPacketPtr));
                    break;
                default:
                    Log.Warn($"Unhandled packet '.{packetId}'.");
                    break;
            }

            ptr = nextPacketPtr;
        }
    }

    private void ProcessAcceptTrade(bool[] myOffer, bool[] yourOffer) {
        var player = Player;
        if (player == null)
            return;

        if (player.tradeAccepted) return;

        player.trade = myOffer;
        if (player.tradeTarget.trade.SequenceEqual(yourOffer)) {
            player.tradeAccepted = true;
            player.tradeTarget.Client.SendTradeAccepted(player.tradeTarget.trade, player.trade);

            if (player.tradeAccepted && player.tradeTarget.tradeAccepted) {
                if (player.Client.Account.Admin != player.tradeTarget.Client.Account.Admin) {
                    player.tradeTarget.CancelTrade();
                    player.CancelTrade();
                    return;
                }

                var failedMsg = "Error while trading. Trade unsuccessful.";
                var msg = "Trade Successful!";
                var thisItems = new List<Item>();
                var targetItems = new List<Item>();

                var tradeTarget = player.tradeTarget;

                // make sure trade targets are valid
                if (tradeTarget == null || player.Owner == null || tradeTarget.Owner == null ||
                    player.Owner != tradeTarget.Owner) {
                    player.Client.SendTradeDone(1, failedMsg);
                    tradeTarget?.Client.SendTradeDone(1, failedMsg);
                    player.ResetTrade();
                    return;
                }

                if (!player.tradeAccepted || !tradeTarget.tradeAccepted)
                    return;

                var pInvTrans = player.Inventory.CreateTransaction();
                var tInvTrans = tradeTarget.Inventory.CreateTransaction();

                for (var i = 4; i < player.trade.Length; i++)
                    if (player.trade[i]) {
                        thisItems.Add(player.Inventory[i]);
                        pInvTrans[i] = null;
                    }

                for (var i = 4; i < tradeTarget.trade.Length; i++)
                    if (tradeTarget.trade[i]) {
                        targetItems.Add(tradeTarget.Inventory[i]);
                        tInvTrans[i] = null;
                    }

                // move thisItems -> tradeTarget
                for (var i = 0; i < 12; i++)
                for (var j = 0; j < thisItems.Count; j++)
                    if (tradeTarget.SlotTypes[i] == 0 &&
                        tInvTrans[i] == null ||
                        thisItems[j] != null &&
                        tradeTarget.SlotTypes[i] == thisItems[j].SlotType &&
                        tInvTrans[i] == null) {
                        tInvTrans[i] = thisItems[j];
                        thisItems.Remove(thisItems[j]);
                        break;
                    }

                // move tradeItems -> this
                for (var i = 0; i < 12; i++)
                for (var j = 0; j < targetItems.Count; j++)
                    if (player.SlotTypes[i] == 0 &&
                        pInvTrans[i] == null ||
                        targetItems[j] != null &&
                        player.SlotTypes[i] == targetItems[j].SlotType &&
                        pInvTrans[i] == null) {
                        pInvTrans[i] = targetItems[j];
                        targetItems.Remove(targetItems[j]);
                        break;
                    }

                // save
                if (!Inventory.Execute(pInvTrans, tInvTrans)) {
                    player.Client.SendTradeDone(1, failedMsg);
                    tradeTarget?.Client.SendTradeDone(1, failedMsg);
                    player.ResetTrade();
                    return;
                }

                // check for lingering items
                if (thisItems.Count > 0 ||
                    targetItems.Count > 0)
                    msg = "An error occured while trading! Some items were lost!";

                // trade successful, notify and save
                player.Client.SendTradeDone(1, msg);
                tradeTarget?.Client.SendTradeDone(1, msg);
                player.ResetTrade();
            }
        }
    }

    private void ProcessAoeAck(long time, float x, float y) {
    }

    private void ProcessBuy(int id) {
        if (Player?.Owner == null)
            return;

        var obj = Player.Owner.GetEntity(id) as SellableObject;
        obj?.Buy(Player);
    }

    private void ProcessCancelTrade() {
        Player?.CancelTrade();
    }

    private void ProcessChangeGuildRank(string name, int rank) {
        var manager = Manager;
        var srcAcnt = Account;
        var srcPlayer = Player;

        if (srcPlayer == null)
            return;

        var targetId = Manager.Database.ResolveId(name);
        if (targetId == 0) {
            srcPlayer.SendErrorText("A player with that name does not exist.");
            return;
        }

        // get target client if available (player is currently connected to the server)
        // otherwise pull account from db
        var target = Manager.Clients.Keys
            .SingleOrDefault(c => c.Account.AccountId == targetId);

        var targetAcnt = target != null ? target.Account : manager.Database.GetAccount(targetId);

        if (srcAcnt.GuildId <= 0 ||
            srcAcnt.GuildRank < 20 ||
            srcAcnt.GuildRank <= targetAcnt.GuildRank ||
            srcAcnt.GuildRank < rank ||
            rank == 40 ||
            srcAcnt.GuildId != targetAcnt.GuildId) {
            srcPlayer.SendErrorText("No permission");
            return;
        }

        var targetRank = targetAcnt.GuildRank;
        var stringRank = rank switch
        {
            0 => "Initiate",
            10 => "Member",
            20 => "Officer",
            30 => "Leader",
            40 => "Founder",
            _ => ""
        };

        if (targetRank == rank) {
            srcPlayer.SendErrorText("Player is already a " + stringRank);
            return;
        }

        // change rank
        if (!Manager.Database.ChangeGuildRank(targetAcnt, rank)) {
            srcPlayer.SendErrorText("Failed to change rank.");
            return;
        }

        // update player if connected
        if (target != null)
            target.Player.GuildRank = rank;

        // notify guild
        if (targetRank < rank)
            Manager.Chat.Guild(
                srcPlayer,
                targetAcnt.Name + " has been promoted to " + stringRank + ".",
                true);
        else
            Manager.Chat.Guild(
                srcPlayer,
                targetAcnt.Name + " has been demoted to " + stringRank + ".",
                true);
    }

    private void ProcessChangeTrade(bool[] offer) {
        var sb = false;
        var player = Player;

        if (player?.tradeTarget == null)
            return;

        for (var i = 0; i < offer.Length; i++)
            if (offer[i])
                if (player.Inventory[i].Soulbound) {
                    sb = true;
                    offer[i] = false;
                }

        player.tradeAccepted = false;
        player.tradeTarget.tradeAccepted = false;
        player.trade = offer;

        player.tradeTarget.Client.SendTradeChanged(player.trade);

        if (sb) player.SendErrorText("You can't trade Soulbound items.");
    }

    private void ProcessChooseName(string name) {
        if (Player == null)
            return;

        Manager.Database.ReloadAccount(Account);

        if (name.Length < 1 || name.Length > 10 || !name.All(char.IsLetter) ||
            Database.GuestNames.Contains(name, StringComparer.InvariantCultureIgnoreCase)) {
            SendNameResult(false, "Invalid name");
        }
        else {
            var key = Database.NAME_LOCK;
            string lockToken = null;
            try {
                while ((lockToken = Manager.Database.AcquireLock(key)) == null) ;

                if (Manager.Database.Conn.HashExists("names", name.ToUpperInvariant())) {
                    SendNameResult(false, "Duplicated name");
                    return;
                }

                if (Account.NameChosen && Account.Credits < 1000) {
                    SendNameResult(false, "Not enough gold");
                }
                else {
                    // remove fame is purchasing name change
                    if (Account.NameChosen)
                        Manager.Database.UpdateCredit(Account, -1000);

                    // rename
                    var oldName = Account.Name;
                    while (!Manager.Database.RenameIGN(Account, name, lockToken)) ;

                    // update player
                    Player.Credits = Account.Credits;
                    Player.CurrentFame = Account.Fame;
                    Player.Name = Account.Name;
                    Player.NameChosen = true;
                    SendNameResult(true, "");
                }
            }
            finally {
                if (lockToken != null)
                    Manager.Database.ReleaseLock(key, lockToken);
            }
        }
    }

    private void ProcessCreateGuild(string guildName) {
        if (Player == null)
            return;

        var acc = Account;

        if (acc.Fame < 1000) {
            SendGuildResult(false, "Guild Creation Error: Insufficient funds");
            return;
        }

        if (!acc.NameChosen) {
            SendGuildResult(false, "Guild Creation Error: Must pick a character name before creating a guild");
            return;
        }

        if (acc.GuildId > 0) {
            SendGuildResult(false, "Guild Creation Error: Already in a guild");
            return;
        }

        var guildResult = Manager.Database.CreateGuild(guildName, out var guild);
        if (guildResult != GuildCreateStatus.OK) {
            SendGuildResult(false, "Guild Creation Error: " + guildResult);
            return;
        }

        var addResult = Manager.Database.AddGuildMember(guild, acc, true);
        if (addResult != AddGuildMemberStatus.OK) {
            SendGuildResult(false, "Guild Creation Error: " + addResult);
            return;
        }

        Manager.Database.UpdateFame(acc, -1000);
        Player.CurrentFame = acc.Fame;
        Player.Guild = guild.Name;
        Player.GuildRank = 40;
        SendGuildResult(true, "Success!");
    }

    private void ProcessEditAccountList(int action, bool add, int id) {
        if (Player == null)
            return;

        const int LockAction = 0;
        const int IgnoreAction = 1;

        if (Player.Owner.GetEntity(id) is not Player targetPlayer || targetPlayer.Client.Account == null) {
            Player.SendErrorText("Player not found.");
            return;
        }

        switch (action) {
            case LockAction:
                Manager.Database.LockAccount(Account, targetPlayer.Client.Account, add);
                return;
            case IgnoreAction:
                Manager.Database.IgnoreAccount(Account, targetPlayer.Client.Account, add);
                break;
        }
    }

    private void ProcessEnemyHit(long time, byte bulletId, int targetId, bool kill)
    {
        var entity = Player?.Owner?.GetEntity(targetId);
        if (entity?.Owner == null)
            return;

        var prj = Player.Projectiles[bulletId];
        if (prj == null)
            Log.Debug("prj is dead...");

        prj?.ForceHit(entity, Manager.Logic.RealmTime);

        if (kill)
            Player.ClientKilledEntity.Enqueue(entity);
    }

    private void ProcessEscape() {
        if (Player?.Owner != null) 
            Reconnect("Hub", -2);
    }

    private void ProcessGroundDamage(long time, float x, float y) {
        if (Player?.Owner != null)
            Player.ForceGroundHit(x, y, time);
    }

    private void ProcessGuildInvite(string name) {
        if (Player == null)
            return;

        if (Account.GuildRank < 20) {
            Player.SendErrorText("Insufficient privileges.");
            return;
        }

        foreach (var client in Manager.Clients.Keys) {
            if (client.Player == null ||
                client.Account == null ||
                !client.Account.Name.Equals(name))
                continue;

            if (!client.Account.NameChosen) {
                Player.SendErrorText("Player needs to choose a name first.");
                return;
            }

            if (client.Account.GuildId > 0) {
                Player.SendErrorText("Player is already in a guild.");
                return;
            }

            client.Player.GuildInvite = Account.GuildId;

            client.SendInvitedToGuild(Player.Guild, Account.Name);
            return;
        }

        Player.SendErrorText("Could not find the player to invite.");
    }

    private void ProcessGuildRemove(string name) {
        if (Player == null)
            return;

        var srcPlayer = Player;

        // if resigning
        if (Account.Name.Equals(name)) {
            // chat needs to be done before removal so we can use
            // srcPlayer as a source for guild info
            Manager.Chat.Guild(srcPlayer, srcPlayer.Name + " has left the guild.", true);

            if (!Manager.Database.RemoveFromGuild(Account)) {
                srcPlayer.SendErrorText("Guild not found.");
                return;
            }

            srcPlayer.Guild = "";
            srcPlayer.GuildRank = 0;

            return;
        }

        // get target account id
        var targetAccId = Manager.Database.ResolveId(name);
        if (targetAccId == 0) {
            Player.SendErrorText("Player not found");
            return;
        }

        // find target player (if connected)
        var targetClient = (from client in Manager.Clients.Keys
                where client.Account != null
                where client.Account.AccountId == targetAccId
                select client)
            .FirstOrDefault();

        // try to remove connected member
        if (targetClient != null) {
            if (Account.GuildRank >= 20 &&
                Account.GuildId == targetClient.Account.GuildId &&
                Account.GuildRank > targetClient.Account.GuildRank) {
                var targetPlayer = targetClient.Player;

                if (!Manager.Database.RemoveFromGuild(targetClient.Account)) {
                    srcPlayer.SendErrorText("Guild not found.");
                    return;
                }

                targetPlayer.Guild = "";
                targetPlayer.GuildRank = 0;

                Manager.Chat.Guild(srcPlayer,
                    targetPlayer.Name + " has been kicked from the guild by " + srcPlayer.Name, true);

                targetPlayer.SendInfo("You have been kicked from the guild.");
                return;
            }

            srcPlayer.SendErrorText("Can't remove member. Insufficient privileges.");
            return;
        }

        // try to remove member via database
        var targetAccount = Manager.Database.GetAccount(targetAccId);

        if (Account.GuildRank >= 20 &&
            Account.GuildId == targetAccount.GuildId &&
            Account.GuildRank > targetAccount.GuildRank) {
            if (!Manager.Database.RemoveFromGuild(targetAccount)) {
                srcPlayer.SendErrorText("Guild not found.");
                return;
            }

            Manager.Chat.Guild(srcPlayer,
                targetAccount.Name + " has been kicked from the guild by " + srcPlayer.Name, true);

            Manager.Chat.SendInfo(targetAccId, "You have been kicked from the guild.");
            return;
        }

        srcPlayer.SendErrorText("Can't remove member. Insufficient privileges.");
    }

    private void ProcessHello(string buildVer, int gameId, string guid, string pwd, short charId, bool createChar,
        ushort charType, ushort skinType) {
        // validate connection eligibility and get acc info
        var version = Manager.Config.serverSettings.version;
        if (!version.Equals(buildVer)) {
            Disconnect(version);
            return;
        }

        var s1 = Manager.Database.Verify(guid, pwd, out var acc);
        if (s1 is LoginStatus.AccountNotExists or LoginStatus.InvalidCredentials) {
            Disconnect("Bad Login");
            return;
        }

        if (acc.Banned) {
            Disconnect("Account banned.");
            Log.Info("{0} ({1}) tried to log in. Account Banned.",
                acc.Name, IP);

            return;
        }

        if (Manager.Database.IsIpBanned(IP)) {
            Disconnect("IP banned.");
            Log.Info($"{acc.Name} ({IP}) tried to log in. IP Banned.");
            return;
        }

        if (!acc.Admin && Manager.Config.serverInfo.adminOnly) {
            Disconnect("Admin Only Server");
            return;
        }

        // log ip
        Manager.Database.LogAccountByIp(IP, acc.AccountId);
        acc.IP = IP;
        acc.FlushAsync();

        Account = acc;

        ConnectManager.Connect(this, gameId, charId, createChar, charType, skinType);
    }

    private void ProcessInvDrop(int objId, byte slotId, int objType) {
        if (Player?.Owner == null || Player.tradeTarget != null)
            return;

        const ushort normBag = 0x0500;
        const ushort soulBag = 0x0503;

        IContainer con;

        // container isn't always the player's inventory, it's given by the SlotObject's ObjectId
        if (objId != Player.Id) {
            if (Player.Owner.GetEntity(objId) is Player) {
                Player.Client.SendInventoryResult(1);
                return;
            }

            con = Player.Owner.GetEntity(objId) as IContainer;
        }
        else {
            con = Player;
        }


        if (objId == Player.Id && Player.Stacks.Any(stack => stack.Slot == slotId)) {
            Player.Client.SendInventoryResult(1);
            return; // don't allow dropping of stacked items
        }

        if (con?.Inventory[slotId] == null) {
            //give proper error
            Player.Client.SendInventoryResult(1);
            return;
        }

        var item = con.Inventory[slotId];
        con.Inventory[slotId] = null;

        // create new container for item to be placed in
        Container container;
        if (item.Soulbound || Player.Client.Account.Admin) {
            container = new Container(Player.Manager, soulBag, 1000 * 60, true);
            container.BagOwners = new[] { Player.AccountId };
        }
        else {
            container = new Container(Player.Manager, normBag, 1000 * 60, true);
        }

        // init container
        container.Inventory[0] = item;
        container.Move(Player.X + (float) ((InvRand.NextDouble() * 2 - 1) * 0.5), Player.Y + (float) ((InvRand.NextDouble() * 2 - 1) * 0.5));

        container.SetDefaultSize(75);
        Player.Owner.EnterWorld(container);

        // send success
        Player.Client.SendInventoryResult(0);
    }

    private bool ValidateEntities(Player p, Entity a, Entity b) {
        // returns false if bad input
        if (a == null || b == null)
            return false;

        if (a is not IContainer || b is not IContainer)
            return false;

        if (a is Player && a != p || b is Player && b != p)
            return false;

        if (a is Container container && container.BagOwners.Length > 0 && !container.BagOwners.Contains(p.AccountId))
            return false;

        if (b is Container container1 && container1.BagOwners.Length > 0 && !container1.BagOwners.Contains(p.AccountId))
            return false;

        if (a is OneWayContainer && b != p || b is OneWayContainer && a != p)
            return false;

        var aPos = new Vector2(a.X, a.Y);
        var bPos = new Vector2(b.X, b.Y);
        if (Vector2.DistanceSquared(aPos, bPos) > 1)
            return false;

        return true;
    }

    private bool ValidateSlotSwap(Player player, IContainer conA, IContainer conB, int slotA, int slotB) {
        return
            (slotA < 12 && slotB < 12 || player.HasBackpack) &&
            conB.AuditItem(conA.Inventory[slotA], slotB) &&
            conA.AuditItem(conB.Inventory[slotB], slotA);
    }

    private bool ValidateItemSwap(Player player, Entity c, Item item) {
        return c == player ||
               item == null ||
               !item.Soulbound && !player.Client.Account.Admin ||
               IsSoleContainerOwner(player, c as IContainer);
    }

    private bool IsSoleContainerOwner(Player player, IContainer con) {
        int[] owners = null;
        if (con is Container container)
            owners = container.BagOwners;

        return owners != null && owners.Length == 1 && owners.Contains(player.AccountId);
    }

    private void DropInSoulboundBag(Player player, Item item) {
        var container = new Container(player.Manager, 0x0503, 1000 * 60, true)
        {
            BagOwners = new[] { player.AccountId },
            Inventory = { [0] = item },
        };

        container.Move(player.X + (float) ((InvRand.NextDouble() * 2 - 1) * 0.5),
            player.Y + (float) ((InvRand.NextDouble() * 2 - 1) * 0.5));

        container.SetDefaultSize(75);
        player.Owner.EnterWorld(container);
    }

    private void ProcessInvSwap(long time, float x, float y, int objId1, byte slotId1, int itemType1, int objId2,
        byte slotId2, int itemType2) {
        var a = Player.Owner.GetEntity(objId1);
        var b = Player.Owner.GetEntity(objId2);

        if (Player?.Owner == null)
            return;

        if (!ValidateEntities(Player, a, b) || Player.tradeTarget != null) {
            a.ForceUpdate(slotId1);
            b.ForceUpdate(slotId2);
            SendInventoryResult(1);
            return;
        }

        var conA = (IContainer) a;
        var conB = (IContainer) b;

        // check if stacking operation
        if (b == Player)
            foreach (var stack in Player.Stacks)
                if (stack.Slot == slotId2) {
                    var stackTrans = conA.Inventory.CreateTransaction();
                    var item = stack.Put(stackTrans[slotId1]);
                    if (item == null) // success
                    {
                        stackTrans[slotId1] = null;
                        Inventory.Execute(stackTrans);
                        SendInventoryResult(1);
                        return;
                    }
                }

        // not stacking operation, continue on with normal swap

        // validate slot types
        if (!ValidateSlotSwap(Player, conA, conB, slotId1, slotId2)) {
            a.ForceUpdate(slotId1);
            b.ForceUpdate(slotId2);
            SendInventoryResult(1);
            return;
        }

        // setup swap
        var queue = new Queue<Action>();
        var conATrans = conA.Inventory.CreateTransaction();
        var conBTrans = conB.Inventory.CreateTransaction();
        var itemA = conATrans[slotId1];
        var itemB = conBTrans[slotId2];
        conBTrans[slotId2] = itemA;
        conATrans[slotId1] = itemB;

        // validate that soulbound items are not placed in public bags (includes any swaped item from admins)
        if (!ValidateItemSwap(Player, a, itemB)) {
            queue.Enqueue(() => DropInSoulboundBag(Player, itemB));
            conATrans[slotId1] = null;
        }

        if (!ValidateItemSwap(Player, b, itemA)) {
            queue.Enqueue(() => DropInSoulboundBag(Player, itemA));
            conBTrans[slotId2] = null;
        }

        // swap items
        if (Inventory.Execute(conATrans, conBTrans)) {
            while (queue.Count > 0)
                queue.Dequeue()();

            SendInventoryResult(0);
            return;
        }

        a.ForceUpdate(slotId1);
        b.ForceUpdate(slotId2);
        SendInventoryResult(1);
    }

    public void ProcessJoinGuild(string guildName) {
        if (Player == null)
            return;

        if (Player.GuildInvite == null) {
            Player.SendErrorText("You have not been invited to a guild.");
            return;
        }

        var guild = Manager.Database.GetGuild((int) Player.GuildInvite);

        if (guild == null) {
            Player.SendErrorText("Internal server error.");
            return;
        }

        if (!guild.Name.Equals(guildName, StringComparison.InvariantCultureIgnoreCase)) {
            Player.SendErrorText("You have not been invited to join " + guildName + ".");
            return;
        }

        var result = Manager.Database.AddGuildMember(guild, Account);
        if (result != AddGuildMemberStatus.OK) {
            Player.SendErrorText("Could not join guild. (" + result + ")");
            return;
        }

        Player.Guild = guild.Name;
        Player.GuildRank = 0;

        Manager.Chat.Guild(Player, Player.Name + " has joined the guild!", true);
    }

    private void ProcessMove(int tickId, long time, float x, float y, MoveRecord[] records) 
    {
        if (Player == null)
            return;//seperated logic so i can breakpoint
        if (Player.Owner == null)
            return;

        if(x == -1 && y == -1) // will cause bounds error might look into removing this type of interaction
            return;

        if(!Player.Owner.Map.Contains((int)x, (int)y))
        {
            Console.WriteLine($"Player went out of map bounds: {x}, {y}");
            //Disconnect($"Player went out of map bounds: {x}, {y}");
            return;
        }

        Player.MoveReceived(Manager.Logic.RealmTime, tickId, time);
        Player.Move(x, y);

        if ((int)x != (int)Player.PreviousX || (int)y != (int)Player.PreviousY)
            Player.Sight.UpdateVisibility();

        if (Player.IsNoClipping() || !Player.Sight.VisibleTiles.Contains(new IntPoint((int)x, (int)y))) // replaces Player.Tiles check
            Disconnect("Invalid position");
    }

    private void ProcessOtherHit(long time, byte bulletId, int ownerId, int targetId) {
    }

    private void ProcessPlayerHit(byte bulletId, int objId)
    {
        if (Player?.Owner == null)
            return;

        var shooter = Player.Owner.GetEntity(objId);
        if (shooter == null)
            return;

        var prj = Player.Owner.GetProjectile(objId, bulletId);
        if(prj == null)
            Console.WriteLine("Null PlayerHit Projectile");
        prj?.ForceHit(Player, Manager.Logic.RealmTime);
    }

    private void ProcessPlayerShoot(long time, byte bulletId, ushort objType, float x, float y, float angle) {
   
        if (Player.Inventory[1].ObjectType == objType) {
            // we dont handle ability
            Player.Client.SeededRandom.NextInt(); // this is needed as we doShoot client side which uses the random gen
            return;
        }

        if (!Player.Manager.Resources.GameData.Items.TryGetValue(Player.Inventory[0].ObjectType, out var item))
        {
            Player.Client.SeededRandom.NextInt();
            return;
        }

        var prjDesc = item.Projectiles[0]; //Assume only one

        // validate
        var result = Player.ValidatePlayerShoot(item, time);
        if (result != PlayerShootStatus.OK) {
            Player.Client.SeededRandom.NextInt();
            return;
        }

        // create projectile and show other players
        var prj = Player.PlayerShootProjectile(
            bulletId, prjDesc, item.ObjectType,
            time, new Position { X = x, Y = y }, angle);

        Player.Owner.AddProjectile(prj);

        foreach (var otherPlayer in Player.Owner.Players.Values)
            if (otherPlayer.Id != Player.Id && otherPlayer.DistSqr(Player) < Player.RADIUS_SQR)
                otherPlayer.Client.SendAllyShoot(bulletId, Player.Id, objType, angle);

        Player.FameCounter.IncrementShoot();
    }

    private void ProcessPlayerText(string text) {
        if (Player?.Owner == null || text.Length > 256)
            return;

        // check for commands before other checks
        if (text[0] == '/') {
            _ = Manager.Commands.Execute(Player, Manager.Logic.RealmTime, text);
        }
        else {
            if (!Player.NameChosen) {
                Player.SendErrorText("Please choose a name before chatting.");
                return;
            }

            if (Player.Muted) {
                Player.SendErrorText("Muted. You can not talk at this time.");
                return;
            }

            // save message for mob behaviors
            Player.Owner.ChatReceived(Player, text);

            ChatManager.Say(Player, text);
        }
    }

    private void ProcessPong(int serial, long pongTime) {
        Player?.Pong(Manager.Logic.RealmTime, serial, pongTime);
    }

    private void ProcessRequestTrade(string name) {
        Player?.RequestTrade(name);
    }

    private void ProcessReskin(ushort skin) {
        if (Player == null)
            return;

        var gameData = Manager.Resources.GameData;

        Account.Reload("skins"); // get newest skin data
        var ownedSkins = Account.Skins;
        var currentClass = Player.ObjectType;

        var skinData = gameData.Skins;
        var skinSize = 100;

        if (skin != 0) {
            skinData.TryGetValue(skin, out var skinDesc);

            if (skinDesc == null) {
                Player.SendErrorText("Unknown skin type.");
                return;
            }

            if (!ownedSkins.Contains(skin)) {
                Player.SendErrorText("Skin not owned.");
                return;
            }

            if (skinDesc.PlayerClassType != currentClass) {
                Player.SendErrorText("Skin is for different class.");
                return;
            }

            skinSize = skinDesc.Size;
        }

        // set skin
        Player.SetDefaultSkin(skin);
        Player.SetDefaultSize(skinSize);
    }

    private void ProcessShootAck(long time) {
        //Player.ShootAckReceived(time);
    }

    private void ProcessSquareHit(long time, byte bulletId, int objId) {
    }

    private void ProcessTeleport(int objId) {
        if (Player?.Owner != null) Player.Teleport(Manager.Logic.RealmTime, objId);
    }

    private void ProcessUpdateAck() {
        Player.UpdateAckReceived();
    }

    private void ProcessUseItem(long time, int objId, byte slotId, int objType, float x, float y, byte useType) {
        if (Player?.Owner != null) Player.UseItem(Manager.Logic.RealmTime, objId, slotId, new Position { X = x, Y = y });
    }

    private void ProcessUsePortal(int objId) {
        if (Player?.Owner == null)
            return;

        var entity = Player.Owner.GetEntity(objId);
        if (entity == null)
            return;

        var portal = entity as Portal;
        if (portal == null || !portal.Usable)
            return;

        lock (portal.CreateWorldLock) {
            var world = portal.WorldInstance;

            // special portal case lookup
            if (world == null && _realmPortals.Contains(portal.ObjectType)) {
                world = Player.Manager.GetRandomRealm();
                if (world == null)
                    return;
            }

            if (world is RealmOfTheMadGod && !Player.Manager.Resources.GameData
                    .ObjectTypeToId[portal.ObjectDesc.ObjectType].Contains("Cowardice"))
                Player.FameCounter.CompleteDungeon(Player.Owner.IdName);

            if (world != null) {
                Player.Client.Reconnect(world.IdName, world.Id);
                return;
            }

            // dynamic case lookup
            if (portal.CreateWorldTask == null || portal.CreateWorldTask.IsCompleted)
                portal.CreateWorldTask = Task.Factory.StartNew(() => portal.CreateWorld(Player))
                    .ContinueWith(e => Log.Error(e.Exception.InnerException.ToString()),
                        TaskContinuationOptions.OnlyOnFaulted);

            portal.WorldInstanceSet += Player.Reconnect;
        }
    }

    #region Send Methods

    public void SendFailure(int errorCode, string description) {
        lock (SendLock) {
            var ptr = LENGTH_PREFIX;
            ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
            WriteByte(ref ptr, ref spanRef, (byte) S2CPacketId.Failure);

            WriteInt(ref ptr, ref spanRef, errorCode);
            WriteString(ref ptr, ref spanRef, description);

            TrySend(ptr);
        }
    }

    public void SendQuestObjectId(int objectId) {
        lock (SendLock) {
            var ptr = LENGTH_PREFIX;
            ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
            WriteByte(ref ptr, ref spanRef, (byte) S2CPacketId.QuestObjId);

            WriteInt(ref ptr, ref spanRef, objectId);

            TrySend(ptr);
        }
    }

    public void SendGoto(int objectId, float x, float y) {
        lock (SendLock) {
            var ptr = LENGTH_PREFIX;
            ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
            WriteByte(ref ptr, ref spanRef, (byte) S2CPacketId.GoTo);

            WriteInt(ref ptr, ref spanRef, objectId);
            WriteFloat(ref ptr, ref spanRef, x);
            WriteFloat(ref ptr, ref spanRef, y);

            TrySend(ptr);
        }
    }

    public void SendInventoryResult(byte result) {
        lock (SendLock) {
            var ptr = LENGTH_PREFIX;
            ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
            WriteByte(ref ptr, ref spanRef, (byte) S2CPacketId.InvResult);

            WriteByte(ref ptr, ref spanRef, result);

            TrySend(ptr);
        }
    }

    public void SendMapInfo(
        int width,
        int height,
        string idName,
        string displayName,
        uint seed,
        int difficulty,
        int background,
        bool allowTeleport,
        bool showDisplays,
        int bgLightColor,
        float bgLightIntensity,
        float dayLightIntensity,
        float nightLightIntensity,
        long totalElapsedMicroSeconds) {
        lock (SendLock) {
            var ptr = LENGTH_PREFIX;
            ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
            WriteByte(ref ptr, ref spanRef, (byte) S2CPacketId.MapInfo);

            WriteInt(ref ptr, ref spanRef, width);
            WriteInt(ref ptr, ref spanRef, height);
            WriteString(ref ptr, ref spanRef, idName);
            WriteString(ref ptr, ref spanRef, displayName);

            WriteUInt(ref ptr, ref spanRef, seed);
            WriteInt(ref ptr, ref spanRef, difficulty);
            WriteInt(ref ptr, ref spanRef, background);

            WriteBool(ref ptr, ref spanRef, allowTeleport);
            WriteBool(ref ptr, ref spanRef, showDisplays);

            WriteInt(ref ptr, ref spanRef, bgLightColor);
            WriteFloat(ref ptr, ref spanRef, bgLightIntensity);
            WriteBool(ref ptr, ref spanRef, dayLightIntensity != 0.0);
            if (dayLightIntensity != 0.0) {
                WriteFloat(ref ptr, ref spanRef, dayLightIntensity);
                WriteFloat(ref ptr, ref spanRef, nightLightIntensity);
                WriteLong(ref ptr, ref spanRef, totalElapsedMicroSeconds);
            }

            TrySend(ptr);
        }
    }

    public void SendAccountList(int accountListId, int[] accountIds) {
        lock (SendLock) {
            var ptr = LENGTH_PREFIX;
            ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
            WriteByte(ref ptr, ref spanRef, (byte) S2CPacketId.AccountList);

            WriteInt(ref ptr, ref spanRef, accountListId);

            WriteUShort(ref ptr, ref spanRef, (ushort) accountIds.Length);
            for (var i = 0; i < accountIds.Length; i++)
                WriteInt(ref ptr, ref spanRef, accountIds[i]);

            TrySend(ptr);
        }
    }

    public void SendCreateSuccess(int objectId, int charId) {
        lock (SendLock) {
            var ptr = LENGTH_PREFIX;
            ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
            WriteByte(ref ptr, ref spanRef, (byte) S2CPacketId.CreateSuccess);

            WriteInt(ref ptr, ref spanRef, objectId);
            WriteInt(ref ptr, ref spanRef, charId);

            TrySend(ptr);
        }
    }

    // kinda hacky but needed for client error with no player 
    public void SendErrorText(string text) {
        SendText("*Error*", 0, -1, 0, string.Empty, text);
    }

    public void SendText(string name, int objectId, int numStars, byte bubbleTime, string recipient, string text) {
        lock (SendLock) {
            var ptr = LENGTH_PREFIX;
            ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
            WriteByte(ref ptr, ref spanRef, (byte) S2CPacketId.Text);

            WriteString(ref ptr, ref spanRef, name);
            WriteInt(ref ptr, ref spanRef, objectId);
            WriteInt(ref ptr, ref spanRef, numStars);
            WriteByte(ref ptr, ref spanRef, bubbleTime);
            WriteString(ref ptr, ref spanRef, recipient);
            WriteString(ref ptr, ref spanRef, text);

            TrySend(ptr);
        }
    }

    public void SendAllyShoot(byte bulletId, int ownerId, ushort containerType, float angle) {
        lock (SendLock) {
            var ptr = LENGTH_PREFIX;
            ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
            WriteByte(ref ptr, ref spanRef, (byte) S2CPacketId.AllyShoot);

            WriteByte(ref ptr, ref spanRef, bulletId);
            WriteInt(ref ptr, ref spanRef, ownerId);
            WriteUShort(ref ptr, ref spanRef, containerType);
            WriteFloat(ref ptr, ref spanRef, angle);

            TrySend(ptr);
        }
    }

    public void SendAoe(Position pos, float radius, ushort damage, ConditionEffectIndex effect, float duration,
        ushort origType) {
        lock (SendLock) {
            var ptr = LENGTH_PREFIX;
            ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
            WriteByte(ref ptr, ref spanRef, (byte) S2CPacketId.Aoe);

            WriteFloat(ref ptr, ref spanRef, pos.X);
            WriteFloat(ref ptr, ref spanRef, pos.Y);
            WriteFloat(ref ptr, ref spanRef, radius);
            WriteUShort(ref ptr, ref spanRef, damage);
            WriteByte(ref ptr, ref spanRef, (byte) effect);
            WriteFloat(ref ptr, ref spanRef, duration);
            WriteUShort(ref ptr, ref spanRef, origType);

            TrySend(ptr);
        }
    }

    public void SendBuyResult(BuyResultType code, string result) {
        lock (SendLock) {
            var ptr = LENGTH_PREFIX;
            ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
            WriteByte(ref ptr, ref spanRef, (byte) S2CPacketId.BuyResult);

            WriteInt(ref ptr, ref spanRef, (int) code);
            WriteString(ref ptr, ref spanRef, result);

            TrySend(ptr);
        }
    }

    public void SendDamage(int targetId, ConditionEffects effects, ushort damageAmount, bool kill /*, byte bulletId, int objectId*/) {
        lock (SendLock) {
            var ptr = LENGTH_PREFIX;
            ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
            WriteByte(ref ptr, ref spanRef, (byte) S2CPacketId.Damage);

            WriteInt(ref ptr, ref spanRef, targetId);
            WriteULong(ref ptr, ref spanRef, (ulong) effects);
            WriteUShort(ref ptr, ref spanRef, damageAmount);
            WriteBool(ref ptr, ref spanRef, kill);
            //WriteByte(ref ptr, ref spanRef, bulletId);
            //WriteInt(ref ptr, ref spanRef, objectId);

            TrySend(ptr);
        }
    }

    public void SendDeath(int accountId, int charId, string killedBy) {
        lock (SendLock) {
            var ptr = LENGTH_PREFIX;
            ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
            WriteByte(ref ptr, ref spanRef, (byte) S2CPacketId.Death);

            WriteInt(ref ptr, ref spanRef, accountId);
            WriteInt(ref ptr, ref spanRef, charId);
            WriteString(ref ptr, ref spanRef, killedBy);

            TrySend(ptr);
        }
    }

    public void SendEnemyShoot(byte bulletId, int ownerId, byte bulletType, Position startPos, float angle,
        short damage, byte numShots, float angleInc) {
        lock (SendLock) {
            var ptr = LENGTH_PREFIX;
            ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
            WriteByte(ref ptr, ref spanRef, (byte) S2CPacketId.EnemyShoot);

            WriteByte(ref ptr, ref spanRef, bulletId);
            WriteInt(ref ptr, ref spanRef, ownerId);
            WriteByte(ref ptr, ref spanRef, bulletType);
            WriteFloat(ref ptr, ref spanRef, startPos.X);
            WriteFloat(ref ptr, ref spanRef, startPos.Y);
            WriteFloat(ref ptr, ref spanRef, angle);
            WriteShort(ref ptr, ref spanRef, damage);
            WriteByte(ref ptr, ref spanRef, numShots);
            WriteFloat(ref ptr, ref spanRef, angleInc);

            TrySend(ptr);
        }
    }

    public void SendGuildResult(bool success, string error) {
        lock (SendLock) {
            var ptr = LENGTH_PREFIX;
            ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
            WriteByte(ref ptr, ref spanRef, (byte) S2CPacketId.GuildResult);

            WriteBool(ref ptr, ref spanRef, success);
            WriteString(ref ptr, ref spanRef, error);

            TrySend(ptr);
        }
    }

    public void SendInvitedToGuild(string guildName, string name) {
        lock (SendLock) {
            var ptr = LENGTH_PREFIX;
            ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
            WriteByte(ref ptr, ref spanRef, (byte) S2CPacketId.InvitedToGuild);

            WriteString(ref ptr, ref spanRef, guildName);
            WriteString(ref ptr, ref spanRef, name);

            TrySend(ptr);
        }
    }

    public void SendNameResult(bool success, string error) {
        lock (SendLock) {
            var ptr = LENGTH_PREFIX;
            ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
            WriteByte(ref ptr, ref spanRef, (byte) S2CPacketId.NameResult);

            WriteBool(ref ptr, ref spanRef, success);
            WriteString(ref ptr, ref spanRef, error);

            TrySend(ptr);
        }
    }

    public void SendNotification(int objectId, string error, ARGB color) {
        lock (SendLock) {
            var ptr = LENGTH_PREFIX;
            ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
            WriteByte(ref ptr, ref spanRef, (byte) S2CPacketId.Notification);

            WriteInt(ref ptr, ref spanRef, objectId);
            WriteString(ref ptr, ref spanRef, error);
            WriteByte(ref ptr, ref spanRef, color.A);
            WriteByte(ref ptr, ref spanRef, color.R);
            WriteByte(ref ptr, ref spanRef, color.G);
            WriteByte(ref ptr, ref spanRef, color.B);

            TrySend(ptr);
        }
    }

    public void SendPing(int serial) {
        lock (SendLock) {
            var ptr = LENGTH_PREFIX;
            ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
            WriteByte(ref ptr, ref spanRef, (byte) S2CPacketId.Ping);

            WriteInt(ref ptr, ref spanRef, serial);

            TrySend(ptr);
        }
    }

    public void SendServerPlayerShoot(byte bulletId, int ownerId, ushort containerType, Position startPos, float angle,
        short dmg) {
        lock (SendLock) {
            var ptr = LENGTH_PREFIX;
            ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
            WriteByte(ref ptr, ref spanRef, (byte) S2CPacketId.ServerPlayerShoot);

            WriteByte(ref ptr, ref spanRef, bulletId);
            WriteInt(ref ptr, ref spanRef, ownerId);
            WriteUShort(ref ptr, ref spanRef, containerType);
            WriteFloat(ref ptr, ref spanRef, startPos.X);
            WriteFloat(ref ptr, ref spanRef, startPos.Y);
            WriteFloat(ref ptr, ref spanRef, angle);
            WriteShort(ref ptr, ref spanRef, dmg);

            TrySend(ptr);
        }
    }

    public void SendShowEffect(EffectType effType, int objId, Position pos1, Position pos2, ARGB color) {
        lock (SendLock) {
            var ptr = LENGTH_PREFIX;
            ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
            WriteByte(ref ptr, ref spanRef, (byte) S2CPacketId.ShowEffect);

            WriteByte(ref ptr, ref spanRef, (byte) effType);
            WriteInt(ref ptr, ref spanRef, objId);
            WriteFloat(ref ptr, ref spanRef, pos1.X);
            WriteFloat(ref ptr, ref spanRef, pos1.Y);
            WriteFloat(ref ptr, ref spanRef, pos2.X);
            WriteFloat(ref ptr, ref spanRef, pos2.Y);
            WriteByte(ref ptr, ref spanRef, color.A);
            WriteByte(ref ptr, ref spanRef, color.R);
            WriteByte(ref ptr, ref spanRef, color.G);
            WriteByte(ref ptr, ref spanRef, color.B);

            TrySend(ptr);
        }
    }

    public void SendTradeAccepted(bool[] myOffer, bool[] yourOffer) {
        lock (SendLock) {
            var ptr = LENGTH_PREFIX;
            ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
            WriteByte(ref ptr, ref spanRef, (byte) S2CPacketId.TradeAccepted);

            WriteUShort(ref ptr, ref spanRef, (ushort) myOffer.Length);
            foreach (var b in myOffer)
                WriteBool(ref ptr, ref spanRef, b);

            WriteUShort(ref ptr, ref spanRef, (ushort) yourOffer.Length);
            foreach (var b in yourOffer)
                WriteBool(ref ptr, ref spanRef, b);

            TrySend(ptr);
        }
    }

    public void SendTradeChanged(bool[] offers) {
        lock (SendLock) {
            var ptr = LENGTH_PREFIX;
            ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
            WriteByte(ref ptr, ref spanRef, (byte) S2CPacketId.TradeChanged);

            WriteUShort(ref ptr, ref spanRef, (ushort) offers.Length);
            foreach (var b in offers)
                WriteBool(ref ptr, ref spanRef, b);

            TrySend(ptr);
        }
    }

    public void SendTradeDone(int code, string desc) {
        lock (SendLock) {
            var ptr = LENGTH_PREFIX;
            ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
            WriteByte(ref ptr, ref spanRef, (byte) S2CPacketId.TradeDone);

            WriteInt(ref ptr, ref spanRef, code);
            WriteString(ref ptr, ref spanRef, desc);

            TrySend(ptr);
        }
    }

    public void SendTradeRequested(string name) {
        lock (SendLock) {
            var ptr = LENGTH_PREFIX;
            ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
            WriteByte(ref ptr, ref spanRef, (byte) S2CPacketId.TradeRequested);

            WriteString(ref ptr, ref spanRef, name);

            TrySend(ptr);
        }
    }

    public void SendTradeStart(TradeItem[] myItems, string yourName, TradeItem[] yourItems) {
        lock (SendLock) {
            var ptr = LENGTH_PREFIX;
            ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
            WriteByte(ref ptr, ref spanRef, (byte) S2CPacketId.TradeAccepted);

            WriteUShort(ref ptr, ref spanRef, (ushort) myItems.Length);
            foreach (var item in myItems) {
                WriteInt(ref ptr, ref spanRef, item.Item);
                WriteInt(ref ptr, ref spanRef, item.SlotType);
                WriteBool(ref ptr, ref spanRef, item.Tradeable);
                WriteBool(ref ptr, ref spanRef, item.Included);
            }

            WriteString(ref ptr, ref spanRef, yourName);

            WriteUShort(ref ptr, ref spanRef, (ushort) yourItems.Length);
            foreach (var item in yourItems) {
                WriteInt(ref ptr, ref spanRef, item.Item);
                WriteInt(ref ptr, ref spanRef, item.SlotType);
                WriteBool(ref ptr, ref spanRef, item.Tradeable);
                WriteBool(ref ptr, ref spanRef, item.Included);
            }

            TrySend(ptr);
        }
    }

    public void SendNewTick(int tickId, int tickTime, ObjectStats[] statuses)
    {
        lock (SendLock)
        {
            var ptr = LENGTH_PREFIX;
            ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
            WriteByte(ref ptr, ref spanRef, (byte)S2CPacketId.NewTick);

            WriteInt(ref ptr, ref spanRef, tickId);
            WriteInt(ref ptr, ref spanRef, tickTime);
            WriteUShort(ref ptr, ref spanRef, (ushort)statuses.Length);
            foreach (var status in statuses)
            {
                WriteInt(ref ptr, ref spanRef, status.Id);
                WriteFloat(ref ptr, ref spanRef, status.Position.X);
                WriteFloat(ref ptr, ref spanRef, status.Position.Y);
                WriteUShort(ref ptr, ref spanRef, (ushort)status.Stats.Length);
                var lengthPtr = ptr;
                WriteUShort(ref ptr, ref spanRef, 0);
                foreach (var stat in status.Stats) WriteStat(ref ptr, ref spanRef, stat.Key, stat.Value);
                WriteUShort(ref lengthPtr, ref spanRef, (ushort)(ptr - lengthPtr - 2));
            }

            TrySend(ptr);
        }
    }

    public void SendUpdate(List<TileData> tiles, List<ObjectDef> newObjs, List<int> drops)
    {
        lock (SendLock)
        {
            var ptr = LENGTH_PREFIX;
            ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
            WriteByte(ref ptr, ref spanRef, (byte)S2CPacketId.Update);

            WriteUShort(ref ptr, ref spanRef, (ushort)tiles.Count);
            foreach (var tile in tiles)
            {
                WriteShort(ref ptr, ref spanRef, tile.X);
                WriteShort(ref ptr, ref spanRef, tile.Y);
                WriteUShort(ref ptr, ref spanRef, tile.Tile);
            }

            WriteUShort(ref ptr, ref spanRef, (ushort)drops.Count);
            foreach (var drop in drops)
                WriteInt(ref ptr, ref spanRef, drop);

            WriteUShort(ref ptr, ref spanRef, (ushort)newObjs.Count);
            foreach (var newObj in newObjs)
            {
                WriteUShort(ref ptr, ref spanRef, newObj.ObjectType);
                WriteInt(ref ptr, ref spanRef, newObj.Stats.Id);
                WriteFloat(ref ptr, ref spanRef, newObj.Stats.Position.X);
                WriteFloat(ref ptr, ref spanRef, newObj.Stats.Position.Y);
                WriteUShort(ref ptr, ref spanRef, (ushort)newObj.Stats.Stats.Length);
                var lengthPtr = ptr;
                WriteUShort(ref ptr, ref spanRef, 0);
                foreach (var stat in newObj.Stats.Stats) WriteStat(ref ptr, ref spanRef, stat.Key, stat.Value);
                WriteUShort(ref lengthPtr, ref spanRef, (ushort)(ptr - lengthPtr - 2));
            }

            TrySend(ptr);
        }
    }

    public static void WriteStat(ref int ptr, ref byte spanRef, StatsType stat, object val) {
        WriteByte(ref ptr, ref spanRef, (byte) stat);
        switch (val) {
            //hack
            case CurrencyType value:
                WriteByte(ref ptr, ref spanRef, (byte) value);
                break;
            case byte value:
                WriteByte(ref ptr, ref spanRef, value);
                break;
            case bool value:
                WriteBool(ref ptr, ref spanRef, value);
                break;
            case short value:
                WriteShort(ref ptr, ref spanRef, value);
                break;
            case ushort value:
                WriteUShort(ref ptr, ref spanRef, value);
                break;
            case int value:
                WriteInt(ref ptr, ref spanRef, value);
                break;
            case uint value:
                WriteUInt(ref ptr, ref spanRef, value);
                break;
            case string value:
                WriteString(ref ptr, ref spanRef, value);
                break;
            case long value:
                WriteLong(ref ptr, ref spanRef, value);
                break;
            case ulong value:
                WriteULong(ref ptr, ref spanRef, value);
                break;
            default:
                Log.Error($"Unhandled stat {stat}, value: {val}");
                break;
        }
    }

    private async void TrySend(int len) {
        if (!Socket.Connected)
            return;

        try {
            //Log.Error($"Sending packet {(S2CPacketId) SendMem.Span[LENGTH_PREFIX]} {len}");
            BinaryPrimitives.WriteUInt16LittleEndian(SendMem.Span, (ushort) (len - LENGTH_PREFIX));
            _ = await Socket.SendAsync(SendMem[..len]);
        }
        catch (Exception e) {
            Disconnect();
            if (e is not SocketException se || se.SocketErrorCode != SocketError.ConnectionReset &&
                se.SocketErrorCode != SocketError.Shutdown)
                Log.Error($"{Account?.Name ?? "[unconnected]"} ({IP}): {e}");
        }
    }

    #endregion
}