﻿using Shared.resources;
using GameServer.logic.loot;
using GameServer.realm.worlds;

namespace GameServer.realm.setpieces;

internal abstract class Temple : ISetPiece {
    protected static readonly string DarkGrass = "Dark Grass";
    protected static readonly string Floor = "Jungle Temple Floor";
    protected static readonly string WallA = "Jungle Temple Bricks";
    protected static readonly string WallB = "Jungle Temple Walls";
    protected static readonly string WallC = "Jungle Temple Column";
    protected static readonly string Flower = "Jungle Ground Flowers";
    protected static readonly string Grass = "Jungle Grass";
    protected static readonly string Tree = "Jungle Tree Big";

    protected static readonly Loot chest = new(
        new TierLoot(4, ItemType.Weapon, 0.3),
        new TierLoot(5, ItemType.Weapon, 0.2),
        new TierLoot(4, ItemType.Armor, 0.3),
        new TierLoot(5, ItemType.Armor, 0.2),
        new TierLoot(1, ItemType.Ability, 0.25),
        new TierLoot(2, ItemType.Ability, 0.15),
        new TierLoot(2, ItemType.Ring, 0.3),
        new TierLoot(3, ItemType.Ring, 0.2),
        new TierLoot(1, ItemType.Potion, 0.5),
        new TierLoot(1, ItemType.Potion, 0.5),
        new TierLoot(1, ItemType.Potion, 0.5)
    );

    public abstract int Size { get; }
    public abstract void RenderSetPiece(World world, IntPoint pos);

    protected static void Render(Temple temple, World world, IntPoint pos, int[,] ground, int[,] objs) {
        var dat = world.Manager.Resources.GameData;
        for (var x = 0; x < temple.Size; x++) //Rendering
        for (var y = 0; y < temple.Size; y++) {
            if (ground[x, y] == 1) {
                var tile = world.Map[x + pos.X, y + pos.Y];
                tile.TileId = dat.IdToTileType[DarkGrass];
                tile.ObjectType = 0;
                tile.UpdateCount++;
            }
            else if (ground[x, y] == 2) {
                var tile = world.Map[x + pos.X, y + pos.Y];
                tile.TileId = dat.IdToTileType[Floor];
                tile.ObjectType = 0;
                tile.UpdateCount++;
            }

            if (objs[x, y] == 1) {
                var tile = world.Map[x + pos.X, y + pos.Y];
                tile.ObjectType = dat.IdToObjectType[WallA];
                tile.ObjectDesc = dat.ObjectDescs[tile.ObjectType];
                if (tile.ObjectId == 0) tile.ObjectId = world.GetNextEntityId();
                tile.UpdateCount++;
            }
            else if (objs[x, y] == 2) {
                var tile = world.Map[x + pos.X, y + pos.Y];
                tile.ObjectType = dat.IdToObjectType[WallB];
                tile.ObjectDesc = dat.ObjectDescs[tile.ObjectType];
                if (tile.ObjectId == 0) tile.ObjectId = world.GetNextEntityId();
                tile.UpdateCount++;
            }
            else if (objs[x, y] == 3) {
                var tile = world.Map[x + pos.X, y + pos.Y];
                tile.ObjectType = dat.IdToObjectType[WallC];
                tile.ObjectDesc = dat.ObjectDescs[tile.ObjectType];
                if (tile.ObjectId == 0) tile.ObjectId = world.GetNextEntityId();
                tile.UpdateCount++;
            }
            else if (objs[x, y] == 4) {
                var tile = world.Map[x + pos.X, y + pos.Y];
                tile.ObjectType = dat.IdToObjectType[Flower];
                tile.ObjectDesc = dat.ObjectDescs[tile.ObjectType];
                if (tile.ObjectId == 0) tile.ObjectId = world.GetNextEntityId();
                tile.UpdateCount++;
            }
            else if (objs[x, y] == 5) {
                var tile = world.Map[x + pos.X, y + pos.Y];
                tile.ObjectType = dat.IdToObjectType[Grass];
                tile.ObjectDesc = dat.ObjectDescs[tile.ObjectType];
                if (tile.ObjectId == 0) tile.ObjectId = world.GetNextEntityId();
                tile.UpdateCount++;
            }
            else if (objs[x, y] == 6) {
                var tile = world.Map[x + pos.X, y + pos.Y];
                tile.ObjectType = dat.IdToObjectType[Tree];
                tile.ObjectDesc = dat.ObjectDescs[tile.ObjectType];
                if (tile.ObjectId == 0) tile.ObjectId = world.GetNextEntityId();
                tile.UpdateCount++;
            }
        }
    }
}