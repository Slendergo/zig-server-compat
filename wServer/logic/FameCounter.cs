using common;
using wServer.realm;
using wServer.realm.entities;

namespace wServer.logic;

public class FameCounter {
    private int elapsed;

    public FameCounter(Player player) {
        Host = player;
        Stats = FameStats.Read(player.Client.Character.FameStats);
        ClassStats = new DbClassStats(player.Client.Account);
    }

    public Player Host { get; }

    public FameStats Stats { get; }
    public DbClassStats ClassStats { get; private set; }

    //HashSet<Projectile> projs = new HashSet<Projectile>();
    public void Shoot(Projectile proj) {
        Stats.Shots++;
        //projs.Add(proj);
    }

    public void Hit(Projectile proj, Enemy enemy) {
        //if (projs.Contains(proj))
        //{
        //    projs.Remove(proj);
        Stats.ShotsThatDamage++;
        //}
    }

    public void CompleteDungeon(string name) {
        switch (name) {
            case "PirateCave":
                Stats.PirateCavesCompleted++;
                break;
            case "Undead Lair":
                Stats.UndeadLairsCompleted++;
                break;
            case "Abyss":
                Stats.AbyssOfDemonsCompleted++;
                break;
            case "Snake Pit":
                Stats.SnakePitsCompleted++;
                break;
            case "Spider Den":
                Stats.SpiderDensCompleted++;
                break;
            case "Sprite World":
                Stats.SpriteWorldsCompleted++;
                break;
            case "Tomb":
                Stats.TombsCompleted++;
                break;
            case "OceanTrench":
                Stats.TrenchesCompleted++;
                break;
            case "Forbidden Jungle":
                Stats.JunglesCompleted++;
                break;
            case "Manor of the Immortals":
                Stats.ManorsCompleted++;
                break;
        }
    }


    public void Killed(Enemy enemy, bool killer) {
        if (enemy.ObjectDesc.God)
            Stats.GodAssists++;
        else
            Stats.MonsterAssists++;
        if (Host.Quest == enemy)
            Stats.QuestsCompleted++;
        if (killer) {
            if (enemy.ObjectDesc.God)
                Stats.GodKills++;
            else
                Stats.MonsterKills++;

            if (enemy.ObjectDesc.Cube)
                Stats.CubeKills++;
            if (enemy.ObjectDesc.Oryx)
                Stats.OryxKills++;
        }
    }

    public void LevelUpAssist(int count) {
        Stats.LevelUpAssists += count;
    }

    public void TileSent(int num) {
        Stats.TilesUncovered += num;
    }

    public void Teleport() {
        Stats.Teleports++;
    }

    public void UseAbility() {
        Stats.SpecialAbilityUses++;
    }

    public void DrinkPot() {
        Stats.PotionsDrunk++;
    }

    public void Tick(RealmTime time) {
        elapsed += time.ElapsedMsDelta;
        if (elapsed > 1000 * 60) {
            elapsed -= 1000 * 60;
            Stats.MinutesActive++;
        }
    }
}