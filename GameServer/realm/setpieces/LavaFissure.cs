﻿using Shared.resources;
using GameServer.logic.loot;
using GameServer.realm.entities;
using GameServer.realm.worlds;

namespace GameServer.realm.setpieces;

internal class LavaFissure : ISetPiece {
    private static readonly string Lava = "Lava Blend";
    private static readonly string Floor = "Partial Red Floor";

    private static readonly Loot chest = new(
        new TierLoot(7, ItemType.Weapon, 0.3),
        new TierLoot(8, ItemType.Weapon, 0.2),
        new TierLoot(9, ItemType.Weapon, 0.1),
        new TierLoot(6, ItemType.Armor, 0.3),
        new TierLoot(7, ItemType.Armor, 0.2),
        new TierLoot(8, ItemType.Armor, 0.1),
        new TierLoot(2, ItemType.Ability, 0.3),
        new TierLoot(3, ItemType.Ability, 0.2),
        new TierLoot(4, ItemType.Ability, 0.1),
        new TierLoot(2, ItemType.Ring, 0.25),
        new TierLoot(3, ItemType.Ring, 0.15),
        new TierLoot(1, ItemType.Potion, 0.5)
    );

    private Random rand = new();

    public int Size => 40;

    public void RenderSetPiece(World world, IntPoint pos) {
        var p = new int[Size, Size];
        const double SCALE = 5.5;
        for (var x = 0; x < Size; x++) //Lava
        {
            var t = (double) x / Size * Math.PI;
            var x_ = t / Math.Sqrt(2) - Math.Sin(t) / (SCALE * Math.Sqrt(2));
            var y1 = t / Math.Sqrt(2) - 2 * Math.Sin(t) / (SCALE * Math.Sqrt(2));
            var y2 = t / Math.Sqrt(2) + Math.Sin(t) / (SCALE * Math.Sqrt(2));
            y1 /= Math.PI / Math.Sqrt(2);
            y2 /= Math.PI / Math.Sqrt(2);

            var y1_ = (int) Math.Ceiling(y1 * Size);
            var y2_ = (int) Math.Floor(y2 * Size);
            for (var i = y1_; i < y2_; i++)
                p[x, i] = 1;
        }

        for (var x = 0; x < Size; x++) //Floor
        for (var y = 0; y < Size; y++)
            if (p[x, y] == 1 && rand.Next() % 5 == 0)
                p[x, y] = 2;

        var r = rand.Next(0, 4); //Rotation
        for (var i = 0; i < r; i++)
            p = SetPieces.rotateCW(p);
        p[20, 20] = 2;

        var dat = world.Manager.Resources.GameData;
        for (var x = 0; x < Size; x++) //Rendering
        for (var y = 0; y < Size; y++)
            if (p[x, y] == 1) {
                var tile = world.Map[x + pos.X, y + pos.Y];
                tile.TileId = dat.IdToTileType[Lava];
                tile.ObjectType = 0;
                tile.UpdateCount++;
            }
            else if (p[x, y] == 2) {
                var tile = world.Map[x + pos.X, y + pos.Y];
                tile.TileId = dat.IdToTileType[Lava];
                tile.ObjectType = dat.IdToObjectType[Floor];
                tile.ObjectDesc = dat.ObjectDescs[tile.ObjectType];
                if (tile.ObjectId == 0) tile.ObjectId = world.GetNextEntityId();
                tile.UpdateCount++;
            }


        var demon = Entity.Resolve(world.Manager, "Red Demon");
        demon.Move(pos.X + 20.5f, pos.Y + 20.5f);
        world.EnterWorld(demon);

        var container = new Container(world.Manager, 0x0501, null, false);
        var items = chest.GetLoots(world.Manager, 5, 8).ToArray();
        for (var i = 0; i < items.Length; i++)
            container.Inventory[i] = items[i];
        container.Move(pos.X + 20.5f, pos.Y + 20.5f);
        world.EnterWorld(container);
    }
}