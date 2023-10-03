using Shared.resources;
using GameServer.realm.entities;
using GameServer.realm.entities.player;

namespace GameServer.realm;

public class ItemStacker {
    private readonly SV<int> _count;

    public ItemStacker(Player owner, int slot, ushort objectType, int count, int maxCount) {
        Owner = owner;
        Slot = slot;
        Item = Owner.Manager.Resources.GameData.Items[objectType];
        MaxCount = maxCount;

        _count = new SV<int>(owner, GetStatsType(slot), count);
    }

    public Player Owner { get; }
    public int Slot { get; private set; }
    public Item Item { get; }
    public int MaxCount { get; }

    public int Count {
        get => _count.GetValue();
        set => _count.SetValue(value);
    }

    public Item Put(Item item) {
        if (Count < MaxCount && item == Item) {
            Count++;
            return null;
        }

        return item;
    }

    public Item Pull() {
        if (Count > 0) {
            Count--;
            return Item;
        }

        return null;
    }

    private static StatsType GetStatsType(int slot) {
        switch (slot) {
            case 254:
                return StatsType.HealthStackCount;
            case 255:
                return StatsType.MagicStackCount;
            default:
                return StatsType.None;
        }
    }
}