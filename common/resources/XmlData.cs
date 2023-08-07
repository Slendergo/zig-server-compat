using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace common.resources
{
    public class XmlData
    {
        static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public Dictionary<ushort, XElement> ObjectTypeToElement = new Dictionary<ushort, XElement>();
        public Dictionary<ushort, string> ObjectTypeToId = new Dictionary<ushort, string>();
        public Dictionary<string, ushort> IdToObjectType = new Dictionary<string, ushort>();
        public Dictionary<string, ushort> DisplayIdToObjectType = new Dictionary<string, ushort>();

        public Dictionary<ushort, XElement> TileTypeToElement = new Dictionary<ushort, XElement>();
        public Dictionary<ushort, string> TileTypeToId = new Dictionary<ushort, string>();
        public Dictionary<string, ushort> IdToTileType = new Dictionary<string, ushort>();

        public Dictionary<int, ItemType> SlotTypeToItemType = new Dictionary<int, ItemType>();

        public Dictionary<ushort, TileDesc> Tiles = new Dictionary<ushort, TileDesc>();
        public Dictionary<ushort, Item> Items = new Dictionary<ushort, Item>();
        public Dictionary<ushort, ObjectDesc> ObjectDescs = new Dictionary<ushort, ObjectDesc>();
        public Dictionary<ushort, PlayerDesc> Classes = new Dictionary<ushort, PlayerDesc>();
        public Dictionary<ushort, PortalDesc> Portals = new Dictionary<ushort, PortalDesc>();
        public Dictionary<ushort, SkinDesc> Skins = new Dictionary<ushort, SkinDesc>();

        public XmlData(string dir)
        {
            Log.Info("Loading XmlData...");
            LoadXmls(dir);

            Log.Info("Loaded {0} Tiles...", Tiles.Count);
            Log.Info("Loaded {0} Items...", Items.Count);
            Log.Info("Loaded {0} Objects...", ObjectDescs.Count);
            Log.Info("Loaded {0} Players...", Classes.Count);
            Log.Info("Loaded {0} Portals...", Portals.Count);
            Log.Info("Loaded {0} Skins...", Skins.Count);
        }

        private void LoadXmls(string dir)
        {
            var xmls = Directory.EnumerateFiles(dir, "*xml", SearchOption.AllDirectories).ToArray();
            foreach (string k in xmls)
            {
                var xml = Utils.Read(k);
                ProcessXml(XElement.Parse(xml));
            }
        }

        private void AddObjects(XElement root)
        {
            foreach (var e in root.Elements("Object"))
            {
                var cls = e.GetValue<string>("Class");
                if (string.IsNullOrWhiteSpace(cls))
                    continue;

                var id = e.GetAttribute<string>("id");
                var type = e.GetAttribute<ushort>("type");
                var displayId = e.GetValue<string>("DisplayId");
                var displayName = string.IsNullOrWhiteSpace(displayId) ? id : displayId;

                if (ObjectTypeToId.ContainsKey(type))
                    Log.Warn("'{0}' and '{1}' have the same type of '0x{2:x4}'", id, ObjectTypeToId[type], type);

                if (IdToObjectType.ContainsKey(id))
                    Log.Warn("'0x{0:x4}' and '0x{1:x4}' have the same id of '{2}'", type, IdToObjectType[id], id);

                ObjectTypeToId[type] = id;
                ObjectTypeToElement[type] = e;
                IdToObjectType[id] = type;
                DisplayIdToObjectType[displayName] = type;

                switch (cls)
                {
                    case "Equipment":
                    case "Dye":
                        Items[type] = new Item(type, e);
                        break;
                    case "Player":
                        var pDesc = Classes[type] = new PlayerDesc(type, e);
                        ObjectDescs[type] = Classes[type];
                        SlotTypeToItemType[pDesc.SlotTypes[0]] = ItemType.Weapon;
                        SlotTypeToItemType[pDesc.SlotTypes[1]] = ItemType.Ability;
                        SlotTypeToItemType[pDesc.SlotTypes[2]] = ItemType.Armor;
                        SlotTypeToItemType[pDesc.SlotTypes[3]] = ItemType.Ring;
                        break;
                    case "GuildHallPortal":
                    case "Portal":
                        Portals[type] = new PortalDesc(type, e);
                        ObjectDescs[type] = Portals[type];
                        break;
                    case "Skin":
                        Skins[type] = new SkinDesc(type, e);
                        break;
                    default:
                        ObjectDescs[type] = new ObjectDesc(type, e);
                        break;
                }
            }
        }

        private void AddGrounds(XElement root)
        {
            foreach (var e in root.Elements("Ground"))
            {
                var id = e.GetAttribute<string>("id");
                var type = e.GetAttribute<ushort>("type");

                if (TileTypeToId.ContainsKey(type))
                    Log.Warn("'{0}' and '{1}' have the same type of '0x{2:x4}'", id, TileTypeToId[type], type);

                if (IdToTileType.ContainsKey(id))
                    Log.Warn("'0x{0:x4}' and '0x{1:x4}' have the same id of '{2}'", type, IdToTileType[id], id);

                TileTypeToId[type] = id;
                TileTypeToElement[type] = e;
                IdToTileType[id] = type;

                Tiles[type] = new TileDesc(type, e);
            }
        }

        private void ProcessXml(XElement root)
        {
            AddObjects(root);
            AddGrounds(root);
        }
    }
}
