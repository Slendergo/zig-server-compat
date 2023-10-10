using GameServer.realm;
using GameServer.realm.entities;
using GameServer.realm.entities.player;
using Shared;

namespace GameServer.logic;

public class FameCounter
{
    public const int ONE_MINUTE_MS = 60000;

    public Player Host { get; }
    public FameStats Stats { get; }

    public DbClassStats ClassStats { get; private set; }

    private int ElapsedTime;

    public FameCounter(Player player)
    {
        Host = player;
        Stats = FameStats.Read(player.Client.Character.FameStats);
        ClassStats = new DbClassStats(player.Client.Account);
    }

    public void IncrementShoot() => Stats.Shots++;
    public void IncrementShotsThatDamage() => Stats.ShotsThatDamage++;

    public void CompleteDungeon(string name)
    {
        switch (name)
        {
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

    public void Killed(Enemy enemy, bool killer)
    {
        if (enemy.ObjectDesc.God)
            Stats.GodAssists++;
        else
            Stats.MonsterAssists++;

        if (Host.Quest == enemy)
            Stats.QuestsCompleted++;
        
        if (killer)
        {
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

    public void LevelUpAssist(int amount) => Stats.LevelUpAssists += amount;
    public void UncoverTiles(int amount) => Stats.TilesUncovered += amount;
    public void Teleport() => Stats.Teleports++;
    public void UseAbility() => Stats.SpecialAbilityUses++;
    public void DrinkPot() => Stats.PotionsDrunk++;

    public void Tick(RealmTime time)
    {
        ElapsedTime += time.ElapsedMsDelta;
        if (ElapsedTime > ONE_MINUTE_MS)
        {
            ElapsedTime -= ONE_MINUTE_MS;
            Stats.MinutesActive++;
        }
    }
}