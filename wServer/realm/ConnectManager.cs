﻿using common;
using NLog;
using wServer.networking;
using wServer.networking.packets.incoming;
using wServer.networking.packets.outgoing;
using wServer.realm.entities;
using wServer.realm.worlds;
using wServer.realm.worlds.logic;

namespace wServer.realm;

public class ConnectManager
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly RealmManager _manager;
    private readonly int _maxPlayerCount;
    private readonly int _maxPlayerCountWithPriority;

    public ConnectManager(RealmManager manager, int maxPlayerCount, int maxPlayerCountWithPriority)
    {
        _manager = manager;
        _maxPlayerCount = maxPlayerCount;
        _maxPlayerCountWithPriority = maxPlayerCountWithPriority;
    }
    
    public int GetPlayerCount()
    {
        return _manager.Clients.Count;
    }
    
    public static void Reconnect(Client client, int gameId) 
    {
        var player = client.Player;
        var currentWorld = client.Player.Owner;

        var world = client.Manager.GetWorld(gameId);
        if (world == null || world.Deleted) {
            world = client.Manager.GetWorld(World.Nexus);
        }

        if (!world.AllowedAccess(client)) {
            if (gameId == World.Nexus) {
                client.Disconnect();
                return;
            }

            world = client.Manager.GetWorld(World.Nexus);
        }

        // send out map info
        var mapSize = Math.Max(world.Map.Width, world.Map.Height);
        client.SendPacket(new MapInfo
        {
            Width = mapSize,
            Height = mapSize,
            Name = world.IdName,
            DisplayName = world.DisplayName,
            Seed = client.Seed,
            Background = world.Background,
            Difficulty = world.Difficulty,
            AllowPlayerTeleport = world.AllowTeleport,
            ShowDisplays = world.ShowDisplays,
            
            BackgroundLightColor = world.BgLightColor,
            BackgroundLightIntensity = world.BgLightIntensity,
            DayLightIntensity = world.DayLightIntensity,
            NightLightIntensity = world.NightLightIntensity,
            GameTime = (int) world.Manager.Logic.WorldTime.TotalElapsedMs
        });

        // send out account lock/ignore list
        client.SendPacket(new AccountList
        {
            AccountListId = 0, // locked list
            AccountIds = client.Account.LockList
        });
        client.SendPacket(new AccountList
        {
            AccountListId = 1, // ignore list
            AccountIds = client.Account.IgnoreList
        });

        if (client.Character != null) {
            if (client.Character.Dead) {
                client.SendFailure("Character is dead");
                return;
            }

            currentWorld?.LeaveWorld(player);
            player.CleanupReconnect();

            // dispose update
            var objectId = world.EnterWorld(player, true);
            client.SendPacket(new CreateSuccess
            {
                CharId = client.Character.CharId,
                ObjectId = objectId
            });
        }
        else {
            client.SendFailure("Failed to load character");
        }

        client.Reconnecting = false;
    }


    public static void Connect(Client client, Hello pkt)
    {
        var acc = client.Account;
        if (!client.Manager.Database.AcquireLock(acc))
        {
            // disconnect current connected client (if any)

            var otherClients = client.Manager.Clients.Keys.Where(c => c == client || c.Account != null && (c.Account.AccountId == acc.AccountId));
            foreach (var otherClient in otherClients)
                otherClient.Disconnect();

            // try again...
            if (!client.Manager.Database.AcquireLock(acc))
            {
                client.SendFailure($"Account in Use ({client.Manager.Database.GetLockTime(acc)?.ToString("%s")} seconds until timeout)");
                return;
            }
        }

        acc.Reload(); // make sure we have the latest data
        client.Account = acc;

        // connect client to realm manager
        if (!client.Manager.TryConnect(client))
        {
            client.SendFailure("Failed to connect");
            return;
        }

        var world = client.Manager.GetWorld(pkt.GameId);
        if (world == null || world.Deleted)
        {
            client.SendPacket(new Text
            {
                BubbleTime = 0,
                NumStars = -1,
                Name = "*Error*",
                Txt = "World does not exist."
            });
            world = client.Manager.GetWorld(World.Nexus);
        }

        if(world == null)
        {
            client.SendPacket(new Text
            {
                BubbleTime = 0,
                NumStars = -1,
                Name = "*Error*",
                Txt = "Failed to parse instance"
            });
            world = client.Manager.GetWorld(World.Nexus);
        }

        if (!world.AllowedAccess(client))
        {
            if (!world.Persist && world.TotalConnects <= 0)
                client.Manager.RemoveWorld(world);

            client.SendPacket(new Text
            {
                BubbleTime = 0,
                NumStars = -1,
                Name = "*Error*",
                Txt = "Access denied"
            });

            if (world is not Nexus)
                world = client.Manager.GetWorld(World.Nexus);
            else
            {
                client.Disconnect();
                return;
            }
        }

        var seed = (uint)((long)Environment.TickCount * client.Account.AccountId.GetHashCode()) % uint.MaxValue;
        client.Random = new wRandom(seed);
        client.Seed = seed;
        client.TargetWorld = world.Id;

        var now = (int) DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

        if (acc.GuildId > 0 && now - acc.LastSeen > 1800)
            client.Manager.Chat.GuildAnnounce(acc, acc.Name + " has joined the game");

        acc.RefreshLastSeen();
        acc.FlushAsync();

        // send out map info
        var mapSize = Math.Max(world.Map.Width, world.Map.Height);
        client.SendPacket(new MapInfo
        {
            Width = mapSize,
            Height = mapSize,
            Name = world.IdName,
            DisplayName = world.DisplayName,
            Seed = seed,
            Background = world.Background,
            Difficulty = world.Difficulty,
            AllowPlayerTeleport = world.AllowTeleport,
            ShowDisplays = world.ShowDisplays,
            
            BackgroundLightColor = world.BgLightColor,
            BackgroundLightIntensity = world.BgLightIntensity,
            DayLightIntensity = world.DayLightIntensity,
            NightLightIntensity = world.NightLightIntensity,
            GameTime = (int) world.Manager.Logic.WorldTime.TotalElapsedMs
        });

        // send out account lock/ignore list
        client.SendPacket(new AccountList
        {
            AccountListId = 0, // locked list
            AccountIds = client.Account.LockList
        });
        client.SendPacket(new AccountList
        {
            AccountListId = 1, // ignore list
            AccountIds = client.Account.IgnoreList
        });


        // either create or load the character

        DbChar character = null;

        if (pkt.CreateCharacter)
        {
            var status = client.Manager.Database.CreateCharacter(acc, pkt.CharacterType, pkt.SkinType, out character);
            switch (status)
            {
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
        }
        else
            character = client.Manager.Database.LoadCharacter(acc, pkt.CharId);

        // didnt load then disconnect
        if(character == null)
        {
            client.SendFailure("Failed to load character");
            return;
        }

        // dead? then disconnect
        if (character.Dead)
        {
            client.SendFailure("Character is dead");
            return;
        }

        // make the player

        client.Character = character;

        if (client.Player?.Owner == null)
            client.Player = new Player(client);

        client.Manager.Worlds[world.Id].EnterWorld(client.Player);

        client.SendPacket(new CreateSuccess()
        {
            CharId = client.Character.CharId,
            ObjectId = client.Player.Id
        });

        client.Manager.Clients[client].WorldInstance = client.Player.Owner.Id;
        client.Manager.Clients[client].WorldName = client.Player.Owner.IdName;

        client.State = ProtocolState.Handshaked;
    }
}