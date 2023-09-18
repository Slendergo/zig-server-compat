using System.Collections;
using common;
using common.resources;
using wServer.realm.entities;

namespace wServer.realm;

public interface IContainer {
    int[] SlotTypes { get; }
    Inventory Inventory { get; }
    RInventory DbLink { get; }
}

public class InventoryChangedEventArgs : EventArgs {
    //index = -1 -> reset
    public InventoryChangedEventArgs(Item[] old, Item[] @new) {
        OldItems = old;
        NewItems = @new;
    }

    public Item[] OldItems { get; private set; }
    public Item[] NewItems { get; private set; }
}

public class InventoryTransaction : IEnumerable<Item> {
    private readonly Item[] _changedItems;
    private readonly Item[] _originalItems;
    private readonly IContainer _parent;

    public InventoryTransaction(IContainer parent) {
        _parent = parent;
        _originalItems = parent.Inventory.GetItems();
        _changedItems = (Item[]) _originalItems.Clone();
    }

    public int Length => _originalItems.Length;

    public Item this[int index] {
        get => _changedItems[index];
        set => _changedItems[index] = value;
    }

    public IEnumerator<Item> GetEnumerator() {
        return ((IEnumerable<Item>) _changedItems).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return _changedItems.GetEnumerator();
    }

    public bool Validate(bool revert = false) {
        if (_parent == null)
            return false;

        var items = revert ? _changedItems : _originalItems;

        for (var i = 0; i < items.Length; i++)
            if (items[i] != _parent.Inventory[i])
                return false;

        return true;
    }

    public void Execute() {
        var inv = _parent.Inventory;
        for (var i = 0; i < inv.Length; i++)
            if (_originalItems[i] != _changedItems[i])
                inv[i] = _changedItems[i];
    }

    public void Revert() {
        var inv = _parent.Inventory;
        for (var i = 0; i < inv.Length; i++)
            if (_originalItems[i] != _changedItems[i])
                inv[i] = _originalItems[i];
    }

    public int GetAvailableInventorySlot(Item item) {
        if (_parent is Player plr) {
            var playerDesc = plr.Manager.Resources.GameData
                .Classes[plr.ObjectDesc.ObjectType];
            for (var i = 0; i < 4; i++)
                if (_changedItems[i] == null && playerDesc.SlotTypes[i] == item.SlotType)
                    return i;

            for (var i = 4; i < 12 || (plr.HasBackpack && i < plr.Inventory.Length); i++)
                if (_changedItems[i] == null)
                    return i;
        }
        else {
            for (var i = 0; i < 8; i++)
                if (_changedItems[i] == null)
                    return i;
        }

        return -1;
    }
}

public class InventoryItems {
    private readonly Item[] _items;
    private readonly SV<int>[] _itemTypes;

    public InventoryItems(IContainer container, Item[] items) {
        _itemTypes = new SV<int>[items.Length];
        _items = new Item[items.Length];

        for (var i = 0; i < items.Length; i++) {
            var sti = (int) StatsType.Inventory0 + i;
            if (i >= 12)
                sti = (int) StatsType.BackPack0 + i - 12;

            _itemTypes[i] = new SV<int>(
                container as Entity,
                (StatsType) sti, items[i]?.ObjectType ?? -1,
                container is Player && i > 3);
            _items[i] = items[i];
        }
    }

    public int Length => _items.Length;

    public Item this[int index] {
        get => _items[index];
        set {
            _itemTypes[index].SetValue(value?.ObjectType ?? -1);
            _items[index] = value;
        }
    }

    public void SetItems(Item[] items) {
        if (items.Length > Length)
            throw new InvalidOperationException("Item array must be <= the size of the initialized array.");

        for (var i = 0; i < items.Length; i++) {
            _itemTypes[i].SetValue(items[i]?.ObjectType ?? -1);
            _items[i] = items[i];
        }
    }

    public Item[] GetItems() {
        return (Item[]) _items.Clone();
    }
}

public class Inventory : IEnumerable<Item> {
    private static readonly object TrySaveLock = new();
    private readonly object _invLock = new();

    private readonly InventoryItems _items;

    public Inventory(IContainer parent)
        : this(parent, new Item[20]) { }

    public Inventory(IContainer parent, Item[] items) {
        Parent = parent;
        _items = new InventoryItems(parent, items);
    }

    public IContainer Parent { get; }

    public int Length => _items.Length;

    public Item this[int index] {
        get {
            using (TimedLock.Lock(_invLock)) {
                return _items[index];
            }
        }
        set {
            using (TimedLock.Lock(_invLock)) {
                if (_items[index] != value) {
                    var oItems = _items.GetItems();
                    _items[index] = value;
                    InventoryChanged?.Invoke(this, new InventoryChangedEventArgs(oItems, _items.GetItems()));
                }
            }
        }
    }

    public IEnumerator<Item> GetEnumerator() {
        return ((IEnumerable<Item>) _items.GetItems()).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return _items.GetItems().GetEnumerator();
    }

    public event EventHandler<InventoryChangedEventArgs> InventoryChanged;

    public void SetItems(Item[] items) {
        using (TimedLock.Lock(_invLock)) {
            var oItems = _items.GetItems();
            _items.SetItems(items);
            InventoryChanged?.Invoke(this, new InventoryChangedEventArgs(oItems, _items.GetItems()));
        }
    }

    public void SetItems(IEnumerable<ushort> items) {
        using (TimedLock.Lock(_invLock)) {
            var oItems = _items.GetItems();
            _items.SetItems(ConvertObjectType2ItemArray(items));
            InventoryChanged?.Invoke(this, new InventoryChangedEventArgs(oItems, _items.GetItems()));
        }
    }

    public Item[] GetItems() {
        using (TimedLock.Lock(TrySaveLock)) {
            using (TimedLock.Lock(_invLock)) {
                return _items.GetItems();
            }
        }
    }

    public ushort[] GetItemTypes() {
        using (TimedLock.Lock(_invLock)) {
            return _items.GetItems().Select(_ => _?.ObjectType ?? 0xffff).ToArray();
        }
    }

    public InventoryTransaction CreateTransaction() {
        return new InventoryTransaction(Parent);
    }

    public static bool Execute(params InventoryTransaction[] transactions) {
        using (TimedLock.Lock(TrySaveLock)) {
            if (transactions.Any(tranaction => !tranaction.Validate()))
                return false;

            foreach (var transcation in transactions)
                transcation.Execute();

            return true;
        }
    }

    public static bool Revert(params InventoryTransaction[] transactions) {
        using (TimedLock.Lock(TrySaveLock)) {
            if (transactions.Any(tranaction => !tranaction.Validate(true)))
                return false;

            foreach (var transcation in transactions)
                transcation.Revert();
            return true;
        }
    }

    public int GetAvailableInventorySlot(Item item) {
        using (TimedLock.Lock(_invLock)) {
            if (Parent is Player plr) {
                var playerDesc = plr.Manager.Resources.GameData
                    .Classes[plr.ObjectDesc.ObjectType];
                for (var i = 0; i < 4; i++)
                    if (_items[i] == null && playerDesc.SlotTypes[i] == item.SlotType)
                        return i;

                for (var i = 4; i < 12 || (plr.HasBackpack && i < plr.Inventory.Length); i++)
                    if (_items[i] == null)
                        return i;
            }
            else {
                for (var i = 0; i < 8; i++)
                    if (_items[i] == null)
                        return i;
            }

            return -1;
        }
    }

    private static Item[] ConvertObjectType2ItemArray(IEnumerable<ushort> a) {
        var gameData = Program.Resources.GameData;
        return a
            .Select(_ => _ == 0xffff || !gameData.Items.ContainsKey(_) ? null : gameData.Items[_])
            .ToArray();
    }
}