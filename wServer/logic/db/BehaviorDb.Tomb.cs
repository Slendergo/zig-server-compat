﻿#region

using wServer.logic.behaviors;
using wServer.logic.loot;
using wServer.logic.transitions;
using common.resources;
#endregion

namespace wServer.logic
{
    partial class BehaviorDb
    {
        private _ Tomb = () => Behav()
            .Init("Tomb Defender",
                new State(
                    new State("idle",
                        new Taunt(true, "THIS WILL NOW BE YOUR TOMB!"),
                        new ConditionalEffect(ConditionEffectIndex.Armored),
                        new Orbit(.6, 5, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new HpLessTransition(.989, "weakning")
                        ),
                    new State("weakning",
                        new Orbit(.6, 5, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new ConditionalEffect(ConditionEffectIndex.Armored),
                        new Taunt(true, "Impudence! I am an Immortal, I needn't waste time on you!"),
                        new Shoot(50, 20, projectileIndex: 3, coolDown: 6000),
                        new State("blue shield 1",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new TimedTransition(3000, "unset blue shield 1")
                            ),
                        new State("unset blue shield 1"),
                        new HpLessTransition(.979, "active")
                        ),
                    new State("active",
                        new Orbit(.7, 5, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new ConditionalEffect(ConditionEffectIndex.Armored),
                        new Shoot(50, 8, 45, 2, 0, 0, coolDown: 1000),
                        new Shoot(50, 3, 120, 1, 0, 0, coolDown: 5000),
                        new Shoot(50, 5, 72, 0, 0, 0, coolDown: 5000),
                        new HpLessTransition(.7, "boomerang")
                        ),
                    new State("boomerang",
                        new ConditionalEffect(ConditionEffectIndex.Armored),
                        new Orbit(.6, 3, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new Taunt(true, "Nut, disable our foes!"),
                        new Shoot(50, 1, projectileIndex: 0, coolDown: 3000),
                        new Shoot(50, 8, projectileIndex: 2, coolDown: 1000),
                        new Shoot(50, 3, 15, 1, coolDown: 3000),
                        new Shoot(50, 2, 90, 1, coolDown: 3000),
                        new State("blue shield 2",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new TimedTransition(3000, "unset blue shield 2")
                            ),
                        new State("unset blue shield 2"),
                        new HpLessTransition(.55, "double shot")

                        ),
                    new State("double shot",
                        new Taunt(true, "Geb, eradicate these cretins from our tomb!"),
                        new Orbit(.7, 5, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new Shoot(50, 8, projectileIndex: 2, coolDown: 1000),
                        new Shoot(50, 2, 10, 0, coolDown: 3000),
                        new Shoot(50, 4, 15, 1, coolDown: 3000),
                        new Shoot(50, 2, 90, 1, coolDown: 3000),
                        new HpLessTransition(.4, "artifacts")
                        ),
                    new State("artifacts",
                        new ConditionalEffect(ConditionEffectIndex.Armored),
                        new Taunt(true, "Nut, let them wish they were dead!"),
                        new Orbit(.6, 7, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new Shoot(50, 8, projectileIndex: 2, coolDown: 1000),
                        new Shoot(50, 2, 10, 0, coolDown: 3000),
                        new Shoot(50, 4, 15, 1, coolDown: 3000),
                        new Shoot(50, 2, 90, 1, coolDown: 3000),
                        new Spawn("Pyramid Artifact 1", 1, 0),
                        new Spawn("Pyramid Artifact 2", 1, 0),
                        new State("blue shield 3",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new TimedTransition(3000, "unset blue shield 3")
                            ),
                        new State("unset blue shield 3"),
                        new HpLessTransition(.25, "artifacts 2")
                        ),
                    new State("artifacts 2",
                        new ConditionalEffect(ConditionEffectIndex.Armored),
                        new Taunt(true, "My artifacts shall prove my wall of defense is impenetrable!"),
                        new Orbit(.6, 7, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new Shoot(50, 8, projectileIndex: 2, coolDown: 1000),
                        new Shoot(50, 3, 10, 0, coolDown: 3000),
                        new Shoot(50, 5, 15, 1, coolDown: 3000),
                        new Shoot(50, 2, 80, 1, coolDown: 3000),
                        new Shoot(50, 2, 90, 1, coolDown: 3000),
                        new Spawn("Pyramid Artifact 1", 2, 0),
                        new State("blue shield 4",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new TimedTransition(3000, "unset blue shield 4")
                            ),
                        new State("unset blue shield 4"),
                        new HpLessTransition(.06, "rage")
                        ),
                    new State("rage",
                        new ConditionalEffect(ConditionEffectIndex.Armored),
                        new Taunt(true, "The end of your path is here!"),
                        new Follow(0.6, range: 1, duration: 5000, coolDown: 0),
                        new Flash(0xfFF0000, 1, 9000001),
                        new Shoot(50, 10, 10, 4, coolDown: 750, coolDownOffset: 750),
                        new Shoot(50, 5, 10, 4, angleOffset: 180, coolDown: 500, coolDownOffset: 500),
                        new Shoot(50, 1, projectileIndex: 0, coolDown: 1000),
                        new Shoot(50, 3, 15, 1, coolDown: 2000),
                        new Shoot(50, 2, 90, 1, coolDown: 2000),
                        new Spawn("Pyramid Artifact 1", 1, 0),
                        new Spawn("Pyramid Artifact 2", 1, 0),
                        new Spawn("Pyramid Artifact 3", 1, 0),
                        new State("blue shield 5",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new TimedTransition(3000, "unset blue shield 5")
                            ),
                        new State("unset blue shield 5")
                        )
                    ),
                    new Threshold(0.01,
                        new ItemLoot("Potion of Life", 1),
                        new ItemLoot("Ring of the Pyramid", 0.04),
                        new ItemLoot("Tome of Holy Protection", 0.01),
                        new ItemLoot("Wine Cellar Incantation", 0.05)
                    )
            )
            .Init("Tomb Support",
                new State(
                    new State("idle",
                        new Taunt(true, "ENOUGH OF YOUR VANDALISM!"),
                        new ConditionalEffect(ConditionEffectIndex.Armored),
                        new Orbit(.6, 5, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new HpLessTransition(.9875, "weakning")
                        ),
                    new State("weakning",
                        new Orbit(.6, 5, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new Taunt("Impudence! I am an immortal, I needn't take your seriously."),
                        new Shoot(50, 20, projectileIndex: 7, coolDown: 6000, coolDownOffset: 2000),
                        new State("blue shield 1",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new TimedTransition(3000, "unset blue shield 1")
                            ),
                        new State("unset blue shield 1"),
                        new HpLessTransition(.97875, "active")
                        ),
                    new State("active",
                        new Orbit(.7, 4, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new Shoot(20, 1, projectileIndex: 5, coolDown: 1000),
                        new Shoot(12, 3, 120, 1, 0, 0, coolDown: 2500, coolDownOffset: 1000),
                        new Shoot(12, 4, 90, 2, 0, 0, coolDown: 2500, coolDownOffset: 1500),
                        new Shoot(12, 5, 72, 3, 0, 0, coolDown: 2500, coolDownOffset: 2000),
                        new Shoot(12, 6, 60, 4, 0, 0, coolDown: 2500, coolDownOffset: 2500),
                        new State("blue shield 2",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new TimedTransition(3000, "unset blue shield 2")
                            ),
                        new State("unset blue shield 2"),
                        new HpLessTransition(.9, "boomerang")
                        ),
                    new State("boomerang",
                        new Orbit(.6, 6, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new Taunt(true, "Bes, protect me at once!"),
                        new Shoot(20, 1, projectileIndex: 5, coolDown: 1000),
                        new Shoot(20, 1, projectileIndex: 6, coolDown: 3000),
                        new Shoot(12, 3, 120, 1, 0, 0, coolDown: 2500, coolDownOffset: 1000),
                        new Shoot(12, 4, 90, 2, 0, 0, coolDown: 2500, coolDownOffset: 1500),
                        new Shoot(12, 5, 72, 3, 0, 0, coolDown: 2500, coolDownOffset: 2000),
                        new Shoot(12, 6, 60, 4, 0, 0, coolDown: 2500, coolDownOffset: 2500),
                        new HpLessTransition(.7, "paralyze")
                        ),
                    new State("paralyze",
                        new Orbit(.6, 7, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new Taunt(true, "Geb, eradicate these cretins from our tomb!"),
                        new Shoot(20, 1, projectileIndex: 5, coolDown: 1000),
                        new Shoot(20, 1, projectileIndex: 6, coolDown: 3000),
                        new Shoot(999, 2, 10, 8, 0, 180, coolDown: 1000),
                        new Shoot(12, 3, 120, 1, 0, 0, coolDown: 2500, coolDownOffset: 1000),
                        new Shoot(12, 4, 90, 2, 0, 0, coolDown: 2500, coolDownOffset: 1500),
                        new Shoot(12, 5, 72, 3, 0, 0, coolDown: 2500, coolDownOffset: 2000),
                        new Shoot(12, 6, 60, 4, 0, 0, coolDown: 2500, coolDownOffset: 2500),
                        new HpLessTransition(.5, "artifacts")
                        ),
                    new State("artifacts",
                        new Orbit(.6, 4, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new Taunt(true, "My artifacts shall make your lethargic lives end much more swiftly!"),
                        new Shoot(20, 1, projectileIndex: 5, coolDown: 1000),
                        new Shoot(20, 1, projectileIndex: 6, coolDown: 3000),
                        new Shoot(12, 3, 120, 1, 0, 0, coolDown: 2500, coolDownOffset: 1000),
                        new Shoot(12, 4, 90, 2, 0, 0, coolDown: 2500, coolDownOffset: 1500),
                        new Shoot(12, 5, 72, 3, 0, 0, coolDown: 2500, coolDownOffset: 2000),
                        new Shoot(12, 6, 60, 4, 0, 0, coolDown: 2500, coolDownOffset: 2500),
                        new Spawn("Sphinx Artifact 1", 1, 0),
                        new State("blue shield 3",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new TimedTransition(3000, "unset blue shield 3")
                            ),
                        new State("unset blue shield 3"),
                        new HpLessTransition(.3, "double shoot")
                        ),
                    new State("double shoot",
                        new Orbit(.6, 5, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new Shoot(20, 2, 15, 5, coolDown: 1000),
                        new Shoot(20, 2, 15, 6, coolDown: 3000),
                        new Shoot(12, 3, 120, 1, 0, 0, coolDown: 2500, coolDownOffset: 1000),
                        new Shoot(12, 4, 90, 2, 0, 0, coolDown: 2500, coolDownOffset: 1500),
                        new Shoot(12, 5, 72, 3, 0, 0, coolDown: 2500, coolDownOffset: 2000),
                        new Shoot(12, 6, 60, 4, 0, 0, coolDown: 2500, coolDownOffset: 2500),
                        new Spawn("Sphinx Artifact 1", 2, 0),
                        new State("blue shield 4",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new TimedTransition(3000, "unset blue shield 4")
                            ),
                        new State("unset blue shield 4"),
                        new HpLessTransition(.06, "rage")
                        ),
                    new State("rage",
                        new Taunt(true, "This cannot be! You shall not succeed!"),
                        new Follow(0.6, range: 1, duration: 5000, coolDown: 0),
                        new Flash(0xfFF0000, 1, 9000001),
                        new Shoot(20, 1, projectileIndex: 5, coolDown: 1000),
                        new Shoot(20, 1, 15, 0, coolDown: 750),
                        new Shoot(12, 4, 90, 1, 0, 0, coolDown: 2500, coolDownOffset: 1000),
                        new Shoot(12, 5, 72, 2, 0, 0, coolDown: 2500, coolDownOffset: 1500),
                        new Shoot(12, 6, 60, 3, 0, 0, coolDown: 2500, coolDownOffset: 2000),
                        new Shoot(12, 8, 45, 4, 0, 0, coolDown: 2500, coolDownOffset: 2500),
                        new Shoot(999, 6, 10, 8, angleOffset: 180, coolDown: 500),
                        new Spawn("Sphinx Artifact 1", 1, 0),
                        new State("blue shield 5",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new TimedTransition(3000, "unset blue shield 5")
                            ),
                        new State("unset blue shield 5")
                        )
                    ),
                    new Threshold(0.01,
                        new ItemLoot("Potion of Life", 1),
                        new ItemLoot("Ring of the Sphinx", 0.04),
                        new ItemLoot("Wine Cellar Incantation", 0.05)
                    )
            )
            .Init("Tomb Attacker",
                new State(
                    new State("idle",
                        new Taunt(true, "ENOUGH OF YOUR VANDALISM!"),
                        new ConditionalEffect(ConditionEffectIndex.Armored),
                        new Orbit(.6, 5, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new HpLessTransition(.988, "weakning")
                    ),
                    new State("weakning",
                        new Orbit(.6, 5, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new Shoot(50, 20, projectileIndex: 3, coolDown: 6000, coolDownOffset: 2000),
                        new State("blue shield 1",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new TimedTransition(3000, "unset blue shield 1")
                            ),
                        new State("unset blue shield 1"),
                        new HpLessTransition(.9788, "active")
                    ),
                    new State("active",
                        new Orbit(.6, 5, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new Shoot(14, 2, 10, 2, coolDown: 500),
                        new Shoot(12, 1, projectileIndex: 0, coolDown: 2000),
                        new State("Grenade 1",
                            new Grenade(3, 160, 10, coolDown: 1500),
                            new TimedTransition(1500, "Grenade 2")
                            ),
                        new State("Grenade 2",
                            new Grenade(4, 120, 10, coolDown: 1500),
                            new TimedTransition(1500, "Grenade 1")
                            ),
                        new HpLessTransition(.72, "lets dance")
                        ),
                    new State("lets dance",
                        new Orbit(.6, 5, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new Taunt(true, "Bes, protect me at once!"),
                        new Shoot(14, 1, projectileIndex: 2, coolDown: 500),
                        new Shoot(14, 2, 90, 2, coolDown: 1000),
                        new Shoot(14, 2, 90, 2, angleOffset: 270, coolDown: 1000),
                        new Shoot(11 + 1 / 5, 8, 45, 1, 0, coolDown: 5000),
                        new Shoot(12, 2, 45, 0, coolDown: 1500),
                        new Shoot(99, 1, projectileIndex: 4, coolDown: 500),
                        new Spawn("Scarab", 3, 0, coolDown: 10000),
                        new State("blue shield 2",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new TimedTransition(3000, "unset blue shield 2")
                            ),
                        new State("unset blue shield 2",
                            new TimedTransition(3000, "Grenade 3")
                            ),
                        new State("Grenade 3",
                            new Grenade(3, 160, 10, coolDown: 1500),
                            new TimedTransition(1500, "Grenade 4")
                            ),
                        new State("Grenade 4",
                            new Grenade(4, 120, 10, coolDown: 1500),
                            new TimedTransition(1500, "Grenade 3")
                            ),
                        new HpLessTransition(.675, "more muthafucka")
                        ),
                    new State("more muthafucka",
                        new Orbit(.6, 5, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new Taunt(true, "Nut, disable our foes!"),
                        new Spawn("Scarab", 3, 0, coolDown: 10000),
                        new Shoot(14, 2, 10, 2, coolDown: 500),
                        new Shoot(14, 1, projectileIndex: 2, coolDown: 500),
                        new Shoot(14, 2, 90, 2, coolDown: 1000),
                        new Shoot(14, 2, 90, 2, angleOffset: 270, coolDown: 1000),
                        new Shoot(11 + 1 / 5, 10, 36, 1, 0, coolDown: 5000),
                        new Shoot(12, 1, projectileIndex: 0, coolDown: 2000),
                        new Shoot(12, 2, 45, 0, coolDown: 2000),
                        new Shoot(99, 1, projectileIndex: 4, coolDown: 500),
                        new Shoot(99, 1, projectileIndex: 4, angleOffset: 90, coolDown: 500),
                        new State("Grenade 5",
                            new Grenade(3, 160, 10, coolDown: 1500),
                            new TimedTransition(1500, "Grenade 6")
                            ),
                        new State("Grenade 6",
                            new Grenade(4, 120, 10, coolDown: 1500),
                            new TimedTransition(1500, "Grenade 5")
                            ),
                        new HpLessTransition(.4, "artifacts")
                        ),
                    new State("artifacts",
                        new Orbit(.6, 4, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new Taunt(true, "My artifacts shall destroy you from your soul your flesh!"),
                        new Spawn("Scarab", 3, 0, coolDown: 10000),
                        new Shoot(14, 2, 10, 2, coolDown: 500),
                        new Shoot(14, 1, projectileIndex: 2, coolDown: 500),
                        new Shoot(14, 2, 90, 2, coolDown: 1000),
                        new Shoot(14, 2, 90, 2, angleOffset: 270, coolDown: 1000),
                        new Shoot(11 + 1 / 5, 10, 36, 1, 0, coolDown: 5000),
                        new Shoot(12, 1, projectileIndex: 0, coolDown: 2000),
                        new Shoot(12, 2, 45, 0, coolDown: 2000),
                        new Shoot(99, 1, projectileIndex: 4, coolDown: 500),
                        new Shoot(99, 1, projectileIndex: 4, angleOffset: 90, coolDown: 500),
                        new Spawn("Nile Artifact 1", 1, 0),
                        new State("blue shield 3",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new TimedTransition(3000, "unset blue shield 3")
                            ),
                        new State("unset blue shield 3",
                            new TimedTransition(3000, "Grenade 7")
                            ),
                        new State("Grenade 7",
                            new Grenade(5, 45, 10, coolDown: 1500),
                            new TimedTransition(1500, "Grenade 8")
                            ),
                        new State("Grenade 8",
                            new Grenade(4, 100, 10, coolDown: 1500),
                            new TimedTransition(1500, "Grenade 9")
                            ),
                        new State("Grenade 9",
                            new Grenade(3, 120, 10, coolDown: 1500),
                            new TimedTransition(1500, "Grenade 7")
                            ),
                        new HpLessTransition(.2, "artifacts 2")
                        ),
                    new State("artifacts 2",
                        new Orbit(.6, 4, target: "Tomb Boss Anchor", radiusVariance: 0.5),
                        new Taunt(true, "My artifacts shall destroy you from your soul your flesh!"),
                        new Spawn("Scarab", 3, 0, coolDown: 10000),
                        new Shoot(14, 2, 10, 2, coolDown: 500),
                        new Shoot(14, 1, projectileIndex: 2, coolDown: 500),
                        new Shoot(14, 2, 90, 2, coolDown: 1000),
                        new Shoot(14, 2, 90, 2, angleOffset: 270, coolDown: 1000),
                        new Shoot(11 + 1 / 5, 10, 36, 1, 0, coolDown: 5000),
                        new Shoot(12, 1, projectileIndex: 0, coolDown: 2000),
                        new Shoot(12, 2, 45, 0, coolDown: 2000),
                        new Shoot(99, 1, projectileIndex: 4, coolDown: 500),
                        new Shoot(99, 1, projectileIndex: 4, angleOffset: 90, coolDown: 500),
                        new Spawn("Nile Artifact 1", 2, 0),
                        new State("blue shield 4",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new TimedTransition(3000, "unset blue shield 4")
                            ),
                        new State("unset blue shield 4",
                            new TimedTransition(3000, "Grenade 10")
                            ),
                        new State("Grenade 10",
                            new Grenade(5, 45, 10, coolDown: 1500),
                            new TimedTransition(1500, "Grenade 11")
                            ),
                        new State("Grenade 11",
                            new Grenade(4, 100, 10, coolDown: 1500),
                            new TimedTransition(1500, "Grenade 12")
                            ),
                        new State("Grenade 12",
                            new Grenade(3, 120, 10, coolDown: 1500),
                            new TimedTransition(1500, "Grenade 10")
                            ),
                        new HpLessTransition(.06, "rage")
                        ),
                    new State("rage",
                        new Taunt(true, "This cannot be! You shall not succeed!"),
                        new Flash(0xfFF0000, 1, 9000001),
                        new StayBack(.5, 6),
                        new Shoot(11 + 1 / 5, 10, 36, 1, 0, coolDown: 5000),
                        new Shoot(14, 2, 10, 2, coolDown: 500),
                        new Shoot(14, 1, projectileIndex: 2, coolDown: 500),
                        new Shoot(14, 2, 90, 2, coolDown: 1000),
                        new Shoot(14, 2, 90, 2, angleOffset: 270, coolDown: 1000),
                        new Shoot(12, 1, projectileIndex: 0, coolDown: 2000),
                        new Shoot(12, 2, 45, 0, coolDown: 2000),
                        new Spawn("Scarab", 3, 0, coolDown: 10000),
                        new Spawn("Nile Artifact 1", 1, 0),
                        new State("blue shield 5",
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new TimedTransition(3000, "unset blue shield 5")
                            ),
                        new State("unset blue shield 5",
                            new TimedTransition(3000, "Grenade 13")
                            ),
                        new State("Grenade 13",
                            new Grenade(3, 150, 10, coolDown: 1500),
                            new TimedTransition(1500, "Grenade 14")
                            ),
                        new State("Grenade 14",
                            new Grenade(4, 120, 10, coolDown: 1500),
                            new TimedTransition(1500, "Grenade 13")
                            )
                    )
                ),
                new Threshold(0.01,
                    new ItemLoot("Potion of Life", 1),
                    new ItemLoot("Ring of the Nile", 0.04),
                    new ItemLoot("Wine Cellar Incantation", 0.05)
                )
            )
            //Minions
            .Init("Pyramid Artifact 1",
                new State(
                    new Prioritize(
                        new Orbit(1, 2, target: "Tomb Defender", radiusVariance: 0.5),
                        new Follow(0.85, range: 1, duration: 5000, coolDown: 0)
                        ),
                    new Shoot(3, 3, 120, coolDown: 2500)
                    )
            )
            .Init("Pyramid Artifact 2",
                new State(
                    new Prioritize(
                        new Orbit(1, 2, target: "Tomb Attacker", radiusVariance: 0.5),
                        new Follow(0.85, range: 1, duration: 5000, coolDown: 0)
                        ),
                    new Shoot(3, 3, 120, coolDown: 2500)
                    )
            )
            .Init("Pyramid Artifact 3",
                new State(
                    new Prioritize(
                        new Orbit(1, 2, target: "Tomb Support", radiusVariance: 0.5),
                        new Follow(0.85, range: 1, duration: 5000, coolDown: 0)
                        ),
                    new Shoot(3, 3, 120, coolDown: 2500)
                    )
            )
        .Init("Sphinx Artifact 1",
                new State(
                    new Prioritize(
                        new Orbit(1, 2, target: "Tomb Defender", radiusVariance: 0.5),
                        new Follow(0.85, range: 1, duration: 5000, coolDown: 0)
                        ),
                    new Shoot(12, 3, 120, coolDown: 2500)
                    )
            )
            .Init("Sphinx Artifact 2",
                new State(
                    new Prioritize(
                        new Orbit(1, 2, target: "Tomb Attacker", radiusVariance: 0.5),
                        new Follow(0.85, range: 1, duration: 5000, coolDown: 0)
                        ),
                    new Shoot(12, 3, 120, coolDown: 2500)
                    )
            )
            .Init("Sphinx Artifact 3",
                new State(
                    new Prioritize(
                        new Orbit(1, 2, target: "Tomb Support", radiusVariance: 0.5),
                        new Follow(0.85, range: 1, duration: 5000, coolDown: 0)
                        ),
                    new Shoot(12, 3, 120, coolDown: 2500)
                    )
            )
            .Init("Nile Artifact 1",
                new State(
                    new Prioritize(
                        new Orbit(1, 2, target: "Tomb Defender", radiusVariance: 0.5),
                        new Follow(0.85, range: 1, duration: 5000, coolDown: 0)
                        ),
                    new Shoot(12, 3, 120, coolDown: 2500)
                    )
            )
            .Init("Nile Artifact 2",
                new State(
                    new Prioritize(
                        new Orbit(1, 2, target: "Tomb Attacker", radiusVariance: 0.5),
                        new Follow(0.85, range: 1, duration: 5000, coolDown: 0)
                        ),
                    new Shoot(12, 3, 120, coolDown: 2500)
                    )
            )
            .Init("Nile Artifact 3",
                new State(
                    new Prioritize(
                        new Orbit(1, 2, target: "Tomb Support", radiusVariance: 0.5),
                        new Follow(0.85, range: 1, duration: 5000, coolDown: 0)
                        ),
                    new Shoot(12, 3, 120, coolDown: 2500)
                    )
            )
            .Init("Tomb Defender Statue",
                new State(
                    new State(
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new EntityNotExistsTransition("Inactive Sarcophagus", 1000, "checkActive"),
                        new EntityNotExistsTransition("Active Sarcophagus", 1000, "checkInactive")
                        ),
                    new State("checkActive",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new EntityNotExistsTransition("Active Sarcophagus", 1000, "ItsGoTime")
                        ),
                    new State("checkInactive",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new EntityNotExistsTransition("Inactive Sarcophagus", 1000, "ItsGoTime")
                        ),
                    new State("ItsGoTime",
                        new Transform("Tomb Defender")
                        )
                    ))
            .Init("Tomb Support Statue",
                new State(
                    new State(
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new EntityNotExistsTransition("Inactive Sarcophagus", 1000, "checkActive"),
                        new EntityNotExistsTransition("Active Sarcophagus", 1000, "checkInactive")
                        ),
                    new State("checkActive",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new EntityNotExistsTransition("Active Sarcophagus", 1000, "ItsGoTime")
                        ),
                    new State("checkInactive",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new EntityNotExistsTransition("Inactive Sarcophagus", 1000, "ItsGoTime")
                        ),
                    new State("ItsGoTime",
                        new Transform("Tomb Support")
                        )
                    ))
            .Init("Tomb Attacker Statue",
                new State(
                    new State(
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new EntityNotExistsTransition("Inactive Sarcophagus", 1000, "checkActive"),
                        new EntityNotExistsTransition("Active Sarcophagus", 1000, "checkInactive")
                        ),
                    new State("checkActive",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new EntityNotExistsTransition("Active Sarcophagus", 1000, "ItsGoTime")
                        ),
                    new State("checkInactive",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new EntityNotExistsTransition("Inactive Sarcophagus", 1000, "ItsGoTime")
                        ),
                    new State("ItsGoTime",
                        new Transform("Tomb Attacker")
                        )
                    ))
            .Init("Inactive Sarcophagus",
                new State(
                    new State(
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new EntityNotExistsTransition("Beam Priestess", 14, "checkPriest"),
                        new EntityNotExistsTransition("Beam Priest", 1000, "checkPriestess")
                        ),
                    new State("checkPriest",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new EntityNotExistsTransition("Beam Priest", 1000, "activate")
                        ),
                    new State("checkPriestess",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new EntityNotExistsTransition("Beam Priestess", 1000, "activate")
                        ),
                    new State("activate",
                        new Transform("Active Sarcophagus")
                        )
                    ))
            .Init("Scarab",
                new State(
                    new NoPlayerWithinTransition(7, "Idle"),
                    new PlayerWithinTransition(7, "Chase"),
                    new State("Idle",
                        new Wander(.1)
                    ),
                    new State("Chase",
                        new Follow(1.5, 7, 0),
                        new Shoot(3, projectileIndex: 1, coolDown: 500)
                    )
                )
                )
            .Init("Active Sarcophagus",
                new State(
                    new State(
                        new HpLessTransition(60, "stun")
                        ),
                    new State("stun",
                        new Shoot(50, 8, 10, 0, coolDown: 9999999, coolDownOffset: 500),
                        new Shoot(50, 8, 10, 0, coolDown: 9999999, coolDownOffset: 1000),
                        new Shoot(50, 8, 10, 0, coolDown: 9999999, coolDownOffset: 1500),
                        new TimedTransition(1500, "idle")
                        ),
                    new State("idle",
                        new ChangeSize(100, 100)
                        )
                    ),
                    new ItemLoot("Magic Potion", 0.002),
                    new ItemLoot("Health Potion", 0.15),
                    new Threshold(0.32,
                        new ItemLoot("Tincture of Mana", 0.15),
                        new ItemLoot("Tincture of Dexterity", 0.15),
                        new ItemLoot("Tincture of Life", 0.15)
                    )
            )
            .Init("Tomb Boss Anchor",
                new State(
                    new ConditionalEffect(ConditionEffectIndex.Invincible, true),
                    new DropPortalOnDeath("Realm Portal", 100),
                    new State("Idle",
                        new EntitiesNotExistsTransition(300, "Death", "Tomb Support", "Tomb Attacker", "Tomb Defender",
                            "Active Sarcophagus", "Tomb Defender Statue", "Tomb Support Statue", "Tomb Attacker Statue")
                    ),
                    new State("Death",
                        new Suicide()
                    )
                )
            );
    }
}