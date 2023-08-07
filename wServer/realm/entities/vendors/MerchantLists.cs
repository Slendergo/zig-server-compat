using System;
using System.Collections.Generic;
using System.Linq;
using common;
using common.resources;
using NLog;
using wServer.realm.terrain;

namespace wServer.realm.entities.vendors
{
    public class ShopItem : ISellableItem
    {
        public ushort ItemId { get; private set; }
        public int Price { get; }
        public int Count { get; }
        public string Name { get; }

        public ShopItem(string name, ushort price, int count = -1)
        {
            ItemId = ushort.MaxValue;
            Price = price;
            Count = count;
            Name = name;
        }

        public void SetItem(ushort item)
        {
            if (ItemId != ushort.MaxValue)
                throw new AccessViolationException("Can't change item after it has been set.");

            ItemId = item;
        }
    }

    internal static class MerchantLists
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static readonly List<ISellableItem> Weapons = new List<ISellableItem>
        {
            new ShopItem("Dagger of Foul Malevolence", 500),
            new ShopItem("Bow of Covert Havens", 500),
            new ShopItem("Staff of the Cosmic Whole", 500),
            new ShopItem("Wand of Recompense", 500),
            new ShopItem("Sword of Acclaim", 500),
            new ShopItem("Masamune", 500)
        };

        private static readonly List<ISellableItem> Abilities = new List<ISellableItem>
        {
            new ShopItem("Cloak of Ghostly Concealment", 500),
            new ShopItem("Quiver of Elvish Mastery", 500),
            new ShopItem("Elemental Detonation Spell", 500),
            new ShopItem("Tome of Holy Guidance", 500),
            new ShopItem("Helm of the Great General", 500),
            new ShopItem("Colossus Shield", 500),
            new ShopItem("Seal of the Blessed Champion", 500),
            new ShopItem("Baneserpent Poison", 500),
            new ShopItem("Bloodsucker Skull", 500),
            new ShopItem("Giantcatcher Trap", 500),
            new ShopItem("Planefetter Orb", 500),
            new ShopItem("Prism of Apparitions", 500),
            new ShopItem("Scepter of Storms", 500),
            new ShopItem("Doom Circle", 500)
        };

        private static readonly List<ISellableItem> Armor = new List<ISellableItem>
        {
            new ShopItem("Robe of the Illusionist", 50),
            new ShopItem("Robe of the Grand Sorcerer", 500),
            new ShopItem("Studded Leather Armor", 50),
            new ShopItem("Hydra Skin Armor", 500),
            new ShopItem("Mithril Armor", 50),
            new ShopItem("Acropolis Armor", 500)
        };

        private static readonly List<ISellableItem> Rings = new List<ISellableItem>
        {
            new ShopItem("Ring of Paramount Attack", 100),
            new ShopItem("Ring of Paramount Defense", 100),
            new ShopItem("Ring of Paramount Speed", 100),
            new ShopItem("Ring of Paramount Dexterity", 100),
            new ShopItem("Ring of Paramount Vitality", 100),
            new ShopItem("Ring of Paramount Wisdom", 100),
            new ShopItem("Ring of Paramount Health", 100),
            new ShopItem("Ring of Paramount Magic", 100),
            new ShopItem("Ring of Unbound Attack", 750),
            new ShopItem("Ring of Unbound Defense", 750),
            new ShopItem("Ring of Unbound Speed", 750),
            new ShopItem("Ring of Unbound Dexterity", 750),
            new ShopItem("Ring of Unbound Vitality", 750),
            new ShopItem("Ring of Unbound Wisdom", 750),
            new ShopItem("Ring of Unbound Health", 750),
            new ShopItem("Ring of Unbound Magic", 750)
        };

        private static readonly List<ISellableItem> Keys = new List<ISellableItem>
        {
        };

        private static readonly List<ISellableItem> Store1 = new List<ISellableItem>
        {
            new ShopItem("Pirate Cave Key", 25),
            new ShopItem("Spider Den Key", 25),
            new ShopItem("Undead Lair Key", 50),
            new ShopItem("Sprite World Key", 50),
            new ShopItem("Abyss of Demons Key", 50),
            new ShopItem("Snake Pit Key", 50),
            new ShopItem("Beachzone Key", 50),
            new ShopItem("Lab Key", 50),
            new ShopItem("Totem Key", 50),
            new ShopItem("Manor Key", 80),
            new ShopItem("Candy Key", 100),
            new ShopItem("Cemetery Key", 150),
            new ShopItem("Davy's Key", 200),
            new ShopItem("Ocean Trench Key", 300),
            new ShopItem("Tomb of the Ancients Key", 400)
        };

        private static readonly List<ISellableItem> Store2 = new List<ISellableItem>
        {
            new ShopItem("Amulet of Resurrection", 11250),
            new ShopItem("Backpack", 2000),
            new ShopItem("Elixir of Health 7", 500),
            new ShopItem("Elixir of Magic 7", 500),
            new ShopItem("Transformation Potion", 500),
        };

        private static readonly List<ISellableItem> Store3 = new List<ISellableItem>
        {
            new ShopItem("Black Cat Generator", 1750),
            new ShopItem("Grey Cat Generator", 1200),
            new ShopItem("Orange Cat Generator", 1200),
            new ShopItem("Tan Cat Generator", 1200),
            new ShopItem("Yellow Cat Generator", 1200),
            new ShopItem("Brown Pup Generator", 1200),
            new ShopItem("Golden Pup Generator", 2500),
            new ShopItem("White Cat Generator", 1200),
            new ShopItem("Blue Snail Generator", 800),
            new ShopItem("Grey Wolf Generator", 1500),
            new ShopItem("Brown Fox Generator", 1500),
            new ShopItem("Green Frog Generator", 800),
            new ShopItem("Lion Generator", 1750),
            new ShopItem("Penguin Generator", 2500),
            new ShopItem("Sheepdog Generator", 1750),
            new ShopItem("Panda Generator", 7500),
        };

        private static readonly List<ISellableItem> Store4 = new List<ISellableItem>
        {
            new ShopItem("Tincture of Fear", 100),
            new ShopItem("Tincture of Courage", 150),
            new ShopItem("Tincture of Dexterity", 100),
            new ShopItem("Tincture of Defense", 100),
            new ShopItem("Tincture of Life", 150),
            new ShopItem("Tincture of Mana", 150),
            new ShopItem("Effusion of Dexterity", 250),
            new ShopItem("Effusion of Life", 250),
            new ShopItem("Effusion of Mana", 250),
            new ShopItem("Effusion of Defense", 250),
        };

        public static readonly Dictionary<TileRegion, Tuple<List<ISellableItem>, CurrencyType, /*Rank Req*/int>> Shops =
            new Dictionary<TileRegion, Tuple<List<ISellableItem>, CurrencyType, int>>()
        {
            { TileRegion.Store_1, new Tuple<List<ISellableItem>, CurrencyType, int>(Store1, CurrencyType.Gold, 0) },
            { TileRegion.Store_2, new Tuple<List<ISellableItem>, CurrencyType, int>(Store2, CurrencyType.Fame, 0) },
            { TileRegion.Store_3, new Tuple<List<ISellableItem>, CurrencyType, int>(Store3, CurrencyType.Gold, 0) },
            { TileRegion.Store_4, new Tuple<List<ISellableItem>, CurrencyType, int>(Store4, CurrencyType.Fame, 0) },
        };

        public static void Init(RealmManager manager)
        {
            InitDyes(manager);
            foreach (var shop in Shops)
                foreach (var shopItem in shop.Value.Item1.OfType<ShopItem>())
                {
                    ushort id;
                    if (!manager.Resources.GameData.IdToObjectType.TryGetValue(shopItem.Name, out id))
                        Log.Warn("Item name: {0}, not found.", shopItem.Name);
                    else
                        shopItem.SetItem(id);
                }
        }

        static void InitDyes(RealmManager manager)
        {
            var d1 = new List<ISellableItem>();
            var d2 = new List<ISellableItem>();
            foreach (var i in manager.Resources.GameData.Items.Values)
            {
                if (!i.Class.Equals("Dye"))
                    continue;

                if (i.Texture1 != 0)
                {
                    ushort price = 60;
                    if (i.ObjectId.Contains("Cloth") && i.ObjectId.Contains("Large"))
                        price *= 2;
                    d1.Add(new ShopItem(i.ObjectId, price));
                    continue;
                }

                if (i.Texture2 != 0)
                {
                    ushort price = 60;
                    if (i.ObjectId.Contains("Cloth") && i.ObjectId.Contains("Small"))
                        price *= 2;
                    d2.Add(new ShopItem(i.ObjectId, price));
                    continue;
                }
            }
            Shops[TileRegion.Store_5] = new Tuple<List<ISellableItem>, CurrencyType, int>(d1, CurrencyType.Gold, 0);
            Shops[TileRegion.Store_6] = new Tuple<List<ISellableItem>, CurrencyType, int>(d2, CurrencyType.Gold, 0);
        }
    }
}