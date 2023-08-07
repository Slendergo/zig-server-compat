#region

using wServer.logic.behaviors;
using wServer.logic.loot;
using wServer.logic.transitions;
using common.resources;

#endregion

namespace wServer.logic
{
    partial class BehaviorDb
    {
        private _ CaveTT = () => Behav()
            .Init("Golden Oryx Effigy",
                new State(
                    new DropPortalOnDeath(target: "Realm Portal"),
                    new State("Ini",
                        new HpLessTransition(threshold: 0.99, targetState: "Q1 Spawn Minion")
                        ),
                    new State("Q1 Spawn Minion",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new TossObject(child: "Gold Planet", range: 7, angle: 0, coolDown: 10000000),
                        new TossObject(child: "Gold Planet", range: 7, angle: 45, coolDown: 10000000),
                        new TossObject(child: "Gold Planet", range: 7, angle: 90, coolDown: 10000000),
                        new TossObject(child: "Gold Planet", range: 7, angle: 135, coolDown: 10000000),
                        new TossObject(child: "Gold Planet", range: 7, angle: 180, coolDown: 10000000),
                        new TossObject(child: "Gold Planet", range: 7, angle: 225, coolDown: 10000000),
                        new TossObject(child: "Gold Planet", range: 7, angle: 270, coolDown: 10000000),
                        new TossObject(child: "Gold Planet", range: 7, angle: 315, coolDown: 10000000),
                        new TossObject(child: "Treasure Oryx Defender", range: 3, angle: 0, coolDown: 10000000),
                        new TossObject(child: "Treasure Oryx Defender", range: 3, angle: 90, coolDown: 10000000),
                        new TossObject(child: "Treasure Oryx Defender", range: 3, angle: 180, coolDown: 10000000),
                        new TossObject(child: "Treasure Oryx Defender", range: 3, angle: 270, coolDown: 10000000),
                        new ChangeSize(rate: -1, target: 60),
                        new TimedTransition(time: 4000, targetState: "Q1 Invulnerable")
                        ),
                    new State("Q1 Invulnerable",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        //order Expand
                        new EntitiesNotExistsTransition(99, "Q1 Vulnerable Transition", "Treasure Oryx Defender")
                        ),
                    new State("Q1 Vulnerable Transition",
                        new State("T1",
                            new SetAltTexture(2),
                            new TimedTransition(time: 50, targetState: "T2")
                            ),
                        new State("T2",
                            new SetAltTexture(minValue: 0, maxValue: 1, cooldown: 100, loop: true)
                            ),
                        new TimedTransition(time: 800, targetState: "Q1 Vulnerable")
                        ),
                    new State("Q1 Vulnerable",
                        new SetAltTexture(1),
                        new Taunt(0.75, "My protectors!", "My guardians are gone!", "What have you done?", "You destroy my guardians in my house? Blasphemy!"),
                        //order Shrink
                        new HpLessTransition(threshold: 0.75, targetState: "Q2 Invulnerable Transition")
                        ),
                    new State("Q2 Invulnerable Transition",
                        new State("T1_2",
                            new SetAltTexture(2),
                            new TimedTransition(time: 50, targetState: "T2_2")
                            ),
                        new State("T2_2",
                            new SetAltTexture(minValue: 0, maxValue: 1, cooldown: 100, loop: true)
                            ),
                        new TimedTransition(time: 800, targetState: "Q2 Spawn Minion")
                        ),
                    new State("Q2 Spawn Minion",
                        new SetAltTexture(0),
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new TossObject(child: "Treasure Oryx Defender", range: 3, angle: 0, coolDown: 10000000),
                        new TossObject(child: "Treasure Oryx Defender", range: 3, angle: 90, coolDown: 10000000),
                        new TossObject(child: "Treasure Oryx Defender", range: 3, angle: 180, coolDown: 10000000),
                        new TossObject(child: "Treasure Oryx Defender", range: 3, angle: 270, coolDown: 10000000),
                        new ChangeSize(rate: -1, target: 60),
                        new TimedTransition(time: 4000, targetState: "Q2 Invulnerable")
                        ),
                    new State("Q2 Invulnerable",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        //order expand
                        new EntitiesNotExistsTransition(99, "Q2 Vulnerable Transition", "Treasure Oryx Defender")
                        ),
                    new State("Q2 Vulnerable Transition",
                        new State("T1_3",
                            new SetAltTexture(2),
                            new TimedTransition(time: 50, targetState: "T2_3")
                            ),
                        new State("T2_3",
                            new SetAltTexture(minValue: 0, maxValue: 1, cooldown: 100, loop: true)
                            ),
                        new TimedTransition(time: 800, targetState: "Q2 Vulnerable")
                        ),
                    new State("Q2 Vulnerable",
                        new SetAltTexture(1),
                        new Taunt(0.75, "My protectors are no more!", "You Mongrels are ruining my beautiful treasure!", "You won't leave with your pilfered loot!", "I'm weakened"),
                        //Shrink
                        new HpLessTransition(threshold: 0.6, targetState: "Q3 Vulnerable Transition")
                        ),
                    new State("Q3 Vulnerable Transition",
                        new State("T1_4",
                            new SetAltTexture(2),
                            new TimedTransition(time: 50, targetState: "T2_4")
                            ),
                        new State("T2_4",
                            new SetAltTexture(minValue: 0, maxValue: 1, cooldown: 100, loop: true)
                            ),
                        new TimedTransition(time: 800, targetState: "Q3")
                        ),
                    new State("Q3",
                        new SetAltTexture(1),
                        new State("Attack1",
                            new State("CardinalBarrage",
                                new Grenade(radius: 0.5, damage: 70, range: 0, fixedAngle: 0, coolDown: 1000),
                                new Grenade(radius: 1, damage: 70, range: 3, fixedAngle: 0, coolDown: 1000),
                                new Grenade(radius: 1, damage: 70, range: 3, fixedAngle: 90, coolDown: 1000),
                                new Grenade(radius: 1, damage: 70, range: 3, fixedAngle: 180, coolDown: 1000),
                                new Grenade(radius: 1, damage: 70, range: 3, fixedAngle: 270, coolDown: 1000),
                                new TimedTransition(time: 1500, targetState: "OrdinalBarrage")
                                ),
                            new State("OrdinalBarrage",
                                new Grenade(radius: 0.5, damage: 70, range: 0, fixedAngle: 0, coolDown: 1000),
                                new Grenade(radius: 1, damage: 70, range: 3, fixedAngle: 45, coolDown: 1000),
                                new Grenade(radius: 1, damage: 70, range: 3, fixedAngle: 135, coolDown: 1000),
                                new Grenade(radius: 1, damage: 70, range: 3, fixedAngle: 225, coolDown: 1000),
                                new Grenade(radius: 1, damage: 70, range: 3, fixedAngle: 315, coolDown: 1000),
                                new TimedTransition(time: 1500, targetState: "CardinalBarrage2")
                                ),
                            new State("CardinalBarrage2",
                                new Grenade(radius: 0.5, damage: 70, range: 0, fixedAngle: 0, coolDown: 1000),
                                new Grenade(radius: 1, damage: 70, range: 3, fixedAngle: 0, coolDown: 1000),
                                new Grenade(radius: 1, damage: 70, range: 3, fixedAngle: 90, coolDown: 1000),
                                new Grenade(radius: 1, damage: 70, range: 3, fixedAngle: 180, coolDown: 1000),
                                new Grenade(radius: 1, damage: 70, range: 3, fixedAngle: 270, coolDown: 1000),
                                new TimedTransition(time: 1500, targetState: "OrdinalBarrage2")
                                ),
                            new State("OrdinalBarrage2",
                                new Grenade(radius: 0.5, damage: 70, range: 0, fixedAngle: 0, coolDown: 1000),
                                new Grenade(radius: 1, damage: 70, range: 3, fixedAngle: 45, coolDown: 1000),
                                new Grenade(radius: 1, damage: 70, range: 3, fixedAngle: 135, coolDown: 1000),
                                new Grenade(radius: 1, damage: 70, range: 3, fixedAngle: 225, coolDown: 1000),
                                new Grenade(radius: 1, damage: 70, range: 3, fixedAngle: 315, coolDown: 1000),
                                new TimedTransition(time: 1500, targetState: "CardinalBarrage")
                                ),
                            new TimedTransition(time: 8500, targetState: "Attack2")
                            ),
                        new State("Attack2",
                            new Flash(color: 0x0000FF, flashPeriod: 0.1, flashRepeats: 10),
                            new Shoot(radius: 0, count: 4, shootAngle: 90, projectileIndex: 1, defaultAngle: 90, coolDown: 10000000, coolDownOffset: 0),
                            new Shoot(radius: 0, count: 4, shootAngle: 90, projectileIndex: 1, defaultAngle: 90, coolDown: 10000000, coolDownOffset: 200),
                            new Shoot(radius: 0, count: 4, shootAngle: 90, projectileIndex: 1, defaultAngle: 80, coolDown: 10000000, coolDownOffset: 400),
                            new Shoot(radius: 0, count: 4, shootAngle: 90, projectileIndex: 1, defaultAngle: 70, coolDown: 10000000, coolDownOffset: 600),
                            new Shoot(radius: 0, count: 4, shootAngle: 90, projectileIndex: 1, defaultAngle: 60, coolDown: 10000000, coolDownOffset: 800),
                            new Shoot(radius: 0, count: 4, shootAngle: 90, projectileIndex: 1, defaultAngle: 50, coolDown: 10000000, coolDownOffset: 1000),
                            new Shoot(radius: 0, count: 4, shootAngle: 90, projectileIndex: 1, defaultAngle: 40, coolDown: 10000000, coolDownOffset: 1200),
                            new Shoot(radius: 0, count: 4, shootAngle: 90, projectileIndex: 1, defaultAngle: 30, coolDown: 10000000, coolDownOffset: 1400),
                            new Shoot(radius: 0, count: 4, shootAngle: 90, projectileIndex: 1, defaultAngle: 20, coolDown: 10000000, coolDownOffset: 1600),
                            new Shoot(radius: 0, count: 4, shootAngle: 90, projectileIndex: 1, defaultAngle: 10, coolDown: 10000000, coolDownOffset: 1800),
                            new Shoot(radius: 0, count: 4, shootAngle: 45, projectileIndex: 1, defaultAngle: 0, coolDown: 10000000, coolDownOffset: 2200),
                            new Shoot(radius: 0, count: 4, shootAngle: 45, projectileIndex: 1, defaultAngle: 0, coolDown: 10000000, coolDownOffset: 2400),
                            new Shoot(radius: 0, count: 4, shootAngle: 90, projectileIndex: 1, defaultAngle: 0, coolDown: 10000000, coolDownOffset: 2600),
                            new Shoot(radius: 0, count: 4, shootAngle: 90, projectileIndex: 1, defaultAngle: 10, coolDown: 10000000, coolDownOffset: 2800),
                            new Shoot(radius: 0, count: 4, shootAngle: 90, projectileIndex: 1, defaultAngle: 20, coolDown: 10000000, coolDownOffset: 3000),
                            new Shoot(radius: 0, count: 4, shootAngle: 90, projectileIndex: 1, defaultAngle: 30, coolDown: 10000000, coolDownOffset: 3200),
                            new Shoot(radius: 0, count: 4, shootAngle: 90, projectileIndex: 1, defaultAngle: 40, coolDown: 10000000, coolDownOffset: 3400),
                            new Shoot(radius: 0, count: 4, shootAngle: 90, projectileIndex: 1, defaultAngle: 50, coolDown: 10000000, coolDownOffset: 3600),
                            new Shoot(radius: 0, count: 4, shootAngle: 90, projectileIndex: 1, defaultAngle: 60, coolDown: 10000000, coolDownOffset: 3800),
                            new Shoot(radius: 0, count: 4, shootAngle: 90, projectileIndex: 1, defaultAngle: 70, coolDown: 10000000, coolDownOffset: 4000),
                            new Shoot(radius: 0, count: 4, shootAngle: 90, projectileIndex: 1, defaultAngle: 80, coolDown: 10000000, coolDownOffset: 4200),
                            new Shoot(radius: 0, count: 4, shootAngle: 90, projectileIndex: 1, defaultAngle: 90, coolDown: 10000000, coolDownOffset: 4400),
                            new Shoot(radius: 0, count: 4, shootAngle: 45, projectileIndex: 1, defaultAngle: 90, coolDown: 10000000, coolDownOffset: 4600),
                            new Shoot(radius: 0, count: 4, shootAngle: 90, projectileIndex: 1, defaultAngle: 90, coolDown: 10000000, coolDownOffset: 4800),
                            new Shoot(radius: 0, count: 4, shootAngle: 90, projectileIndex: 1, defaultAngle: 90, coolDown: 10000000, coolDownOffset: 5000),
                            new Shoot(radius: 0, count: 4, shootAngle: 90, projectileIndex: 1, defaultAngle: 90, coolDown: 10000000, coolDownOffset: 5200),
                            new Shoot(radius: 0, count: 4, shootAngle: 90, projectileIndex: 1, defaultAngle: 80, coolDown: 10000000, coolDownOffset: 5400),
                            new Shoot(radius: 0, count: 4, shootAngle: 90, projectileIndex: 1, defaultAngle: 70, coolDown: 10000000, coolDownOffset: 5600),
                            new Shoot(radius: 0, count: 4, shootAngle: 90, projectileIndex: 1, defaultAngle: 60, coolDown: 10000000, coolDownOffset: 5800),
                            new Shoot(radius: 0, count: 4, shootAngle: 90, projectileIndex: 1, defaultAngle: 50, coolDown: 10000000, coolDownOffset: 6000),
                            new Shoot(radius: 0, count: 4, shootAngle: 90, projectileIndex: 1, defaultAngle: 40, coolDown: 10000000, coolDownOffset: 6200),
                            new Shoot(radius: 0, count: 4, shootAngle: 90, projectileIndex: 1, defaultAngle: 30, coolDown: 10000000, coolDownOffset: 6400),
                            new Shoot(radius: 0, count: 4, shootAngle: 90, projectileIndex: 1, defaultAngle: 20, coolDown: 10000000, coolDownOffset: 6600),
                            new Shoot(radius: 0, count: 4, shootAngle: 90, projectileIndex: 1, defaultAngle: 10, coolDown: 10000000, coolDownOffset: 6800),
                            new Shoot(radius: 0, count: 4, shootAngle: 45, projectileIndex: 1, defaultAngle: 0, coolDown: 10000000, coolDownOffset: 7000),
                            new TimedTransition(time: 7000, targetState: "Recuperate")
                            ),
                        new State("Recuperate",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new HealSelf(coolDown: 1000, amount: 200),
                            new TimedTransition(time: 3000, targetState: "Attack1")
                            )
                        )
                ),
                new Threshold(0.01,
                    new ItemLoot(item: "Potion of Defense", probability: 0.3),
                    new ItemLoot(item: "Potion of Attack", probability: 0.3),
                    new ItemLoot(item: "Potion of Speed", probability: 0.3),
                    new ItemLoot(item: "Potion of Dexterity", probability: 0.3),
                    new ItemLoot(item: "Potion of Vitality", probability: 0.3),
                    new ItemLoot(item: "Potion of Wisdom", probability: 0.3),
                    new ItemLoot(item: "Wine Cellar Incantation", probability: 0.01),
                    new TierLoot(tier: 8, type: ItemType.Weapon, probability: 0.5),
                    new TierLoot(tier: 8, type: ItemType.Armor, probability: 0.5),
                    new TierLoot(tier: 9, type: ItemType.Weapon, probability: 0.1),
                    new TierLoot(tier: 9, type: ItemType.Armor, probability: 0.1),
                    new TierLoot(tier: 10, type: ItemType.Weapon, probability: 0.05),
                    new TierLoot(tier: 10, type: ItemType.Armor, probability: 0.05),
                    new TierLoot(tier: 5, type: ItemType.Ability, probability: 0.05),
                    new TierLoot(tier: 4, type: ItemType.Ability, probability: 0.5),
                    new TierLoot(tier: 4, type: ItemType.Ring, probability: 0.5),
                    new TierLoot(tier: 5, type: ItemType.Ring, probability: 0.05)
                )
            )
            .Init("Treasure Oryx Defender",
                new State(
                    new Prioritize(
                        new Orbit(speed: 0.5, radius: 3, acquireRange: 6, target: "Golden Oryx Effigy", speedVariance: 0, radiusVariance: 0)
                        ),
                    new Shoot(radius: 0, count: 8, shootAngle: 45, defaultAngle: 0, coolDown: 3000)
                )
            )
            .Init("Gold Planet",
                new State(
                    new ConditionalEffect(ConditionEffectIndex.Invincible),
                    new EntityNotExistsTransition(target: "Golden Oryx Effigy", dist: 999, targetState: "Die"),
                    new Prioritize(
                        new Orbit(speed: 0.5, radius: 7, acquireRange: 20, target: "Golden Oryx Effigy", speedVariance: 0, radiusVariance: 0)
                        ),
                    new State("GreySpiral",
                        new Shoot(radius: 0, count: 2, shootAngle: 180, projectileIndex: 1, defaultAngle: 90, coolDown: 10000, coolDownOffset: 0),
                        new Shoot(radius: 0, count: 2, shootAngle: 180, projectileIndex: 1, defaultAngle: 90, coolDown: 10000, coolDownOffset: 400),
                        new Shoot(radius: 0, count: 2, shootAngle: 180, projectileIndex: 1, defaultAngle: 80, coolDown: 10000, coolDownOffset: 800),
                        new Shoot(radius: 0, count: 2, shootAngle: 180, projectileIndex: 1, defaultAngle: 70, coolDown: 10000, coolDownOffset: 1200),
                        new Shoot(radius: 0, count: 2, shootAngle: 180, projectileIndex: 0, defaultAngle: 60, coolDown: 10000, coolDownOffset: 1600),
                        new Shoot(radius: 0, count: 2, shootAngle: 180, projectileIndex: 1, defaultAngle: 50, coolDown: 10000, coolDownOffset: 2000),
                        new Shoot(radius: 0, count: 2, shootAngle: 180, projectileIndex: 1, defaultAngle: 40, coolDown: 10000, coolDownOffset: 2400),
                        new Shoot(radius: 0, count: 2, shootAngle: 180, projectileIndex: 1, defaultAngle: 30, coolDown: 10000, coolDownOffset: 2800),
                        new Shoot(radius: 0, count: 2, shootAngle: 180, projectileIndex: 1, defaultAngle: 20, coolDown: 10000, coolDownOffset: 3200),
                        new Shoot(radius: 0, count: 2, shootAngle: 180, projectileIndex: 0, defaultAngle: 10, coolDown: 10000, coolDownOffset: 3600),
                        new Shoot(radius: 0, count: 2, shootAngle: 180, projectileIndex: 1, defaultAngle: 0, coolDown: 10000, coolDownOffset: 4000),
                        new Shoot(radius: 0, count: 2, shootAngle: 180, projectileIndex: 1, defaultAngle: -10, coolDown: 10000, coolDownOffset: 4400),
                        new Shoot(radius: 0, count: 2, shootAngle: 180, projectileIndex: 1, defaultAngle: -20, coolDown: 10000, coolDownOffset: 4800),
                        new Shoot(radius: 0, count: 2, shootAngle: 180, projectileIndex: 1, defaultAngle: -30, coolDown: 10000, coolDownOffset: 5200),
                        new Shoot(radius: 0, count: 2, shootAngle: 180, projectileIndex: 0, defaultAngle: -40, coolDown: 10000, coolDownOffset: 5600),
                        new TimedTransition(time: 5600, targetState: "Reset")
                        ),
                    new State("Reset",
                        new TimedTransition(time: 0, targetState: "GreySpiral")
                        ),
                    new State("Die",
                        new Suicide()
                        )
                )
            )
        ;
    }
}