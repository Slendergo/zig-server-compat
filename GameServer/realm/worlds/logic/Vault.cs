﻿using Shared;
using Shared.resources;
using GameServer.realm.entities;
using GameServer.realm.entities.vendors;

namespace GameServer.realm.worlds.logic;

public class Vault : World {
    private readonly Client _client;

    private LinkedList<Container> vaults;

    public Vault(RealmManager manager, WorldTemplateData template, Client client)
        : base(manager, template) {
        _client = client;
        AccountId = _client.Account.AccountId;
        vaults = new LinkedList<Container>();
    }

    public int AccountId { get; }

    public override bool AllowedAccess(Client client) {
        return base.AllowedAccess(client) && AccountId == client.Account.AccountId;
    }

    public override void Init() {
        var vaultChestPosition = new List<IntPoint>();
        var spawn = new IntPoint(0, 0);

        var w = Map.Width;
        var h = Map.Height;

        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++) {
            var tile = Map[x, y];
            switch (tile.Region) {
                case TileRegion.Spawn:
                    spawn = new IntPoint(x, y);
                    break;
                case TileRegion.Vault:
                    vaultChestPosition.Add(new IntPoint(x, y));
                    break;
            }
        }

        vaultChestPosition.Sort((x, y) => Comparer<int>.Default.Compare(
            (x.X - spawn.X) * (x.X - spawn.X) + (x.Y - spawn.Y) * (x.Y - spawn.Y),
            (y.X - spawn.X) * (y.X - spawn.X) + (y.Y - spawn.Y) * (y.Y - spawn.Y)));

        for (var i = 0; i < _client.Account.VaultCount && vaultChestPosition.Count > 0; i++) {
            var vaultChest = new DbVaultSingle(_client.Account, i);
            var con = new Container(_client.Manager, 0x0504, null, false, vaultChest);
            con.BagOwners = new[] {_client.Account.AccountId};
            con.Inventory.SetItems(vaultChest.Items);
            con.Inventory.InventoryChanged += (sender, e) => SaveChest(((Inventory) sender).Parent);
            con.Move(vaultChestPosition[0].X + 0.5f, vaultChestPosition[0].Y + 0.5f);
            EnterWorld(con);
            vaultChestPosition.RemoveAt(0);
            vaults.AddFirst(con);
        }

        foreach (var i in vaultChestPosition) {
            var x = new ClosedVaultChest(_client.Manager, 0x0505);
            x.Move(i.X + 0.5f, i.Y + 0.5f);
            EnterWorld(x);
        }
    }

    public void AddChest(Entity original) {
        var vaultChest = new DbVaultSingle(_client.Account, _client.Account.VaultCount - 1);
        var con = new Container(_client.Manager, 0x0504, null, false, vaultChest);
        con.BagOwners = new[] {_client.Account.AccountId};
        con.Inventory.SetItems(vaultChest.Items);
        con.Inventory.InventoryChanged += (sender, e) => SaveChest(((Inventory) sender).Parent);
        con.Move(original.X, original.Y);
        LeaveWorld(original);
        EnterWorld(con);
        vaults.AddFirst(con);
    }

    private void SaveChest(IContainer chest) {
        var dbLink = chest?.DbLink;
        if (dbLink == null)
            return;

        dbLink.Items = chest.Inventory.GetItemTypes();
        dbLink.FlushAsync();
    }

    public override void LeaveWorld(Entity entity) {
        base.LeaveWorld(entity);

        if (entity.ObjectType != 0x0744)
            return;

        var x = new StaticObject(_client.Manager, 0x0743, null, true, false, false);
        x.Move(entity.X, entity.Y);
        EnterWorld(x);
    }
}