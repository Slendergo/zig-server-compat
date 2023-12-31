﻿using System.Xml.Linq;
using NLog;

namespace Shared.resources;

public class XmlData {
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public Dictionary<ushort, PlayerDesc> Classes = new();
    public Dictionary<string, ushort> DisplayIdToObjectType = new(StringComparer.InvariantCultureIgnoreCase);
    public Dictionary<string, ushort> IdToObjectType = new(StringComparer.InvariantCultureIgnoreCase);
    public Dictionary<string, ushort> IdToTileType = new(StringComparer.InvariantCulture);
    public Dictionary<ushort, Item> Items = new();
    public Dictionary<ushort, ObjectDesc> ObjectDescs = new();

    public Dictionary<ushort, XElement> ObjectTypeToElement = new();
    public Dictionary<ushort, string> ObjectTypeToId = new();
    public Dictionary<ushort, PortalDesc> Portals = new();
    public Dictionary<ushort, SkinDesc> Skins = new();

    public Dictionary<int, ItemType> SlotTypeToItemType = new();

    public Dictionary<ushort, TileDesc> Tiles = new();

    public Dictionary<ushort, XElement> TileTypeToElement = new();
    public Dictionary<ushort, string> TileTypeToId = new();
    public Dictionary<string, WorldTemplateData> WorldTemplates = new(StringComparer.InvariantCulture);

    public XmlData(string dir) {
        SLog.Info("Loading XmlData...");
        LoadXmls(dir);

        SLog.Info("Loaded {0} Tiles...", Tiles.Count);
        SLog.Info("Loaded {0} Items...", Items.Count);
        SLog.Info("Loaded {0} Objects...", ObjectDescs.Count);
        SLog.Info("Loaded {0} Players...", Classes.Count);
        SLog.Info("Loaded {0} Portals...", Portals.Count);
        SLog.Info("Loaded {0} Skins...", Skins.Count);
        SLog.Info("Loaded {0} WorldTemplates...", WorldTemplates.Count);
    }

    private void LoadXmls(string dir) {
        var xmls = Directory.EnumerateFiles(dir, "*xml", SearchOption.AllDirectories).ToArray();
        foreach (var k in xmls) {
            var xml = Utils.Read(k);
            ProcessXml(XElement.Parse(xml));
        }
    }

    private void AddObjects(XElement root) {
        foreach (var e in root.Elements("Object")) {
            var cls = e.GetValue<string>("Class");
            if (string.IsNullOrWhiteSpace(cls))
                continue;

            var id = e.GetAttribute<string>("id");
            var type = e.GetAttribute<ushort>("type");
            var displayId = e.GetValue<string>("DisplayId");
            var displayName = string.IsNullOrWhiteSpace(displayId) ? id : displayId;

            if (ObjectTypeToId.ContainsKey(type))
                SLog.Warn("'{0}' and '{1}' have the same type of '0x{2:x4}'", id, ObjectTypeToId[type], type);

            if (IdToObjectType.ContainsKey(id))
                SLog.Warn("'0x{0:x4}' and '0x{1:x4}' have the same id of '{2}'", type, IdToObjectType[id], id);

            ObjectTypeToId[type] = id;
            ObjectTypeToElement[type] = e;
            IdToObjectType[id] = type;
            DisplayIdToObjectType[displayName] = type;

            switch (cls) {
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

    private void AddGrounds(XElement root) {
        foreach (var e in root.Elements("Ground")) {
            var id = e.GetAttribute<string>("id");
            var type = e.GetAttribute<ushort>("type");

            if (TileTypeToId.ContainsKey(type))
                SLog.Warn("'{0}' and '{1}' have the same type of '0x{2:x4}'", id, TileTypeToId[type], type);

            if (IdToTileType.ContainsKey(id))
                SLog.Warn("'0x{0:x4}' and '0x{1:x4}' have the same id of '{2}'", type, IdToTileType[id], id);

            TileTypeToId[type] = id;
            TileTypeToElement[type] = e;
            IdToTileType[id] = type;

            Tiles[type] = new TileDesc(type, e);
        }
    }

    private void AddWorlds(XElement root) {
        foreach (var e in root.Elements("World")) {
            var template = new WorldTemplateData(e);
            WorldTemplates[template.IdName] = template;
        }
    }

    private void ProcessXml(XElement root) {
        AddObjects(root);
        AddGrounds(root);
        AddWorlds(root);
    }
}