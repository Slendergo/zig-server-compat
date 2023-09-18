using common;
using wServer.realm.worlds.logic;

namespace wServer.realm.entities;

partial class Player {
    internal Dictionary<Player, int> potentialTrader = new();
    internal bool[] trade;
    internal bool tradeAccepted;
    internal Player tradeTarget;

    public void RequestTrade(string name) {
        if (Owner is Test)
            return;

        Manager.Database.ReloadAccount(Client.Account);
        var acc = Client.Account;

        if (!acc.NameChosen) {
            SendErrorText("A unique name is required before trading with others!");
            return;
        }

        if (tradeTarget != null) {
            SendErrorText("Already trading!");
            return;
        }

        if (Database.GuestNames.Contains(name)) {
            SendErrorText(name + " needs to choose a unique name first!");
            return;
        }

        var target = Owner.GetUniqueNamedPlayer(name);
        if (target == null) {
            SendErrorText(name + " not found!");
            return;
        }

        if (target == this) {
            SendErrorText("You can't trade with yourself!");
            return;
        }

        if (target.Client.Account.IgnoreList.Contains(AccountId))
            return; // account is ignored

        if (target.tradeTarget != null) {
            SendErrorText(target.Name + " is already trading!");
            return;
        }

        if (potentialTrader.ContainsKey(target)) {
            tradeTarget = target;
            trade = new bool[12];
            tradeAccepted = false;
            target.tradeTarget = this;
            target.trade = new bool[12];
            target.tradeAccepted = false;
            potentialTrader.Clear();
            target.potentialTrader.Clear();

            // shouldn't be needed since there is checks on
            // invswap, invdrop, and useitem packets for trading
            //MonitorTrade();
            //target.MonitorTrade();

            var my = new TradeItem[12];
            for (var i = 0; i < 12; i++)
                my[i] = new TradeItem {
                    Item = Inventory[i] == null ? -1 : Inventory[i].ObjectType,
                    SlotType = SlotTypes[i],
                    Included = false,
                    Tradeable = Inventory[i] != null && i >= 4 && !Inventory[i].Soulbound
                };
            var your = new TradeItem[12];
            for (var i = 0; i < 12; i++)
                your[i] = new TradeItem {
                    Item = target.Inventory[i] == null ? -1 : target.Inventory[i].ObjectType,
                    SlotType = target.SlotTypes[i],
                    Included = false,
                    Tradeable = target.Inventory[i] != null && i >= 4 && !target.Inventory[i].Soulbound
                };

            Client.SendTradeStart(my, target.Name, your);
            target.Client.SendTradeStart(your, Name, my);
        }
        else {
            target.potentialTrader[this] = 1000 * 20;
            target.Client.SendTradeRequested(Name);
            SendInfo("You have sent a trade request to " + target.Name + "!");
        }
    }

    public void CancelTrade() {
        Client.SendTradeDone(1, "Trade canceled!");

        if (tradeTarget != null && tradeTarget.Client != null)
            tradeTarget.Client.SendTradeDone(1, "Trade canceled!");

        ResetTrade();
    }

    public void ResetTrade() {
        if (tradeTarget != null) {
            tradeTarget.tradeTarget = null;
            tradeTarget.trade = null;
            tradeTarget.tradeAccepted = false;
        }

        tradeTarget = null;
        trade = null;
        tradeAccepted = false;
    }

    private void CheckTradeTimeout(RealmTime time) {
        var newState = new List<Tuple<Player, int>>();
        foreach (var i in potentialTrader)
            newState.Add(new Tuple<Player, int>(i.Key, i.Value - time.ElapsedMsDelta));

        foreach (var i in newState)
            if (i.Item2 < 0) {
                i.Item1.SendInfo("Trade to " + Name + " has timed out!");
                potentialTrader.Remove(i.Item1);
            }
            else {
                potentialTrader[i.Item1] = i.Item2;
            }
    }
}