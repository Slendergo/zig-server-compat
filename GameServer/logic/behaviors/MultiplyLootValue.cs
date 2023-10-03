using System.Xml.Linq;
using Shared;
using GameServer.realm;

namespace GameServer.logic.behaviors;

internal class MultiplyLootValue : Behavior {
    //State storage: cooldown timer

    private int multiplier;

    public MultiplyLootValue(XElement e) {
        multiplier = e.ParseInt("@multiplier");
    }

    public MultiplyLootValue(int multiplier) {
        this.multiplier = multiplier;
    }

    protected override void OnStateEntry(Entity host, RealmTime time, ref object state) {
        state = false;
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state) {
        var multiplied = (bool) state;
        if (!multiplied) {
            var newLootValue = host.LootValue * multiplier;
            host.LootValue = newLootValue;
            multiplied = true;
        }

        state = multiplied;
    }
}