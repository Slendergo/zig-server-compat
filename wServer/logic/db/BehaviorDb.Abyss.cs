using common.resources;
using wServer.logic.behaviors;
using wServer.logic.transitions;
using wServer.logic.loot;

namespace wServer.logic
{
    partial class BehaviorDb
    {
        private _ Abyss = () => Behav()
            .Init("Malphas Protector",
                new State(
                    new Shoot(radius: 5, count: 3, shootAngle: 5, projectileIndex: 0, predictive: 0.45, coolDown: 1200),
                    new Orbit(speed: 3.2, radius: 9, acquireRange: 20, target: "Archdemon Malphas", speedVariance: 0, radiusVariance: 0, orbitClockwise: true)
                    ),
                new Threshold(0.01,
                    new ItemLoot(item: "Magic Potion", probability: 0.06),
                    new ItemLoot(item: "Health Potion", probability: 0.04)
                    )
            )
            .Init("Malphas Missile",
                new State(
                    new State("Start",
                        new TimedTransition(time: 50, targetState: "Attacking")
                        ),
                    new State("Attacking",
                        new Follow(speed: 1.1, acquireRange: 10, range: 0.2),
                        new PlayerWithinTransition(dist: 1.3, targetState: "FlashBeforeExplode"),
                        new TimedTransition(time: 5000, targetState: "FlashBeforeExplode")
                        ),
                    new State("FlashBeforeExplode",
                        new Flash(color: 0xFFFFFF, flashPeriod: 0.1, flashRepeats: 6),
                        new TimedTransition(time: 600, targetState: "Explode")
                        ),
                    new State("Explode",
                        new Shoot(radius: 0, count: 8, shootAngle: 45, projectileIndex: 0, fixedAngle: 0),
                        new Suicide()
                        )
                    )
            )
            .Init("Archdemon Malphas",
                new State(
                    new State("start_the_fun",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new PlayerWithinTransition(dist: 11, targetState: "he_is_never_alone", seeInvis: true)
                        ),
                    new State("he_is_never_alone",
                        new Reproduce(children: "Malphas Protector", densityRadius: 24, densityMax: 3, coolDown: 1000),
                        new State("Missile_Fire",
                            new Prioritize(
                                new StayCloseToSpawn(speed: 0.3, range: 5),
                                new Follow(speed: 0.3, acquireRange: 8, range: 2)
                                ),
                            new Shoot(radius: 8, count: 1, projectileIndex: 0, angleOffset: 1, predictive: 0.15, coolDown: 900),
                            new Reproduce(children: "Malphas Missile", densityRadius: 24, densityMax: 4, coolDown: 1800),
                            new State("invulnerable1",
                                new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                                new TimedTransition(time: 2000, targetState: "vulnerable")
                                ),
                            new State("vulnerable",
                                new TimedTransition(time: 4000, targetState: "invulnerable2")
                                ),
                            new State("invulnerable2",
                                new ConditionalEffect(ConditionEffectIndex.Invulnerable)
                                ),
                            new TimedTransition(time: 9000, targetState: "Pause1")
                            ),
                        new State("Pause1",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new Prioritize(
                                new StayCloseToSpawn(speed: 0.4, range: 5),
                                new Wander(speed: 0.4)
                                ),
                            new TimedTransition(time: 2500, targetState: "Small_target")
                            ),
                        new State("Small_target",
                            new Prioritize(
                                new StayCloseToSpawn(speed: 0.8, range: 5),
                                new Wander(speed: 0.8)
                                ),
                            new ChangeSize(rate: -11, target: 30),
                            new Shoot(radius: 0, count: 6, shootAngle: 60, projectileIndex: 1, fixedAngle: 0, coolDown: 1200),
                            new Shoot(radius: 8, count: 1, angleOffset: 0.6, predictive: 0.15, coolDown: 900),
                            new TimedTransition(time: 12000, targetState: "Size_matters")
                            ),
                        new State("Size_matters",
                            new Prioritize(
                                new StayCloseToSpawn(speed: 0.2, range: 5),
                                new Wander(speed: 0.2)
                                ),
                            new State("Growbig",
                                new ChangeSize(rate: 11, target: 140),
                                new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                                new TimedTransition(time: 1800, targetState: "Shot_rotation1")
                                ),
                            new State("Shot_rotation1",
                                new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                                new Shoot(radius: 8, count: 1, projectileIndex: 2, predictive: 0.2, coolDown: 900),
                                new Shoot(radius: 0, count: 3, shootAngle: 120, projectileIndex: 3, angleOffset: 0.7, defaultAngle: 0, coolDown: 700),
                                new TimedTransition(time: 1400, targetState: "Shot_rotation2")
                                ),
                            new State("Shot_rotation2",
                                new Shoot(radius: 8, count: 1, projectileIndex: 2, predictive: 0.2, coolDown: 900),
                                new Shoot(radius: 8, count: 1, projectileIndex: 2, predictive: 0.25, coolDown: 2000),
                                new Shoot(radius: 0, count: 3, shootAngle: 120, projectileIndex: 3, angleOffset: 0.7, defaultAngle: 40, coolDown: 700),
                                new TimedTransition(time: 1400, targetState: "Shot_rotation3")
                                ),
                            new State("Shot_rotation3",
                                new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                                new Shoot(radius: 8, count: 1, projectileIndex: 2, predictive: 0.2, coolDown: 900),
                                new Shoot(radius: 0, count: 3, shootAngle: 120, projectileIndex: 3, angleOffset: 0.7, defaultAngle: 80, coolDown: 700),
                                new TimedTransition(time: 1400, targetState: "Shot_rotation1")
                                ),
                            new TimedTransition(time: 13000, targetState: "Pause2")
                            ),
                        new State("Pause2",
                            new ChangeSize(rate: -11, target: 100),
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new Prioritize(
                                new StayCloseToSpawn(speed: 0.4, range: 5),
                                new Wander(speed: 0.4)
                                ),
                            new TimedTransition(time: 2500, targetState: "Bring_on_the_flamers")
                            ),
                        new State("Bring_on_the_flamers",
                            new ChangeSize(rate: 14, target: 100),
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new Prioritize(
                                new StayCloseToSpawn(speed: 0.4, range: 5),
                                new Follow(speed: 0.4, acquireRange: 9, range: 2)
                                ),
                            new Shoot(radius: 8, count: 1, predictive: 0.25, coolDown: 2100),
                            new Reproduce(children: "Malphas Flamer", densityRadius: 24, densityMax: 5, coolDown: 500),
                            new TossObject(child: "Malphas Flamer", range: 6, angle: 0, coolDown: 9000),
                            new TossObject(child: "Malphas Flamer", range: 6, angle: 90, coolDown: 9000),
                            new TossObject(child: "Malphas Flamer", range: 6, angle: 180, coolDown: 9000),
                            new TossObject(child: "Malphas Flamer", range: 6, angle: 270, coolDown: 9000),
                            new TimedTransition(time: 8000, targetState: "Temporary_exhaustion")
                            ),
                        new State("Temporary_exhaustion",
                            new Flash(color: 0x484848, flashPeriod: 0.6, flashRepeats: 5),
                            new StayBack(speed: 0.4, distance: 4),
                            new TimedTransition(time: 3200, targetState: "Missile_Fire")
                            )
                        ),
                    new DropPortalOnDeath(target: "Realm Portal", probability: 1)
                    ),
                new Threshold(0.01,
                    new TierLoot(tier: 7, type: ItemType.Weapon, probability: 0.2),
                    new TierLoot(tier: 9, type: ItemType.Weapon, probability: 0.11),
                    new TierLoot(tier: 10, type: ItemType.Weapon, probability: 0.05),
                    new TierLoot(tier: 7, type: ItemType.Armor, probability: 0.2),
                    new TierLoot(tier: 9, type: ItemType.Armor, probability: 0.11),
                    new TierLoot(tier: 10, type: ItemType.Armor, probability: 0.05),
                    new TierLoot(tier: 3, type: ItemType.Ability, probability: 0.1),
                    new TierLoot(tier: 4, type: ItemType.Ability, probability: 0.06),
                    new TierLoot(tier: 3, type: ItemType.Ring, probability: 0.1),
                    new TierLoot(tier: 4, type: ItemType.Ring, probability: 0.06),
                    new ItemLoot(item: "Wine Cellar Incantation", probability: 0.01),
                    new ItemLoot(item: "Potion of Vitality", probability: 0.3, numRequired: 1),
                    new ItemLoot(item: "Potion of Defense", probability: 0.3),
                    new ItemLoot(item: "Demon Blade", probability: 0.01)
                )
            )
            .Init("Malphas Flamer",
                new State(
                    new State("Attacking",
                        new State("Charge",
                            new Prioritize(
                                new Follow(speed: 0.7, acquireRange: 10, range: 0.1)
                                ),
                            new PlayerWithinTransition(dist: 2, targetState: "Bullet1", seeInvis: true)
                            ),
                        new State("Bullet1",
                            new ChangeSize(rate: 20, target: 130),
                            new Flash(color: 0xFFAA00, flashPeriod: 0.2, flashRepeats: 20),
                            new Shoot(radius: 8, coolDown: 200),
                            new TimedTransition(time: 4000, targetState: "Wait1")
                            ),
                        new State("Wait1",
                            new ChangeSize(rate: -20, target: 70),
                            new Charge(speed: 3, range: 20, coolDown: 600)
                            ),
                        new HpLessTransition(threshold: 0.2, targetState: "FlashBeforeExplode")
                    ),
                    new State("FlashBeforeExplode",
                        new Flash(color: 0xFF0000, flashPeriod: 0.75, flashRepeats: 1),
                        new TimedTransition(time: 300, targetState: "Explode")
                        ),
                    new State("Explode",
                        new Shoot(radius: 0, count: 8, shootAngle: 45, defaultAngle: 0),
                        new Decay(time: 100)
                        )
                    ),
                new Threshold(0.01,
                    new ItemLoot("Health Potion", 0.1),
                    new ItemLoot("Magic Potion", 0.1)
                    )
            )
            ;
    }
}