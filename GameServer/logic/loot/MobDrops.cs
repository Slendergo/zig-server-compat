using System.Xml.Linq;
using Shared;
using Shared.resources;
using GameServer.realm;
using NLog;

namespace GameServer.logic.loot;

public abstract class MobDrops {
    protected static XmlData XmlData;
    public readonly IList<LootDef> LootDefs = new List<LootDef>();

    public static void Init(RealmManager manager) {
        if (XmlData != null)
            throw new Exception("MobDrops already initialized");

        XmlData = manager.Resources.GameData;
    }

    public virtual void Populate(IList<LootDef> lootDefs, LootDef overrides = null) {
        if (overrides == null) {
            foreach (var lootDef in LootDefs)
                lootDefs.Add(lootDef);
            return;
        }

        foreach (var lootDef in LootDefs)
            lootDefs.Add(new LootDef(
                lootDef.Item,
                overrides.Probabilty >= 0 ? overrides.Probabilty : lootDef.Probabilty,
                overrides.NumRequired >= 0 ? overrides.NumRequired : lootDef.NumRequired,
                overrides.Threshold >= 0 ? overrides.Threshold : lootDef.Threshold));
    }
}

public class ItemLoot : MobDrops {
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public ItemLoot(XElement e) {
        var itemId = e.ParseString("@item");
        var prob = e.ParseFloat("@probability");
        var numReq = e.ParseInt("@numRequired");
        var threshold = e.ParseFloat("@threshold");
        if (XmlData.IdToObjectType.TryGetValue(itemId, out var objType) && XmlData.Items.TryGetValue(objType, out Item item)) {
            LootDefs.Add(new LootDef(item, prob, numReq, threshold));
        }
        else {
            SLog.Error($"Error when adding \"{itemId}\" to loot definitions. Item not found.");
        }
    }
}

public class TierLoot : MobDrops {
    private static readonly int[] WeaponT = {1, 2, 3, 8, 17, 24};
    private static readonly int[] AbilityT = {4, 5, 11, 12, 13, 15, 16, 18, 19, 20, 21, 22, 23, 25};
    private static readonly int[] ArmorT = {6, 7, 14};
    private static readonly int[] RingT = {9};
    private static readonly int[] PotionT = {10};

    public TierLoot(XElement e) {
        var tier = e.ParseInt("@tier");
        var type = (ItemType) e.ParseInt("@type");
        var prob = e.ParseFloat("@probability");
        var numReq = e.ParseInt("@numRequired");
        var threshold = e.ParseFloat("@threshold");
        var types = type switch
        {
            ItemType.Weapon => WeaponT,
            ItemType.Ability => AbilityT,
            ItemType.Armor => ArmorT,
            ItemType.Ring => RingT,
            ItemType.Potion => PotionT,
            _ => throw new NotSupportedException(type.ToString())
        };
        var items = XmlData.Items
            .Where(item => Array.IndexOf(types, item.Value.SlotType) != -1)
            .Where(item => item.Value.Tier == tier)
            .Select(item => item.Value)
            .ToArray();
        foreach (var item in items)
            LootDefs.Add(new LootDef(item, prob / items.Length, numReq, threshold));
    }
    
    public TierLoot(byte tier, ItemType type, double probability = 1, int numRequired = 0, double threshold = 0) {
        var types = type switch
        {
            ItemType.Weapon => WeaponT,
            ItemType.Ability => AbilityT,
            ItemType.Armor => ArmorT,
            ItemType.Ring => RingT,
            ItemType.Potion => PotionT,
            _ => throw new NotSupportedException(type.ToString())
        };
        var items = XmlData.Items
            .Where(item => Array.IndexOf(types, item.Value.SlotType) != -1)
            .Where(item => item.Value.Tier == tier)
            .Select(item => item.Value)
            .ToArray();
        foreach (var item in items)
            LootDefs.Add(new LootDef(item, probability / items.Length, numRequired, threshold));
    }
}

public class Threshold : MobDrops {
    public Threshold(XElement e) {
        var threshold = e.ParseFloat("@threshold");
        var children = e.Elements().Select(i => i.Name.LocalName switch {
            "ItemLoot" => new ItemLoot(i),
            "TierLoot" => (MobDrops) new TierLoot(i),
            _ => throw new NotSupportedException(i.Name.LocalName)
        });
        foreach (var i in children)
            i.Populate(LootDefs, new LootDef(null, -1, -1, threshold));
    }
    
    public Threshold(double threshold, params MobDrops[] children) {
        foreach (var i in children)
            i.Populate(LootDefs, new LootDef(null, -1, -1, threshold));
    }
}