using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using common.resources;
using StackExchange.Redis;
using wServer.networking.packets;
using wServer.networking.packets.outgoing;
using wServer.realm.worlds;
using wServer.realm.worlds.logic;

namespace wServer.realm.entities
{
    partial class Player
    {
        public const int MaxAbilityDist = 14;

        public static readonly ConditionEffect[] NegativeEffs = new ConditionEffect[]
        {
            new ConditionEffect()
            {
                Effect = ConditionEffectIndex.Slowed,
                DurationMS = 0
            },
            new ConditionEffect()
            {
                Effect = ConditionEffectIndex.Paralyzed,
                DurationMS = 0
            },
            new ConditionEffect()
            {
                Effect = ConditionEffectIndex.Weak,
                DurationMS = 0
            },
            new ConditionEffect()
            {
                Effect = ConditionEffectIndex.Stunned,
                DurationMS = 0
            },
            new ConditionEffect()
            {
                Effect = ConditionEffectIndex.Confused,
                DurationMS = 0
            },
            new ConditionEffect()
            {
                Effect = ConditionEffectIndex.Blind,
                DurationMS = 0
            },
            new ConditionEffect()
            {
                Effect = ConditionEffectIndex.Quiet,
                DurationMS = 0
            },
            new ConditionEffect()
            {
                Effect = ConditionEffectIndex.ArmorBroken,
                DurationMS = 0
            },
            new ConditionEffect()
            {
                Effect = ConditionEffectIndex.Bleeding,
                DurationMS = 0
            },
            new ConditionEffect()
            {
                Effect = ConditionEffectIndex.Dazed,
                DurationMS = 0
            },
            new ConditionEffect()
            {
                Effect = ConditionEffectIndex.Sick,
                DurationMS = 0
            },
            new ConditionEffect()
            {
                Effect = ConditionEffectIndex.Drunk,
                DurationMS = 0
            },
            new ConditionEffect()
            {
                Effect = ConditionEffectIndex.Hallucinating,
                DurationMS = 0
            },
            new ConditionEffect()
            {
                Effect = ConditionEffectIndex.Hexed,
                DurationMS = 0
            },
        };

        private readonly object _useLock = new object();
        public void UseItem(RealmTime time, int objId, int slot, Position pos)
        {
            using (TimedLock.Lock(_useLock))
            {
                //Log.Debug(objId + ":" + slot);
                var entity = Owner.GetEntity(objId);
                if (entity == null)
                {
                    Client.SendPacket(new InvResult() { Result = 1 });
                    return;
                }

                if (entity is Player && objId != Id)
                {
                    Client.SendPacket(new InvResult() { Result = 1 });
                    return;
                }

                var container = entity as IContainer;

                // eheh no more clearing BBQ loot bags
                if (this.Dist(entity) > 3)
                {
                    Client.SendPacket(new InvResult() { Result = 1 });
                    return;
                }

                var cInv = container?.Inventory.CreateTransaction();

                // get item
                Item item = null;
                foreach (var stack in Stacks.Where(stack => stack.Slot == slot))
                {
                    item = stack.Pull();

                    if (item == null)
                        return;

                    break;
                }
                if (item == null)
                {
                    if (container == null)
                        return;

                    item = cInv[slot];
                }

                if (item == null)
                    return;

                // make sure not trading and trying to cunsume item
                if (tradeTarget != null && item.Consumable)
                    return;

                if (MP < item.MpCost)
                {
                    Client.SendPacket(new InvResult() { Result = 1 });
                    return;
                }


                // use item
                var slotType = 10;
                if (slot < cInv.Length)
                {
                    slotType = container.SlotTypes[slot];

                    if (item.TypeOfConsumable)
                    {
                        var gameData = Manager.Resources.GameData;
                        var db = Manager.Database;

                        if (item.Consumable)
                        {
                            Item successor = null;
                            if (item.SuccessorId != null)
                                successor = gameData.Items[gameData.IdToObjectType[item.SuccessorId]];
                            cInv[slot] = successor;
                        }

                        if (!Inventory.Execute(cInv)) // can result in the loss of an item if inv trans fails...
                        {
                            entity.ForceUpdate(slot);
                            return;
                        }

                        if (slotType > 0)
                        {
                            FameCounter.UseAbility();
                        }
                        else
                        {
                            if (item.ActivateEffects.Any(eff => eff.Effect == ActivateEffects.Heal ||
                                                                eff.Effect == ActivateEffects.HealNova ||
                                                                eff.Effect == ActivateEffects.Magic ||
                                                                eff.Effect == ActivateEffects.MagicNova))
                            {
                                FameCounter.DrinkPot();
                            }
                        }

                        Activate(time, item, pos);
                        return;
                    }

                    if (slotType > 0)
                    {
                        FameCounter.UseAbility();
                    }
                }
                else
                {
                    FameCounter.DrinkPot();
                }

                //Log.Debug(item.SlotType + ":" + slotType);
                if (item.Consumable || item.SlotType == slotType)
                {
                    //Log.Debug("HUH");
                    Activate(time, item, pos);
                }
                else
                    Client.SendPacket(new InvResult() { Result = 1 });
            }
        }

        private void Activate(RealmTime time, Item item, Position target)
        {
            MP -= item.MpCost;
            foreach (var eff in item.ActivateEffects)
            {
                switch (eff.Effect)
                {
                    case ActivateEffects.Create:
                        AECreate(time, item, target, eff);
                        break;
                    case ActivateEffects.Dye:
                        AEDye(time, item, target, eff);
                        break;
                    case ActivateEffects.Shoot:
                        AEShoot(time, item, target, eff);
                        break;
                    case ActivateEffects.IncrementStat:
                        AEIncrementStat(time, item, target, eff);
                        break;
                    case ActivateEffects.Heal:
                        AEHeal(time, item, target, eff);
                        break;
                    case ActivateEffects.Magic:
                        AEMagic(time, item, target, eff);
                        break;
                    case ActivateEffects.HealNova:
                        AEHealNova(time, item, target, eff);
                        break;
                    case ActivateEffects.StatBoostSelf:
                        AEStatBoostSelf(time, item, target, eff);
                        break;
                    case ActivateEffects.StatBoostAura:
                        AEStatBoostAura(time, item, target, eff);
                        break;
                    case ActivateEffects.BulletNova:
                        AEBulletNova(time, item, target, eff);
                        break;
                    case ActivateEffects.ConditionEffectSelf:
                        AEConditionEffectSelf(time, item, target, eff);
                        break;
                    case ActivateEffects.ConditionEffectAura:
                        AEConditionEffectAura(time, item, target, eff);
                        break;
                    case ActivateEffects.Teleport:
                        AETeleport(time, item, target, eff);
                        break;
                    case ActivateEffects.PoisonGrenade:
                        AEPoisonGrenade(time, item, target, eff);
                        break;
                    case ActivateEffects.VampireBlast:
                        AEVampireBlast(time, item, target, eff);
                        break;
                    case ActivateEffects.Trap:
                        AETrap(time, item, target, eff);
                        break;
                    case ActivateEffects.StasisBlast:
                        StasisBlast(time, item, target, eff);
                        break;
                    case ActivateEffects.Pet:
                        break;
                    case ActivateEffects.Decoy:
                        AEDecoy(time, item, target, eff);
                        break;
                    case ActivateEffects.Lightning:
                        AELightning(time, item, target, eff);
                        break;
                    case ActivateEffects.UnlockPortal:
                        AEUnlockPortal(time, item, target, eff);
                        break;
                    case ActivateEffects.MagicNova:
                        AEMagicNova(time, item, target, eff);
                        break;
                    case ActivateEffects.ClearConditionEffectAura:
                        AEClearConditionEffectAura(time, item, target, eff);
                        break;
                    case ActivateEffects.RemoveNegativeConditions:
                        AERemoveNegativeConditions(time, item, target, eff);
                        break;
                    case ActivateEffects.ClearConditionEffectSelf:
                        AEClearConditionEffectSelf(time, item, target, eff);
                        break;
                    case ActivateEffects.RemoveNegativeConditionsSelf:
                        AERemoveNegativeConditionSelf(time, item, target, eff);
                        break;
                    case ActivateEffects.ShurikenAbility:
                        AEShurikenAbility(time, item, target, eff);
                        break;
                    case ActivateEffects.DazeBlast:
                        break;
                    case ActivateEffects.PermaPet:
                        AEPermaPet(time, item, target, eff);
                        break;
                    case ActivateEffects.Backpack:
                        AEBackpack(time, item, target, eff);
                        break;
                    default:
                        Log.Warn("Activate effect {0} not implemented.", eff.Effect);
                        break;
                }
            }
        }

        private void AEBackpack(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            HasBackpack = true;
        }

        private void AEPermaPet(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            var type = Manager.Resources.GameData.IdToObjectType[eff.ObjectId];
            var desc = Manager.Resources.GameData.ObjectDescs[type];
            //Log.Debug(desc.ObjectType);
            PetId = desc.ObjectType;
            SpawnPetIfAttached(Owner);
            //Log.Debug("hey!");
        }

        private void AEUnlockPortal(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            var gameData = Manager.Resources.GameData;

            // find locked portal
            var portals = Owner.StaticObjects.Values
                .Where(s => s is Portal && s.ObjectDesc.ObjectId.Equals(eff.LockedName) && s.DistSqr(this) <= 9)
                .Select(s => s as Portal);
            if (!portals.Any())
                return;
            var portal = portals.Aggregate(
                (curmin, x) => (curmin == null || x.DistSqr(this) < curmin.DistSqr(this) ? x : curmin));
            if (portal == null)
                return;

            // get proto of world
            ProtoWorld proto;
            if (!Manager.Resources.Worlds.Data.TryGetValue(eff.DungeonName, out proto))
            {
                Log.Error("Unable to unlock portal. \"" + eff.DungeonName + "\" does not exist.");
                return;
            }

            if (proto.portals == null || proto.portals.Length < 1)
            {
                Log.Error("World is not associated with any portals.");
                return;
            }

            // create portal of unlocked world
            var portalType = (ushort)proto.portals[0];
            var uPortal = Resolve(Manager, portalType) as Portal;
            if (uPortal == null)
            {
                Log.Error("Error creating portal: {0}", portalType);
                return;
            }

            var portalDesc = gameData.Portals[portal.ObjectType];
            var uPortalDesc = gameData.Portals[portalType];

            // create world
            World world;
            if (proto.id < 0)
                world = Manager.GetWorld(proto.id);
            else
            {
                DynamicWorld.TryGetWorld(proto, Client, out world);
                world = Manager.AddWorld(world ?? new World(proto));
            }
            uPortal.WorldInstance = world;

            // swap portals
            if (!portalDesc.NexusPortal || !Manager.Monitor.RemovePortal(portal))
                Owner.LeaveWorld(portal);
            uPortal.Move(portal.X, portal.Y);
            uPortal.Name = uPortalDesc.DisplayId;
            var uPortalPos = new Position() { X = portal.X - .5f, Y = portal.Y - .5f };
            if (!uPortalDesc.NexusPortal || !Manager.Monitor.AddPortal(world.Id, uPortal, uPortalPos))
                Owner.EnterWorld(uPortal);

            // setup timeout
            if (!uPortalDesc.NexusPortal)
            {
                var timeoutTime = gameData.Portals[portalType].Timeout;
                Owner.Timers.Add(new WorldTimer(timeoutTime * 1000, (w, t) => w.LeaveWorld(uPortal)));
            }

            // announce
            Owner.BroadcastPacket(new Notification
            {
                Color = new ARGB(0xFF00FF00),
                ObjectId = Id,
                Message = "Unlocked by " + Name
            }, null);
            foreach (var player in Owner.Players.Values)
                player.SendInfo(string.Format("{0} unlocked by {1}!", world.SBName, Name));
        }
        private void AEBulletNova(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            var prjs = new Projectile[20];
            var prjDesc = item.Projectiles[0]; //Assume only one
            var batch = new Packet[21];
            for (var i = 0; i < 20; i++)
            {
                var proj = CreateProjectile(prjDesc, item.ObjectType,
                    Random.Next(prjDesc.MinDamage, prjDesc.MaxDamage),
                    time.TotalElapsedMs, target, (float)(i * (Math.PI * 2) / 20));
                Owner.EnterWorld(proj);
                FameCounter.Shoot(proj);
                batch[i] = new ServerPlayerShoot()
                {
                    BulletId = proj.ProjectileId,
                    OwnerId = Id,
                    ContainerType = item.ObjectType,
                    StartingPos = target,
                    Angle = proj.Angle,
                    Damage = (short)proj.Damage
                };
                prjs[i] = proj;
            }
            batch[20] = new ShowEffect()
            {
                EffectType = EffectType.Trail,
                Pos1 = target,
                TargetObjectId = Id,
                Color = new ARGB(0xFFFF00AA)
            };

            foreach (var plr in Owner.Players.Values
                        .Where(p => p.DistSqr(this) < RadiusSqr))
            {
                plr.Client.SendPackets(batch);
            }
        }

        private void AEShurikenAbility(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            if (!HasConditionEffect(ConditionEffects.NinjaSpeedy))
            {
                ApplyConditionEffect(ConditionEffectIndex.NinjaSpeedy);
                return;
            }

            if (MP >= item.MpEndCost)
            {
                MP -= item.MpEndCost;
                AEShoot(time, item, target, eff);
            }

            ApplyConditionEffect(ConditionEffectIndex.NinjaSpeedy, 0);
        }

        private void AEDye(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            if (item.Texture1 != 0)
                Texture1 = item.Texture1;
            if (item.Texture2 != 0)
                Texture2 = item.Texture2;
        }

        private void AECreate(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            var gameData = Manager.Resources.GameData;

            ushort objType;
            if (!gameData.IdToObjectType.TryGetValue(eff.Id, out objType) ||
                !gameData.Portals.ContainsKey(objType))
                return; // object not found, ignore

            var entity = Resolve(Manager, objType);
            var timeoutTime = gameData.Portals[objType].Timeout;

            entity.Move(X, Y);
            Owner.EnterWorld(entity);

            Owner.Timers.Add(new WorldTimer(timeoutTime * 1000, (world, t) => world.LeaveWorld(entity)));

            string openedByMsg = gameData.Portals[objType].DungeonName + " opened by " + Name + "!";
            Owner.BroadcastPacket(new Notification
            {
                Color = new ARGB(0xFF00FF00),
                ObjectId = Id,
                Message = openedByMsg
            }, null);
            foreach (var player in Owner.Players.Values)
                player.SendInfo(openedByMsg);
        }

        private void AEIncrementStat(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            var idx = StatsManager.GetStatIndex((StatsType)eff.Stats);
            var statInfo = Manager.Resources.GameData.Classes[ObjectType].Stats;

            Stats.Base[idx] += eff.Amount;
            if (Stats.Base[idx] > statInfo[idx].MaxValue)
            {
                Stats.Base[idx] = statInfo[idx].MaxValue;

                // pot boosting
                var boostAmount = 1;
                if (idx == 0 || idx == 1)
                    boostAmount = 20;
                Stats.Boost.ActivateBoost[idx].AddOffset(boostAmount);
                Stats.ReCalculateValues();
                SendInfo("Already maxed... Stat boosted!");
            }
            else
            {
                SendInfo("Potion consumed...");
            }
        }
        private void AERemoveNegativeConditionSelf(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            ApplyConditionEffect(NegativeEffs);
            BroadcastSync(new ShowEffect()
            {
                EffectType = EffectType.AreaBlast,
                TargetObjectId = Id,
                Color = new ARGB(0xffffffff),
                Pos1 = new Position() { X = 1 }
            }, p => this.DistSqr(p) < RadiusSqr);
        }

        private void AERemoveNegativeConditions(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            this.AOE(eff.Range, true, player => player.ApplyConditionEffect(NegativeEffs));
            BroadcastSync(new ShowEffect()
            {
                EffectType = EffectType.AreaBlast,
                TargetObjectId = Id,
                Color = new ARGB(0xffffffff),
                Pos1 = new Position() { X = eff.Range }
            }, p => this.DistSqr(p) < RadiusSqr);
        }

        private void AEPoisonGrenade(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            BroadcastSync(new ShowEffect()
            {
                EffectType = EffectType.Throw,
                Color = new ARGB(0xffddff00),
                TargetObjectId = Id,
                Pos1 = target
            }, p => this.DistSqr(p) < RadiusSqr);

            var x = new Placeholder(Manager, 1500);
            x.Move(target.X, target.Y);
            Owner.EnterWorld(x);
            Owner.Timers.Add(new WorldTimer(1500, (world, t) =>
            {
                world.BroadcastPacketNearby(new ShowEffect()
                {
                    EffectType = EffectType.AreaBlast,
                    Color = new ARGB(0xffddff00),
                    TargetObjectId = x.Id,
                    Pos1 = new Position() { X = eff.Radius }
                }, x, null);

                world.AOE(target, eff.Radius, false,
                    enemy => PoisonEnemy(world, enemy as Enemy, eff));
            }));
        }

        private void AELightning(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            const double coneRange = Math.PI / 4;
            var mouseAngle = Math.Atan2(target.Y - Y, target.X - X);

            // get starting target
            var startTarget = this.GetNearestEntity(MaxAbilityDist, false, e => e is Enemy &&
                Math.Abs(mouseAngle - Math.Atan2(e.Y - Y, e.X - X)) <= coneRange);

            // no targets? bolt air animation
            if (startTarget == null)
            {
                var noTargets = new Packet[3];
                var angles = new double[] { mouseAngle, mouseAngle - coneRange, mouseAngle + coneRange };
                for (var i = 0; i < 3; i++)
                {
                    var x = (int)(MaxAbilityDist * Math.Cos(angles[i])) + X;
                    var y = (int)(MaxAbilityDist * Math.Sin(angles[i])) + Y;
                    noTargets[i] = new ShowEffect()
                    {
                        EffectType = EffectType.Trail,
                        TargetObjectId = Id,
                        Color = new ARGB(0xffff0088),
                        Pos1 = new Position()
                        {
                            X = x,
                            Y = y
                        },
                        Pos2 = new Position() { X = 350 }
                    };
                }
                BroadcastSync(noTargets, p => this.DistSqr(p) < RadiusSqr);
                return;
            }

            var current = startTarget;
            var targets = new Entity[eff.MaxTargets];
            for (int i = 0; i < targets.Length; i++)
            {
                targets[i] = current;
                var next = current.GetNearestEntity(10, false, e =>
                {
                    if (!(e is Enemy) ||
                        e.HasConditionEffect(ConditionEffects.Invincible) ||
                        e.HasConditionEffect(ConditionEffects.Stasis) ||
                        Array.IndexOf(targets, e) != -1)
                        return false;

                    return true;
                });

                if (next == null)
                    break;

                current = next;
            }

            var pkts = new List<Packet>();
            for (var i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null)
                    break;

                var prev = i == 0 ? this : targets[i - 1];

                (targets[i] as Enemy).Damage(this, time, eff.TotalDamage, false);

                if (eff.ConditionEffect != null)
                    targets[i].ApplyConditionEffect(new ConditionEffect()
                    {
                        Effect = eff.ConditionEffect.Value,
                        DurationMS = (int)(eff.EffectDuration * 1000)
                    });

                pkts.Add(new ShowEffect()
                {
                    EffectType = EffectType.Lightning,
                    TargetObjectId = prev.Id,
                    Color = new ARGB(0xffff0088),
                    Pos1 = new Position()
                    {
                        X = targets[i].X,
                        Y = targets[i].Y
                    },
                    Pos2 = new Position() { X = 350 }
                });
            }
            BroadcastSync(pkts, p => this.DistSqr(p) < RadiusSqr);
        }

        private void AEDecoy(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            var decoy = new Decoy(this, eff.DurationMS, 4);
            decoy.Move(X, Y);
            Owner.EnterWorld(decoy);
        }

        private void StasisBlast(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            var pkts = new List<Packet>
            {
                new ShowEffect()
                {
                    EffectType = EffectType.Concentrate,
                    TargetObjectId = Id,
                    Pos1 = target,
                    Pos2 = new Position() {X = target.X + 3, Y = target.Y},
                    Color = new ARGB(0xffffffff)
                }
            };

            Owner.AOE(target, 3, false, enemy =>
            {
                if (enemy.HasConditionEffect(ConditionEffects.StasisImmune))
                {
                    pkts.Add(new Notification()
                    {
                        ObjectId = enemy.Id,
                        Color = new ARGB(0xff00ff00),
                        Message = "Immune"
                    });
                }
                else if (!enemy.HasConditionEffect(ConditionEffects.Stasis))
                {
                    enemy.ApplyConditionEffect(ConditionEffectIndex.Stasis, eff.DurationMS);

                    Owner.Timers.Add(new WorldTimer(eff.DurationMS, (world, t) =>
                        enemy.ApplyConditionEffect(ConditionEffectIndex.StasisImmune, 3000)));

                    pkts.Add(new Notification()
                    {
                        ObjectId = enemy.Id,
                        Color = new ARGB(0xffff0000),
                        Message = "Stasis"
                    });
                }
            });
            BroadcastSync(pkts, p => this.DistSqr(p) < RadiusSqr);
        }

        private void AETrap(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            BroadcastSync(new ShowEffect()
            {
                EffectType = EffectType.Throw,
                Color = new ARGB(0xff9000ff),
                TargetObjectId = Id,
                Pos1 = target
            }, p => this.DistSqr(p) < RadiusSqr);

            Owner.Timers.Add(new WorldTimer(1500, (world, t) =>
            {
                var trap = new Trap(
                    this,
                    eff.Radius,
                    eff.TotalDamage,
                    eff.ConditionEffect ?? ConditionEffectIndex.Slowed,
                    eff.EffectDuration);
                trap.Move(target.X, target.Y);
                world.EnterWorld(trap);
            }));
        }

        private void AEVampireBlast(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            var pkts = new List<Packet>
            {
                new ShowEffect()
                {
                    EffectType = EffectType.Trail,
                    TargetObjectId = Id,
                    Pos1 = target,
                    Color = new ARGB(0xFFFF0000)
                },
                new ShowEffect
                {
                    EffectType = EffectType.Diffuse,
                    Color = new ARGB(0xFFFF0000),
                    TargetObjectId = Id,
                    Pos1 = target,
                    Pos2 = new Position { X = target.X + eff.Radius, Y = target.Y }
                }
            };

            var totalDmg = 0;
            var enemies = new List<Enemy>();
            Owner.AOE(target, eff.Radius, false, enemy =>
            {
                enemies.Add(enemy as Enemy);
                totalDmg += (enemy as Enemy).Damage(this, time, eff.TotalDamage, false);
            });

            var players = new List<Player>();
            this.AOE(eff.Radius, true, player =>
            {
                if (!player.HasConditionEffect(ConditionEffects.Sick))
                {
                    players.Add(player as Player);
                    ActivateHealHp(player as Player, totalDmg, pkts);
                }
            });

            if (enemies.Count > 0)
            {
                var rand = new Random();
                for (var i = 0; i < 5; i++)
                {
                    var a = enemies[rand.Next(0, enemies.Count)];
                    var b = players[rand.Next(0, players.Count)];
                    pkts.Add(new ShowEffect()
                    {
                        EffectType = EffectType.Flow,
                        TargetObjectId = b.Id,
                        Pos1 = new Position() { X = a.X, Y = a.Y },
                        Color = new ARGB(0xffffffff)
                    });
                }
            }

            BroadcastSync(pkts, p => this.DistSqr(p) < RadiusSqr);
        }

        private void AETeleport(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            TeleportPosition(time, target, true);
        }

        private void AEMagicNova(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            var pkts = new List<Packet>();
            this.AOE(eff.Range, true, player =>
                ActivateHealMp(player as Player, eff.Amount, pkts));
            pkts.Add(new ShowEffect()
            {
                EffectType = EffectType.AreaBlast,
                TargetObjectId = Id,
                Color = new ARGB(0xffffffff),
                Pos1 = new Position() { X = eff.Range }
            });
            BroadcastSync(pkts, p => this.DistSqr(p) < RadiusSqr);
        }

        private void AEMagic(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            var pkts = new List<Packet>();
            ActivateHealMp(this, eff.Amount, pkts);
            BroadcastSync(pkts, p => this.DistSqr(p) < RadiusSqr);
        }

        private void AEHealNova(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            var amount = eff.Amount;
            var range = eff.Range;

            var pkts = new List<Packet>();
            this.AOE(range, true, player =>
            {
                if (!player.HasConditionEffect(ConditionEffects.Sick))
                    ActivateHealHp(player as Player, amount, pkts);
            });
            pkts.Add(new ShowEffect()
            {
                EffectType = EffectType.AreaBlast,
                TargetObjectId = Id,
                Color = new ARGB(0xffffffff),
                Pos1 = new Position() { X = range }
            });
            BroadcastSync(pkts, p => this.DistSqr(p) < RadiusSqr);
        }

        private void AEHeal(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            if (!HasConditionEffect(ConditionEffects.Sick))
            {
                var pkts = new List<Packet>();
                ActivateHealHp(this, eff.Amount, pkts);
                BroadcastSync(pkts, p => this.DistSqr(p) < RadiusSqr);
            }
        }

        private void AEConditionEffectAura(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            var duration = eff.DurationMS;
            var range = eff.Range;

            this.AOE(range, true, player =>
            {
                player.ApplyConditionEffect(new ConditionEffect()
                {
                    Effect = eff.ConditionEffect.Value,
                    DurationMS = duration
                });
            });
            var color = 0xffffffff;
            if (eff.ConditionEffect.Value == ConditionEffectIndex.Damaging)
                color = 0xffff0000;
            BroadcastSync(new ShowEffect()
            {
                EffectType = EffectType.AreaBlast,
                TargetObjectId = Id,
                Color = new ARGB(color),
                Pos1 = new Position() { X = range }
            }, p => this.DistSqr(p) < RadiusSqr);
        }

        private void AEClearConditionEffectSelf(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            var condition = eff.CheckExistingEffect;
            ConditionEffects conditions = 0;

            if (condition.HasValue)
                conditions |= (ConditionEffects)(1 << (Byte)condition.Value);

            if (!condition.HasValue || HasConditionEffect(conditions))
            {
                ApplyConditionEffect(new ConditionEffect()
                {
                    Effect = eff.ConditionEffect.Value,
                    DurationMS = 0
                });
            }
        }

        private void AEClearConditionEffectAura(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            this.AOE(eff.Range, true, player =>
            {
                var condition = eff.CheckExistingEffect;
                ConditionEffects conditions = 0;
                conditions |= (ConditionEffects)(1 << (Byte)condition.Value);
                if (!condition.HasValue || player.HasConditionEffect(conditions))
                {
                    player.ApplyConditionEffect(new ConditionEffect()
                    {
                        Effect = eff.ConditionEffect.Value,
                        DurationMS = 0
                    });
                }
            });
        }

        private void AEConditionEffectSelf(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            var duration = eff.DurationMS;

            ApplyConditionEffect(new ConditionEffect()
            {
                Effect = eff.ConditionEffect.Value,
                DurationMS = duration
            });
            BroadcastSync(new ShowEffect()
            {
                EffectType = EffectType.AreaBlast,
                TargetObjectId = Id,
                Color = new ARGB(0xffffffff),
                Pos1 = new Position() { X = 1 }
            }, p => this.DistSqr(p) < RadiusSqr);
        }

        private void AEStatBoostAura(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            var idx = StatsManager.GetStatIndex((StatsType)eff.Stats);
            var amount = eff.Amount;
            var duration = eff.DurationMS;
            var range = eff.Range;

            this.AOE(range, true, player =>
            {
                ((Player)player).Stats.Boost.ActivateBoost[idx].Push(amount, false);
                ((Player)player).Stats.ReCalculateValues();

                // hack job to allow instant heal of nostack boosts
                //if (eff.NoStack && amount > 0 && idx == 0)
                //{
                //    ((Player)player).HP = Math.Min(((Player)player).Stats[0], ((Player)player).HP + amount);
                //}

                Owner.Timers.Add(new WorldTimer(duration, (world, t) =>
                {
                    ((Player)player).Stats.Boost.ActivateBoost[idx].Pop(amount, false);
                    ((Player)player).Stats.ReCalculateValues();
                }));
            });

            BroadcastSync(new ShowEffect()
            {
                EffectType = EffectType.AreaBlast,
                TargetObjectId = Id,
                Color = new ARGB(0xffffffff),
                Pos1 = new Position() { X = range }
            }, p => this.DistSqr(p) < RadiusSqr);
        }

        private void AEStatBoostSelf(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            var idx = StatsManager.GetStatIndex((StatsType)eff.Stats);
            var s = eff.Amount;
            Stats.Boost.ActivateBoost[idx].Push(s, false);
            Stats.ReCalculateValues();
            Owner.Timers.Add(new WorldTimer(eff.DurationMS, (world, t) =>
            {
                Stats.Boost.ActivateBoost[idx].Pop(s, false);
                Stats.ReCalculateValues();
            }));
            BroadcastSync(new ShowEffect()
            {
                EffectType = EffectType.Potion,
                TargetObjectId = Id,
                Color = new ARGB(0xffffffff)
            }, p => this.DistSqr(p) < RadiusSqr);
        }

        private void AEShoot(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            var arcGap = item.ArcGap * Math.PI / 180;
            var startAngle = Math.Atan2(target.Y - Y, target.X - X) - (item.NumProjectiles - 1) / 2 * arcGap;
            var prjDesc = item.Projectiles[0]; //Assume only one

            var sPkts = new Packet[item.NumProjectiles];
            for (var i = 0; i < item.NumProjectiles; i++)
            {
                var proj = CreateProjectile(prjDesc, item.ObjectType,
                    (int)Stats.GetAttackDamage(prjDesc.MinDamage, prjDesc.MaxDamage, true),
                    time.TotalElapsedMs, new Position() { X = X, Y = Y }, (float)(startAngle + arcGap * i));
                Owner.EnterWorld(proj);
                sPkts[i] = new AllyShoot()
                {
                    OwnerId = Id,
                    Angle = proj.Angle,
                    ContainerType = item.ObjectType,
                    BulletId = proj.ProjectileId
                };
                FameCounter.Shoot(proj);
            }
            BroadcastSync(sPkts, p => p != this && this.DistSqr(p) < RadiusSqr);
        }


        static void ActivateHealHp(Player player, int amount, List<Packet> pkts)
        {
            var maxHp = player.Stats[0];
            var newHp = Math.Min(maxHp, player.HP + amount);
            if (newHp == player.HP)
                return;

            pkts.Add(new ShowEffect()
            {
                EffectType = EffectType.Potion,
                TargetObjectId = player.Id,
                Color = new ARGB(0xffffffff)
            });
            pkts.Add(new Notification()
            {
                Color = new ARGB(0xff00ff00),
                ObjectId = player.Id,
                Message = "+" + (newHp - player.HP)
            });

            player.HP = newHp;
        }

        static void ActivateHealMp(Player player, int amount, List<Packet> pkts)
        {
            var maxMp = player.Stats[1];
            var newMp = Math.Min(maxMp, player.MP + amount);
            if (newMp == player.MP)
                return;

            pkts.Add(new ShowEffect()
            {
                EffectType = EffectType.Potion,
                TargetObjectId = player.Id,
                Color = new ARGB(0xffffffff)
            });
            pkts.Add(new Notification()
            {
                Color = new ARGB(0xff9000ff),
                ObjectId = player.Id,
                Message = "+" + (newMp - player.MP)
            });

            player.MP = newMp;
        }

        void PoisonEnemy(World world, Enemy enemy, ActivateEffect eff)
        {
            var remainingDmg = (int)StatsManager.GetDefenseDamage(enemy, eff.TotalDamage, enemy.ObjectDesc.Defense);
            var perDmg =( remainingDmg * 1000) / eff.DurationMS;

            WorldTimer tmr = null;
            var x = 0;

            Func<World, RealmTime, bool> poisonTick = (w, t) =>
            {
                if (enemy.Owner == null || w == null)
                    return true;

                /*w.BroadcastPacketConditional(new ShowEffect()
                {
                    EffectType = EffectType.Dead,
                    TargetObjectId = enemy.Id,
                    Color = new ARGB(0xffddff00)
                }, p => enemy.DistSqr(p) < RadiusSqr);*/

                if (x % 4 == 0) // make sure to change this if timer delay is changed
                {
                    var thisDmg = perDmg;
                    if (remainingDmg < thisDmg)
                        thisDmg = remainingDmg;

                    enemy.Damage(this, t, thisDmg, true);
                    remainingDmg -= thisDmg;
                    if (remainingDmg <= 0)
                        return true;
                }
                x++;

                tmr.Reset();
                return false;
            };

            tmr = new WorldTimer(250, poisonTick);
            world.Timers.Add(tmr);
        }

        void HealingPlayersPoison(World world, Player player, ActivateEffect eff)
        {
            var remainingHeal = eff.TotalDamage;
            var perHeal = eff.TotalDamage * 1000 / eff.DurationMS;

            WorldTimer tmr = null;
            var x = 0;

            Func<World, RealmTime, bool> healTick = (w, t) =>
            {
                if (player.Owner == null || w == null)
                    return true;

                if (x % 4 == 0) // make sure to change this if timer delay is changed
                {
                    var thisHeal = perHeal;
                    if (remainingHeal < thisHeal)
                        thisHeal = remainingHeal;

                    List<Packet> pkts = new List<Packet>();

                    Player.ActivateHealHp(player, thisHeal, pkts);
                    w.BroadcastPackets(pkts, null);
                    remainingHeal -= thisHeal;
                    if (remainingHeal <= 0)
                        return true;
                }
                x++;

                tmr.Reset();
                return false;
            };

            tmr = new WorldTimer(250, healTick);
            world.Timers.Add(tmr);
        }

  
    }
}
