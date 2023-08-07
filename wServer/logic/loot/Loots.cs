﻿using System;
using System.Collections.Generic;
using System.Linq;
using common.resources;
using wServer.realm.entities;
using wServer.realm;
using wServer.realm.worlds.logic;

namespace wServer.logic.loot
{
    public class LootDef
    {
        public readonly Item Item;
        public readonly double Probabilty;
        public readonly int NumRequired;
        public readonly double Threshold;

        public LootDef(Item item, double probabilty, int numRequired, double threshold)
        {
            Item = item;
            Probabilty = probabilty;
            NumRequired = numRequired;
            Threshold = threshold;
        }
    }

    public class Loot : List<MobDrops>
    {
        static readonly Random Rand = new Random();

        static readonly ushort BROWN_BAG = 0x0500;
        static readonly ushort PURPLE_BAG = 0x0506;
        static readonly ushort CYAN_BAG = 0x0507;
        static readonly ushort POTION_BAG = 0x0508;
        static readonly ushort WHITE_BAG = 0x0509;

        public Loot(params MobDrops[] lootDefs)   //For independent loots(e.g. chests)
        {
            AddRange(lootDefs);
        }

        public IEnumerable<Item> GetLoots(RealmManager manager, int min, int max)   //For independent loots(e.g. chests)
        {
            var consideration = new List<LootDef>();
            foreach (var i in this)
            {
                i.Populate(consideration);
            }

            var retCount = Rand.Next(min, max);
            foreach (var i in consideration)
            {
                if (Rand.NextDouble() < i.Probabilty)
                {
                    yield return i.Item;
                    retCount--;
                }
                if (retCount == 0)
                {
                    yield break;
                }
            }
        }

        private List<LootDef> GetPossibleDrops()
        {
            var possibleDrops = new List<LootDef>();
            foreach (var i in this)
            {
                i.Populate(possibleDrops);
            }
            return possibleDrops;
        }

        public void Handle(Enemy enemy, RealmTime time)
        {
            // enemies that shouldn't drop loot
            if (enemy.Spawned)
            {
                return;
            }

            // generate a list of all possible loot drops
            var possibleDrops = GetPossibleDrops();
            var reqDrops = possibleDrops.ToDictionary(drop => drop, drop => drop.NumRequired);

            // generate public loot
            var publicLoot = new List<Item>();
            foreach (var i in possibleDrops)
            {
                if (i.Threshold <= 0 && Rand.NextDouble() < i.Probabilty)
                {
                    publicLoot.Add(i.Item);
                    reqDrops[i]--;
                }
            }

            // generate individual player loot
            var eligiblePlayers = enemy.DamageCounter.GetPlayerData();
            var privateLoot = new Dictionary<Player, IList<Item>>();
            foreach (var player in eligiblePlayers)
            {
                var loot = new List<Item>();
                foreach (var i in possibleDrops)
                {
                    if (i.Threshold > 0 && i.Threshold <= player.Item2 &&
                        Rand.NextDouble() < i.Probabilty)
                    {
                        loot.Add(i.Item);
                        reqDrops[i]--;
                    }
                }

                privateLoot[player.Item1] = loot;
            }

            // add required drops that didn't drop already
            foreach (var i in possibleDrops)
            {
                if (i.Threshold <= 0)
                {
                    // add public required loot
                    while (reqDrops[i] > 0)
                    {
                        publicLoot.Add(i.Item);
                        reqDrops[i]--;
                    }
                    continue;
                }

                // add private required loot
                var ePlayers = eligiblePlayers.Where(p => i.Threshold <= p.Item2).ToList();
                if (ePlayers.Count() <= 0)
                {
                    continue;
                }

                while (reqDrops[i] > 0 && ePlayers.Count() > 0)
                {
                    // make sure a player doesn't recieve more than one required loot
                    var reciever = ePlayers.RandomElement(Rand);
                    ePlayers.Remove(reciever);

                    // don't assign item if player already recieved one with random chance
                    if (privateLoot[reciever.Item1].Contains(i.Item))
                    {
                        continue;
                    }

                    privateLoot[reciever.Item1].Add(i.Item);
                    reqDrops[i]--;
                }
            }

            AddBagsToWorld(enemy, publicLoot, privateLoot);
        }

        private void AddBagsToWorld(Enemy enemy, IList<Item> shared, IDictionary<Player, IList<Item>> playerLoot)
        {
            foreach (var i in playerLoot)
            {
                if (i.Value.Count > 0)
                {
                    ShowBags(enemy, i.Value, i.Key);
                }
            }
            ShowBags(enemy, shared);
        }

        private static void ShowBags(Enemy enemy, IEnumerable<Item> loots, params Player[] owners)
        {
            var ownerIds = owners.Select(x => x.AccountId).ToArray();
            var items = new Item[8];
            var idx = 0;

            var bagType = 0;

            foreach (var i in loots)
            {
                if (i.BagType > bagType)
                {
                    bagType = i.BagType;
                }

                items[idx] = i;
                idx++;

                if (idx != 8)
                {
                    continue;
                }

                ShowBag(enemy, ownerIds, bagType, items);

                bagType = 0;
                items = new Item[8];
                idx = 0;
            }

            if (idx > 0)
            {
                ShowBag(enemy, ownerIds, bagType, items);
            }
        }

        private static void ShowBag(Enemy enemy, int[] owners, int bagType, Item[] items)
        {
            ushort bag = BROWN_BAG;
            switch (bagType)
            {
                case 0: bag = BROWN_BAG; break;
                case 1: bag = PURPLE_BAG; break;
                case 2: bag = CYAN_BAG; break;
                case 3: bag = POTION_BAG; break;
                case 4: bag = WHITE_BAG; break;
            }

            var container = new Container(enemy.Manager, bag, 1000 * 60, true);
            for (int j = 0; j < 8; j++)
            {
                container.Inventory[j] = items[j];
            }
            container.BagOwners = owners;
            container.Move(
                enemy.X + (float)((Rand.NextDouble() * 2 - 1) * 0.5),
                enemy.Y + (float)((Rand.NextDouble() * 2 - 1) * 0.5));
            container.SetDefaultSize(80);
            enemy.Owner.EnterWorld(container);
        }
    }
}
