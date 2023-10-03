using Shared.resources;
using GameServer.realm.entities.player;

namespace GameServer.realm.entities.vendors;

internal class GuildMerchant : SellableObject {
    private readonly int[] _hallLevels = {1, 2, 3};
    private readonly int[] _hallPrices = {10000, 100000, 250000};
    private readonly int[] _hallTypes = {0x736, 0x737, 0x738};

    private readonly int _upgradeLevel;

    public GuildMerchant(RealmManager manager, ushort objType) : base(manager, objType) {
        Currency = CurrencyType.Fame;
        Price = int.MaxValue; // just in case for some reason _hallType isn't found
        for (var i = 0; i < _hallTypes.Length; i++) {
            if (objType != _hallTypes[i])
                continue;

            Price = _hallPrices[i];
            _upgradeLevel = _hallLevels[i];
        }
    }

    public override void Buy(Player player) {
        var account = player.Manager.Database.GetAccount(player.AccountId);
        var guild = player.Manager.Database.GetGuild(account.GuildId);


        if (guild.IsNull || account.GuildRank < 30) {
            player.SendErrorText("Verification failed.");
            return;
        }

        if (guild.Fame < Price) {
            player.Client.SendBuyResult(BuyResultType.NotEnoughGuildFame, "Not enough Guild Fame!");
            return;
        }

        // change guild level
        if (!player.Manager.Database.ChangeGuildLevel(guild, _upgradeLevel)) {
            player.SendErrorText("Internal server error.");
            return;
        }

        player.Manager.Database.UpdateGuildFame(guild, -Price);
        player.Client.SendBuyResult(BuyResultType.Success,
            "Upgrade successful! Please leave the Guild Hall to have it upgraded.");
    }
}