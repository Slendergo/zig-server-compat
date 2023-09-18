using common.resources;
using wServer.networking;
using wServer.realm.worlds.logic;

namespace wServer.realm.entities.vendors;

internal class ClosedVaultChest : SellableObject {
    public ClosedVaultChest(RealmManager manager, ushort objType) : base(manager, objType) {
        Price = 750;
        Currency = CurrencyType.Fame;
    }

    public override void Buy(Player player) {
        var result = ValidateCustomer(player, null);
        if (result != BuyResult.Ok) {
            SendFailed(player, result);
            return;
        }

        var db = Manager.Database;
        var acc = player.Client.Account;

        var trans = db.Conn.CreateTransaction();
        Manager.Database.CreateChest(acc, trans);
        var t1 = db.UpdateCurrency(acc, -Price, Currency, trans);
        var t2 = trans.ExecuteAsync();
        Task.WhenAll(t1, t2).ContinueWith(t => {
            if (t.IsCanceled) {
                SendFailed(player, BuyResult.TransactionFailed);
                return;
            }

            acc.Reload("vaultCount");
            player.CurrentFame = acc.Fame;

            (Owner as Vault)?.AddChest(this);
            player.Client.SendBuyResult(BuyResultType.Success, "Vault chest purchased!");
        }).ContinueWith(e =>
                Log.Error(e.Exception.InnerException.ToString()),
            TaskContinuationOptions.OnlyOnFaulted);
    }
}