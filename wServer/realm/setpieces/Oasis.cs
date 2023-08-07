﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using common.resources;
using wServer.logic.loot;
using wServer.realm.entities;
using wServer.realm.worlds;

namespace wServer.realm.setpieces
{
    class Oasis : ISetPiece
    {
        public int Size
        {
            get { return 30; }
        }

        static readonly string Floor = "Light Grass";
        static readonly string Water = "Shallow Water";
        static readonly string Tree = "Palm Tree";

        static readonly Loot chest = new Loot(
                new TierLoot(5, ItemType.Weapon, 0.3),
                new TierLoot(6, ItemType.Weapon, 0.2),
                new TierLoot(7, ItemType.Weapon, 0.1),

                new TierLoot(4, ItemType.Armor, 0.3),
                new TierLoot(5, ItemType.Armor, 0.2),
                new TierLoot(6, ItemType.Armor, 0.1),

                new TierLoot(2, ItemType.Ability, 0.3),
                new TierLoot(3, ItemType.Ability, 0.2),

                new TierLoot(1, ItemType.Ring, 0.25),
                new TierLoot(2, ItemType.Ring, 0.15),

                new TierLoot(1, ItemType.Potion, 0.5)
            );

        Random rand = new Random();
        public void RenderSetPiece(World world, IntPoint pos)
        {
            int outerRadius = 13;
            int waterRadius = 10;
            int islandRadius = 3;
            List<IntPoint> border = new List<IntPoint>();

            int[,] t = new int[Size, Size];
            for (int y = 0; y < Size; y++)      //Outer
                for (int x = 0; x < Size; x++)
                {
                    double dx = x - (Size / 2.0);
                    double dy = y - (Size / 2.0);
                    double r = Math.Sqrt(dx * dx + dy * dy);
                    if (r <= outerRadius)
                        t[x, y] = 1;
                }

            for (int y = 0; y < Size; y++)      //Water
                for (int x = 0; x < Size; x++)
                {
                    double dx = x - (Size / 2.0);
                    double dy = y - (Size / 2.0);
                    double r = Math.Sqrt(dx * dx + dy * dy);
                    if (r <= waterRadius)
                    {
                        t[x, y] = 2;
                        if (waterRadius - r < 1)
                            border.Add(new IntPoint(x, y));
                    }
                }

            for (int y = 0; y < Size; y++)      //Island
                for (int x = 0; x < Size; x++)
                {
                    double dx = x - (Size / 2.0);
                    double dy = y - (Size / 2.0);
                    double r = Math.Sqrt(dx * dx + dy * dy);
                    if (r <= islandRadius)
                    {
                        t[x, y] = 1;
                        if (islandRadius - r < 1)
                            border.Add(new IntPoint(x, y));
                    }
                }

            HashSet<IntPoint> trees = new HashSet<IntPoint>();
            while (trees.Count < border.Count * 0.5)
                trees.Add(border[rand.Next(0, border.Count)]);

            foreach (var i in trees)
                t[i.X, i.Y] = 3;

            var dat = world.Manager.Resources.GameData;
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                {
                    if (t[x, y] == 1)
                    {
                        var tile = world.Map[x + pos.X, y + pos.Y];
                        tile.TileId = dat.IdToTileType[Floor];
                        tile.ObjType = 0;
                        tile.UpdateCount++;
                    }
                    else if (t[x, y] == 2)
                    {
                        var tile = world.Map[x + pos.X, y + pos.Y];
                        tile.TileId = dat.IdToTileType[Water];
                        tile.ObjType = 0;
                        tile.UpdateCount++;
                    }
                    else if (t[x, y] == 3)
                    {
                        var tile = world.Map[x + pos.X, y + pos.Y];
                        tile.TileId = dat.IdToTileType[Floor];
                        tile.ObjType = dat.IdToObjectType[Tree];
                        tile.ObjDesc = dat.ObjectDescs[tile.ObjType];
                        tile.ObjCfg = "size:" + (rand.Next() % 2 == 0 ? 120 : 140);
                        if (tile.ObjId == 0) tile.ObjId = world.GetNextEntityId();
                        tile.UpdateCount++;
                    }
                }

            Entity giant = Entity.Resolve(world.Manager, "Oasis Giant");
            giant.Move(pos.X + 15.5f, pos.Y + 15.5f);
            world.EnterWorld(giant);

            Container container = new Container(world.Manager, 0x0501, null, false);
            Item[] items = chest.GetLoots(world.Manager, 5, 8).ToArray();
            for (int i = 0; i < items.Length; i++)
                container.Inventory[i] = items[i];
            container.Move(pos.X + 15.5f, pos.Y + 15.5f);
            world.EnterWorld(container);
        }
    }
}
