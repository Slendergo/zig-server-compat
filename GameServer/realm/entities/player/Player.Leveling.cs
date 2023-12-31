﻿namespace GameServer.realm.entities.player;

public partial class Player {
    private static readonly Dictionary<string, Tuple<int, int, int>> QuestDat =
        new() //Priority, Min, Max
        {
            // wandering quest enemies
            {"Scorpion Queen", Tuple.Create(1, 1, 6)},
            {"Bandit Leader", Tuple.Create(1, 1, 6)},
            {"Hobbit Mage", Tuple.Create(3, 3, 8)},
            {"Undead Hobbit Mage", Tuple.Create(3, 3, 8)},
            {"Giant Crab", Tuple.Create(3, 3, 8)},
            {"Desert Werewolf", Tuple.Create(3, 3, 8)},
            {"Sandsman King", Tuple.Create(4, 4, 9)},
            {"Goblin Mage", Tuple.Create(4, 4, 9)},
            {"Elf Wizard", Tuple.Create(4, 4, 9)},
            {"Dwarf King", Tuple.Create(5, 5, 10)},
            {"Swarm", Tuple.Create(6, 6, 11)},
            {"Shambling Sludge", Tuple.Create(6, 6, 11)},
            {"Great Lizard", Tuple.Create(7, 7, 12)},
            {"Wasp Queen", Tuple.Create(8, 7, 20)},
            {"Horned Drake", Tuple.Create(8, 7, 20)},

            // setpiece bosses
            {"Deathmage", Tuple.Create(5, 6, 11)},
            {"Great Coil Snake", Tuple.Create(6, 6, 12)},
            {"Lich", Tuple.Create(8, 6, 20)},
            {"Actual Lich", Tuple.Create(8, 7, 20)},
            {"Ent Ancient", Tuple.Create(9, 7, 20)},
            {"Actual Ent Ancient", Tuple.Create(9, 7, 20)},
            {"Oasis Giant", Tuple.Create(10, 8, 20)},
            {"Phoenix Lord", Tuple.Create(10, 9, 20)},
            {"Ghost King", Tuple.Create(11, 10, 20)},
            {"Actual Ghost King", Tuple.Create(11, 10, 20)},
            {"Cyclops God", Tuple.Create(12, 10, 20)},
            {"Kage Kami", Tuple.Create(12, 10, 20)},
            {"Red Demon", Tuple.Create(13, 15, 20)},

            // events
            {"Skull Shrine", Tuple.Create(14, 15, 20)},
            {"Pentaract", Tuple.Create(14, 15, 20)},
            {"Cube God", Tuple.Create(14, 15, 20)},
            {"Grand Sphinx", Tuple.Create(14, 15, 20)},
            {"Lord of the Lost Lands", Tuple.Create(14, 15, 20)},
            {"Hermit God", Tuple.Create(14, 15, 20)},
            {"Ghost Ship", Tuple.Create(14, 15, 20)},
            {"Dragon Head", Tuple.Create(14, 15, 20)},
            {"Lucky Ent God", Tuple.Create(14, 15, 20)},
            {"Lucky Djinn", Tuple.Create(14, 15, 20)},

            // dungeon bosses
            {"Arachna the Spider Queen", Tuple.Create(15, 1, 20)},
            {"Stheno the Snake Queen", Tuple.Create(15, 1, 20)},
            {"Mixcoatl the Masked God", Tuple.Create(15, 1, 20)},
            {"Limon the Sprite God", Tuple.Create(15, 1, 20)},
            {"Septavius the Ghost God", Tuple.Create(15, 1, 20)},
            {"Davy Jones", Tuple.Create(15, 1, 20)},
            {"Lord Ruthven", Tuple.Create(15, 1, 20)},
            {"Archdemon Malphas", Tuple.Create(15, 1, 20)},
            {"Thessal the Mermaid Goddess", Tuple.Create(15, 1, 20)},
            {"Dr Terrible", Tuple.Create(15, 1, 20)},
            {"Horrific Creation", Tuple.Create(15, 1, 20)},
            {"Masked Party God", Tuple.Create(15, 1, 20)},
            {"Oryx Stone Guardian Left", Tuple.Create(15, 1, 20)},
            {"Oryx Stone Guardian Right", Tuple.Create(15, 1, 20)},
            {"Oryx the Mad God 1", Tuple.Create(15, 1, 20)},
            {"Oryx the Mad God 2", Tuple.Create(15, 1, 20)},
            {"Gigacorn", Tuple.Create(15, 1, 20)},
            {"Desire Troll", Tuple.Create(15, 1, 20)},
            {"Spoiled Creampuff", Tuple.Create(15, 1, 20)},
            {"MegaRototo", Tuple.Create(15, 1, 20)},
            {"Swoll Fairy", Tuple.Create(15, 1, 20)},
            {"BedlamGod", Tuple.Create(15, 1, 20)},
            {"Troll 3", Tuple.Create(15, 1, 20)},
            {"Arena Ghost Bride", Tuple.Create(15, 1, 20)},
            {"Arena Statue Left", Tuple.Create(15, 1, 20)},
            {"Arena Statue Right", Tuple.Create(15, 1, 20)},
            {"Arena Grave Caretaker", Tuple.Create(15, 1, 20)},
            {"Ghost of Skuld", Tuple.Create(15, 1, 20)},
            {"Tomb Defender", Tuple.Create(15, 1, 20)},
            {"Tomb Support", Tuple.Create(15, 1, 20)},
            {"Tomb Attacker", Tuple.Create(15, 1, 20)},
            {"Active Sarcophagus", Tuple.Create(15, 1, 20)}
        };

    public Entity Quest { get; private set; }

    public static int GetExpGoal(int level) {
        return 50 + (level - 1) * 100;
    }

    public static int GetLevelExp(int level) {
        if (level == 1) return 0;
        return 50 * (level - 1) + (level - 2) * (level - 1) * 50;
    }

    public static int GetFameGoal(int fame) {
        if (fame >= 2000) return 0;
        if (fame >= 800) return 2000;
        if (fame >= 400) return 800;
        if (fame >= 150) return 400;
        if (fame >= 20) return 150;
        return 20;
    }

    public int GetStars() {
        var ret = 0;
        foreach (var i in FameCounter.ClassStats.AllKeys) {
            var entry = FameCounter.ClassStats[ushort.Parse(i)];
            if (entry.BestFame >= 2000) ret += 5;
            else if (entry.BestFame >= 800) ret += 4;
            else if (entry.BestFame >= 400) ret += 3;
            else if (entry.BestFame >= 150) ret += 2;
            else if (entry.BestFame >= 20) ret += 1;
        }

        return ret;
    }

    private Entity FindQuest(Position? destination = null) {
        Entity ret = null;
        double? bestScore = null;
        var pX = !destination.HasValue ? X : destination.Value.X;
        var pY = !destination.HasValue ? Y : destination.Value.Y;

        foreach (var i in Owner.Quests.Values
                     .OrderBy(quest => MathsUtils.DistSqr(quest.X, quest.Y, pX, pY))) {
            if (i.ObjectDesc == null || !i.ObjectDesc.Quest) continue;

            if (!QuestDat.TryGetValue(i.ObjectDesc.ObjectId, out var x))
                continue;

            if (Level >= x.Item2 && Level <= x.Item3) {
                var score = (20 - Math.Abs(i.ObjectDesc.Level - Level)) * x.Item1 - //priority * level diff
                            this.Dist(i) / 100; //minus 1 for every 100 tile distance
                if (bestScore == null || score > bestScore) {
                    bestScore = score;
                    ret = i;
                }
            }
        }

        return ret;
    }

    public void HandleQuest(RealmTime time, bool force = false, Position? destination = null) {
        
        if (Owner == null)
            return;

        if (force || time.TickCount % 500 == 0 || Quest == null || Quest.Owner == null) {
            var newQuest = FindQuest(destination);
            if (newQuest != null && newQuest != Quest) {
                Client.SendQuestObjectId(newQuest.Id);
                Quest = newQuest;
            }
        }
    }

    public void CalculateFame() {
        var newFame = Experience < 200 * 1000 ? Experience / 1000 : 200 + (Experience - 200 * 1000) / 1000;

        if (newFame == Fame)
            return;

        var stats = FameCounter.ClassStats[ObjectType];
        var newGoal = GetFameGoal(stats.BestFame > newFame ? stats.BestFame : newFame);

        if (newGoal > FameGoal) {
            foreach (var player in Owner.Players.Values)
                if (player.DistSqr(this) < RADIUS_SQR)
                    player.Client.SendNotification(Id, "Class Quest Complete!", new ARGB(0xff00ff00));

            Stars = GetStars();
        }

        Fame = newFame;
        FameGoal = newGoal;
    }

    private bool CheckLevelUp() {
        if (Experience - GetLevelExp(Level) >= ExperienceGoal && Level < 20) {
            Level++;
            ExperienceGoal = GetExpGoal(Level);
            var statInfo = Manager.Resources.GameData.Classes[ObjectType].Stats;
            var rand = new Random();
            for (var i = 0; i < statInfo.Length; i++) {
                var min = statInfo[i].MinIncrease;
                var max = statInfo[i].MaxIncrease + 1;
                Stats.Base[i] += rand.Next(min, max);
                if (Stats.Base[i] > statInfo[i].MaxValue)
                    Stats.Base[i] = statInfo[i].MaxValue;
            }

            HP = Stats[0];
            MP = Stats[1];

            if (Level == 20)
                foreach (var i in Owner.Players.Values)
                    i.SendInfo(Name + " achieved level 20");
            else
                // to get exp scaled to new exp goal
                InvokeStatChange(StatsType.Experience, Experience - GetLevelExp(Level), true);

            Quest = null;

            return true;
        }

        CalculateFame();
        return false;
    }

    public bool EnemyKilled(Enemy enemy, int exp, bool killer) {
        if (enemy == Quest)
            foreach (var player in Owner.Players.Values)
                if (player.DistSqr(this) < RADIUS_SQR)
                    player.Client.SendNotification(Id, "Quest Complete!", new ARGB(0xff00ff00));

        if (exp != 0) Experience += exp;
        FameCounter.Killed(enemy, killer);
        return CheckLevelUp();
    }
}