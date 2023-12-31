﻿using GameServer.realm.entities.player;
using GameServer.realm.worlds;
using GameServer.realm.worlds.logic;
using NLog;
using Shared;

namespace GameServer.realm;

public class ConnectManager {
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly RealmManager _manager;
    private readonly int _maxPlayerCount;
    private readonly int _maxPlayerCountWithPriority;

    public ConnectManager(RealmManager manager, int maxPlayerCount, int maxPlayerCountWithPriority) {
        _manager = manager;
        _maxPlayerCount = maxPlayerCount;
        _maxPlayerCountWithPriority = maxPlayerCountWithPriority;
    }

    public int GetPlayerCount() {
        return _manager.Clients.Count;
    }

    public static void Reconnect(Client client, int gameId) {
        var player = client.Player;
        var currentWorld = client.Player.Owner;

        var world = client.Manager.GetWorld(gameId);
        if (world == null || world.Deleted) 
            world = client.Manager.GetWorld(World.Nexus);

        if (!world.AllowedAccess(client)) {
            if (gameId == World.Nexus) {
                client.Disconnect();
                return;
            }

            world = client.Manager.GetWorld(World.Nexus);
        }

        var seed = (uint)((long)Environment.TickCount * client.Account.AccountId.GetHashCode()) % uint.MaxValue;
        client.SeededRandom = new WRandom(seed);

        // send out map info
        var mapSize = Math.Max(world.Map.Width, world.Map.Height);
        client.SendMapInfo(mapSize, mapSize, world.IdName, world.DisplayName, seed, world.Difficulty,
            world.Background, world.AllowTeleport, world.ShowDisplays, world.BgLightColor, world.BgLightIntensity,
            world.DayLightIntensity, world.NightLightIntensity, world.Manager.Logic.RealmTime.TotalElapsedMicroSeconds);

        // send out account lock/ignore list
        client.SendAccountList(0, client.Account.LockList);
        client.SendAccountList(1, client.Account.IgnoreList);

        if (client.Character != null) {
            if (client.Character.Dead) {
                client.Disconnect("Character is dead");
                return;
            }

            currentWorld?.LeaveWorld(player);
            player.CleanupReconnect();

            // dispose update
            var objectId = world.EnterWorld(player, true);
            client.SendCreateSuccess(objectId, client.Character.CharId);
        }
        else {
            client.Disconnect("Failed to load character");
        }
    }


    public static void Connect(Client client, int gameId, short charId, bool createChar, ushort charType,
        ushort skinType) {
        var acc = client.Account;
        if (!client.Manager.Database.AcquireLock(acc)) {
            // disconnect current connected client (if any)

            var otherClients = client.Manager.Clients.Keys.Where(c =>
                c == client || c.Account != null && c.Account.AccountId == acc.AccountId);
            foreach (var otherClient in otherClients)
                otherClient.Disconnect();

            // try again...
            if (!client.Manager.Database.AcquireLock(acc)) {
                client.Disconnect(
                    $"Account in Use ({client.Manager.Database.GetLockTime(acc)?.ToString("%s")} seconds until timeout)");
                return;
            }
        }

        acc.Reload(); // make sure we have the latest data
        client.Account = acc;

        // connect client to realm manager
        if (!client.Manager.TryConnect(client)) {
            client.Disconnect("Failed to connect");
            return;
        }

        var world = client.Manager.GetWorld(gameId);
        if (world == null || world.Deleted) {
            client.SendErrorText("World does not exist");

            world = client.Manager.GetWorld(World.Nexus);
        }

        if (world == null) {
            client.SendErrorText("Failed to parse instance");

            world = client.Manager.GetWorld(World.Nexus);
        }

        if (!world.AllowedAccess(client)) {
            if (!world.Persists /*&& world.TotalConnects <= 0*/) // not sure why this exists might be needed idk removed it for now can read back later if it causes issues
                client.Manager.RemoveWorld(world);

            client.SendErrorText("Access denied");

            if (world is not Nexus) {
                world = client.Manager.GetWorld(World.Nexus);
            }
            else {
                client.Disconnect();
                return;
            }
        }

        var now = (int) DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        if (acc.GuildId > 0 && now - acc.LastSeen > 1800)
            client.Manager.Chat.GuildAnnounce(acc, acc.Name + " has joined the game");

        acc.RefreshLastSeen();
        acc.FlushAsync();

        var seed = (uint)((long)Environment.TickCount * client.Account.AccountId.GetHashCode()) % uint.MaxValue;
        client.SeededRandom = new WRandom(seed);

        // send out map info
        var mapSize = Math.Max(world.Map.Width, world.Map.Height);
        client.SendMapInfo(mapSize, mapSize, world.IdName, world.DisplayName, seed, world.Difficulty,
            world.Background, world.AllowTeleport, world.ShowDisplays, world.BgLightColor, world.BgLightIntensity,
            world.DayLightIntensity, world.NightLightIntensity, world.Manager.Logic.RealmTime.TotalElapsedMicroSeconds);

        // send out account lock/ignore list
        client.SendAccountList(0, client.Account.LockList);
        client.SendAccountList(1, client.Account.IgnoreList);

        // either create or load the character

        DbChar character = null;

        if (createChar) {
            var status = client.Manager.Database.CreateCharacter(acc, charType, skinType, out character);
            switch (status) {
                case CreateStatus.ReachCharLimit:
                    client.Disconnect("Too many characters");
                    return;
                case CreateStatus.SkinUnavailable:
                    client.Disconnect("Skin unavailable");
                    return;
                case CreateStatus.Locked:
                    client.Disconnect("Class locked");
                    return;
            }
        }
        else {
            character = client.Manager.Database.LoadCharacter(acc, charId);
        }

        // didnt load then disconnect
        if (character == null) {
            client.Disconnect("Failed to load character");
            return;
        }

        // dead? then disconnect
        if (character.Dead) {
            client.Disconnect("Character is dead");
            return;
        }

        // make the player

        client.Character = character;

        if (client.Player?.Owner == null)
            client.Player = new Player(client);

        client.Manager.Worlds[world.Id].EnterWorld(client.Player);

        client.SendCreateSuccess(client.Player.Id, client.Character.CharId);

        client.Manager.Clients[client].WorldInstance = client.Player.Owner.Id;
        client.Manager.Clients[client].WorldName = client.Player.Owner.IdName;
    }
}