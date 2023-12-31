﻿using System.ComponentModel;
using Shared;
using Shared.resources;
using GameServer.realm.entities.player;
using GameServer.realm.worlds.logic;

namespace GameServer.realm.entities.vendors;

public enum BuyResult {
    [Description("Purchase successful.")] Ok,

    [Description("Cannot purchase items with a guest account.")]
    IsGuest,
    [Description("Insufficient Rank.")] InsufficientRank,
    [Description("Insufficient Funds.")] InsufficientFunds,

    [Description("Can't buy items on a test map.")]
    IsTestMap,
    [Description("Uninitalized.")] Uninitialized,
    [Description("Transaction failed.")] TransactionFailed,

    [Description("Item is currently being purchased.")]
    BeingPurchased,

    [Description("You don't have enough inventory slots.")]
    NotEnoughSlots
}

public abstract class SellableObject : StaticObject {
    protected static Random Rand = new();
    private readonly SV<CurrencyType> _currency;

    private readonly SV<int> _price;
    private readonly SV<int> _rankReq;

    protected SellableObject(RealmManager manager, ushort objType)
        : base(manager, objType, null, true, false, false) {
        _price = new SV<int>(this, StatsType.SellablePrice, 0);
        _currency = new SV<CurrencyType>(this, StatsType.SellablePriceCurrency, 0);
        _rankReq = new SV<int>(this, StatsType.SellableRankRequirement, 0);
    }

    public int Price {
        get => _price.GetValue();
        set => _price.SetValue(value);
    }

    public CurrencyType Currency {
        get => _currency.GetValue();
        set => _currency.SetValue(value);
    }

    public int RankReq {
        get => _rankReq.GetValue();
        set => _rankReq.SetValue(value);
    }

    public virtual void Buy(Player player) {
        SendFailed(player, BuyResult.Uninitialized);
    }

    protected override void ExportStats(IDictionary<StatsType, object> stats) {
        stats[StatsType.SellablePrice] = Price;
        stats[StatsType.SellablePriceCurrency] = Currency;
        stats[StatsType.SellableRankRequirement] = RankReq;
        base.ExportStats(stats);
    }

    protected BuyResult ValidateCustomer(Player player, Item item) {
        if (Owner is Test)
            return BuyResult.IsTestMap;
        if (player.Stars < RankReq)
            return BuyResult.InsufficientRank;

        var acc = player.Client.Account;
        if (acc.Guest) {
            // reload guest prop just in case user registered in game
            acc.Reload("guest");
            if (acc.Guest)
                return BuyResult.IsGuest;
        }

        if (player.GetCurrency(Currency) < Price)
            return BuyResult.InsufficientFunds;

        if (item != null) // not perfect, but does the job for now
        {
            var availableSlot = player.Inventory.CreateTransaction().GetAvailableInventorySlot(item);
            if (availableSlot == -1)
                return BuyResult.NotEnoughSlots;
        }

        return BuyResult.Ok;
    }

    protected void SendFailed(Player player, BuyResult result) {
        player.Client.SendBuyResult(BuyResultType.NotEnoughGold, $"Purchase Error: {result.GetDescription()}");
    }
}