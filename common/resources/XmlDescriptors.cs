using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace common.resources
{
    public enum ItemType
    {
        Weapon,
        Ability,
        Armor,
        Ring,
        Potion,
        StatPot,
        Other,
        None
    }

    public enum CurrencyType
    {
        Gold = 0,
        Fame = 1,
        GuildFame = 2
    }

    public enum TerrainType
    {
        None,
        Mountains,
        HighSand,
        HighPlains,
        HighForest,
        MidSand,
        MidPlains,
        MidForest,
        LowSand,
        LowPlains,
        LowForest,
        ShoreSand,
        ShorePlains,
        BeachTowels
    }

    public enum TileRegion
    {
        None = 0,
        Spawn = 1,
        Realm_Portals = 2,
        Store_1 = 3,
        Store_2 = 4,
        Store_3 = 5,
        Store_4 = 6,
        Store_5 = 7,
        Store_6 = 8,
        Vault = 9,
        Loot = 10,
        Defender = 11,
        Hallway = 12,
        Enemy = 13,
        Hallway1 = 14,
        Hallway2 = 15,
        Hallway3 = 16,
        Store_7 = 17,
        Store_8 = 18,
        Store_9 = 19,
        Store_10 = 21,
        Store_11 = 22,
        Store_12 = 23,
        Store_13 = 24,
        Store_14 = 26,
        Store_15 = 27,
        Store_16 = 28,
        Store_17 = 29,
        Store_18 = 30,
        Store_19 = 31,
        Store_20 = 32,
        Store_21 = 33,
        Store_22 = 34,
        Store_23 = 35,
        Store_24 = 36
    }

    public enum ActivateEffects
    {
        Create,
        Dye,
        Shoot,
        IncrementStat,
        Heal,
        Magic,
        HealNova,
        StatBoostSelf,
        StatBoostAura,
        BulletNova,
        ConditionEffectSelf,
        ConditionEffectAura,
        Teleport,
        PoisonGrenade,
        VampireBlast,
        Trap,
        StasisBlast,
        Pet,
        Decoy, 
        Lightning,
        UnlockPortal,
        MagicNova,
        ClearConditionEffectAura,
        RemoveNegativeConditions,
        ClearConditionEffectSelf,
        RemoveNegativeConditionsSelf,
        ShurikenAbility,
        DazeBlast,
        PermaPet,
        Backpack
    }

    [Flags]
    public enum ConditionEffects : ulong
    {
        Dead = 1 << 0,
        Quiet = 1 << 1,
        Weak = 1 << 2,
        Slowed = 1 << 3,
        Sick = 1 << 4,
        Dazed = 1 << 5,
        Stunned = 1 << 6,
        Blind = 1 << 7,
        Hallucinating = 1 << 8,
        Drunk = 1 << 9,
        Confused = 1 << 10,
        StunImmune = 1 << 11,
        Invisible = 1 << 12,
        Paralyzed = 1 << 13,
        Speedy = 1 << 14,
        Bleeding = 1 << 15,
        NotUsed = 1 << 16,
        Healing = 1 << 17,
        Damaging = 1 << 18,
        Berserk = 1 << 19,
        Paused = 1 << 20,
        Stasis = 1 << 21,
        StasisImmune = 1 << 22,
        Invincible = 1 << 23,
        Invulnerable = 1 << 24,
        Armored = 1 << 25,
        ArmorBroken = 1 << 26,
        Hexed = 1 << 27,
        NinjaSpeedy = 1 << 28
    }

    public enum ConditionEffectIndex
    {
        Dead = 0,
        Quiet = 1,
        Weak = 2,
        Slowed = 3,
        Sick = 4,
        Dazed = 5,
        Stunned = 6,
        Blind = 7,
        Hallucinating = 8,
        Drunk = 9,
        Confused = 10,
        StunImmune = 11,
        Invisible = 12,
        Paralyzed = 13,
        Speedy = 14,
        Bleeding = 15,
        NotUsed = 16,
        Healing = 17,
        Damaging = 18,
        Berserk = 19,
        Paused = 20,
        Stasis = 21,
        StasisImmune = 22,
        Invincible = 23,
        Invulnerable = 24,
        Armored = 25,
        ArmorBroken = 26,
        Hexed = 27,
        NinjaSpeedy = 28,
    }

    public class ObjectDesc
    {
        public readonly ushort ObjectType;
        public readonly string ObjectId;
        public readonly string DisplayId;
        public readonly string DisplayName;
        public readonly string Class;
        public readonly bool Static;
        public readonly bool OccupySquare;
        public readonly bool FullOccupy;
        public readonly bool EnemyOccupySquare;
        public readonly bool BlocksSight;
        public readonly bool Container;
        public readonly int[] SlotTypes;
        public readonly bool CanPutNormalObjects;
        public readonly bool CanPutSoulboundObjects;
        public readonly bool Loot;
        public readonly int Size;
        public readonly bool Enemy;
        public readonly int MaxHP;
        public readonly int Defense;
        public readonly float ExpMultiplier;
        public readonly int MinSize;
        public readonly int MaxSize;
        public readonly int SizeStep;
        public readonly bool SpawnPoint;
        public readonly string Group;
        public readonly bool Quest;
        public readonly int Level;
        public readonly bool God;
        public readonly bool NoArticle;
        public readonly bool StasisImmune;
        public readonly bool StunImmune;
        public readonly bool Encounter;
        public readonly int PerRealmMax;
        public readonly bool Hero;
        public readonly bool Cube;
        public readonly bool Oryx;
        public readonly bool Player;
        public readonly bool KeepDamageRecord;
        public readonly bool Connects;
        public readonly bool ProtectFromGroundDamage;
        public readonly bool ProtectFromSink;
        public readonly bool Character;
        public readonly ProjectileDesc[] Projectiles;

        public readonly TerrainType Terrain;
        public readonly float SpawnProb;
        public readonly SpawnCount Spawn;

        public ObjectDesc(ushort type, XElement e)
        {
            ObjectType = type;
            ObjectId = e.GetAttribute<string>("id");
            DisplayId = e.GetValue<string>("DisplayId");
            DisplayName = string.IsNullOrWhiteSpace(DisplayId) ? ObjectId : DisplayId;
            Class = e.GetValue<string>("Class");
            Static = e.HasElement("Static");
            OccupySquare = e.HasElement("OccupySquare");
            FullOccupy = e.HasElement("FullOccupy");
            EnemyOccupySquare = e.HasElement("EnemyOccupySquare");
            BlocksSight = e.HasElement("BlocksSight");
            Container = e.HasElement("Container");
            if (e.HasElement("SlotTypes"))
                SlotTypes = e.GetValue<string>("SlotTypes").CommaToArray<int>();
            CanPutNormalObjects = e.HasElement("CanPutNormalObjects");
            CanPutSoulboundObjects = e.HasElement("CanPutSoulboundObjects");
            Loot = e.HasElement("Loot");
            Size = e.GetValue<int>("Size", 100);
            Enemy = e.HasElement("Enemy");
            MaxHP = e.GetValue<int>("MaxHitPoints");
            Defense = e.GetValue<int>("Defense");
            ExpMultiplier = e.GetValue<float>("XpMult", 1.0f);
            if (e.HasElement("MinSize") && e.HasElement("MaxSize"))
            {
                MinSize = e.GetValue<int>("MinSize");
                MaxSize = e.GetValue<int>("MaxSize");
                SizeStep = e.GetValue<int>("SizeStep", 1);
            }
            else
            {
                MinSize = MaxSize = Size;
                SizeStep = 0;
            }
            Character = Class.Equals("Character");
            SpawnPoint = e.HasElement("SpawnPoint");
            Group = e.GetValue<string>("Group");
            Quest = e.HasElement("Quest");
            Level = e.GetValue<int>("Level");
            God = e.HasElement("God");
            NoArticle = e.HasElement("NoArticle");
            StasisImmune = e.HasElement("StasisImmune");
            StunImmune = e.HasElement("StunImmune");
            SpawnProb = e.GetValue<float>("SpawnProb");
            if (e.HasElement("Spawn"))
                Spawn = new SpawnCount(e.Element("Spawn"));
            Terrain = (TerrainType)Enum.Parse(typeof(TerrainType), e.GetValue<string>("Terrain", "None"));
            Encounter = e.HasElement("Encounter");
            PerRealmMax = e.GetValue<int>("PerRealmMax");
            Hero = e.HasElement("Hero");
            Cube = e.HasElement("Cube");
            Oryx = e.HasElement("Oryx");
            Player = e.HasElement("Player");
            KeepDamageRecord = e.HasElement("KeepDamageRecord");
            Connects = e.HasElement("Connects");
            ProtectFromGroundDamage = e.HasElement("ProtectFromGroundDamage");
            ProtectFromSink = e.HasElement("ProtectFromSink");

            var projs = new List<ProjectileDesc>();
            foreach (var i in e.Elements("Projectile"))
                projs.Add(new ProjectileDesc(i));
            Projectiles = projs.ToArray();
        }
    }

    public class SpawnCount
    {
        public readonly int Mean;
        public readonly int StdDev;
        public readonly int Min;
        public readonly int Max;

        public SpawnCount(XElement e)
        {
            Mean = e.GetValue<int>("Mean");
            StdDev = e.GetValue<int>("StdDev");
            Min = e.GetValue<int>("Min");
            Max = e.GetValue<int>("Max");
        }
    }

    public class PortalDesc : ObjectDesc
    {
        public readonly string DungeonName;
        public readonly bool IntergamePortal;
        public readonly bool Locked;
        public readonly int Timeout;
        public readonly bool NexusPortal;

        public PortalDesc(ushort type, XElement e) : base(type, e)
        {
            NexusPortal = e.HasElement("NexusPortal");
            DungeonName = e.GetValue<string>("DungeonName");
            IntergamePortal = e.HasElement("IntergamePortal");
            Locked = e.HasElement("LockedPortal");
            Timeout = e.GetValue<int>("Timeout", 30);
        }
    }

    public class PlayerDesc : ObjectDesc
    {
        public readonly ushort[] Equipment;
        public readonly Stat[] Stats;
        public readonly UnlockClass Unlock;

        public PlayerDesc(ushort type, XElement e) : base(type, e)
        {
            Equipment = e.GetValue<string>("Equipment").CommaToArray<ushort>();
            Stats = new Stat[8];
            for (var i = 0; i < Stats.Length; i++)
                Stats[i] = new Stat(i, e);
            if (e.HasElement("UnlockLevel") || e.HasElement("UnlockCost"))
                Unlock = new UnlockClass(e);
        }
    }

    public class Stat
    {
        public readonly string Type;
        public readonly int MaxValue;
        public readonly int StartingValue;
        public readonly int MinIncrease;
        public readonly int MaxIncrease;

        public Stat(int index, XElement e)
        {
            Type = StatIndexToName(index);
            var x = e.Element(Type);
            if (x != null)
            {
                StartingValue = int.Parse(x.Value);
                MaxValue = x.GetAttribute<int>("max");
            }

            var y = e.Elements("LevelIncrease");
            foreach (var s in y)
                if (s.Value == Type)
                {
                    MinIncrease = s.GetAttribute<int>("min");
                    MaxIncrease = s.GetAttribute<int>("max");
                    break;
                }
        }

        private static string StatIndexToName(int index)
        {
            switch (index)
            {
                case 0: return "MaxHitPoints";
                case 1: return "MaxMagicPoints";
                case 2: return "Attack";
                case 3: return "Defense";
                case 4: return "Speed";
                case 5: return "Dexterity";
                case 6: return "HpRegen";
                case 7: return "MpRegen";
            }
            return null;
        }
    }

    public class UnlockClass
    {
        public readonly ushort? Type;
        public readonly ushort? Level;
        public readonly uint? Cost;

        public UnlockClass(XElement e)
        {
            var n = e.Element("UnlockLevel");
            if(n != null && n.HasAttribute("type") && n.HasAttribute("level"))
            {
                Type = n.GetAttribute<ushort>("type");
                Level = n.GetAttribute<ushort>("level");
            }

            n = e.Element("UnlockCost");
            if (n != null)
            {
                Cost = (uint)int.Parse(n.Value);
            }
        }
    }

    public class SkinDesc
    {
        public readonly ushort Type;
        public readonly string ObjectId;
        public readonly ushort PlayerClassType;
        public readonly int UnlockLevel;
        public readonly int Cost;
        public readonly int Size;

        public SkinDesc(ushort type, XElement e)
        {
            Type = type;
            ObjectId = e.GetAttribute<string>("id");
            PlayerClassType = e.GetValue<ushort>("PlayerClassType");
            UnlockLevel = e.GetValue<int>("UnlockLevel");
            Cost = e.GetValue<int>("Cost", 900);
            Size = e.GetValue<int>("Size", 100);
        }
    }

    public class Item
    {
        public readonly ushort ObjectType;
        public readonly string ObjectId;
        public readonly string Class;
        public readonly string DisplayId;
        public readonly string DisplayName;
        public readonly int Texture1;
        public readonly int Texture2;
        public readonly int SlotType;
        public readonly string Description;
        public readonly bool Consumable;
        public readonly bool InvUse;
        public readonly bool TypeOfConsumable;
        public readonly bool Soulbound;
        public readonly bool Potion;
        public readonly bool Usable;
        public readonly bool Resurrects;
        public readonly float RateOfFire;
        public readonly int? Tier;
        public readonly int BagType;
        public readonly int FameBonus;
        public readonly int NumProjectiles;
        public readonly float ArcGap;
        public readonly int MpCost;
        public readonly float Cooldown;
        public readonly int Doses;
        public readonly string SuccessorId;
        public readonly bool Backpack;
        public readonly bool LDBoosted;
        public readonly bool LTBoosted;
        public readonly bool XpBoost;
        public readonly float Timer;
        public readonly int MpEndCost;

        public readonly KeyValuePair<int, int>[] StatsBoost;
        public readonly ActivateEffect[] ActivateEffects;
        public readonly ProjectileDesc[] Projectiles;

        public Item(ushort type, XElement e)
        {
            ObjectType = type;
            ObjectId = e.GetAttribute<string>("id");
            Class = e.GetValue<string>("Class");
            DisplayId = e.GetValue<string>("DisplayId");
            DisplayName = string.IsNullOrWhiteSpace(DisplayId) ? ObjectId : DisplayId;
            Texture1 = e.GetValue<int>("Tex1");
            Texture2 = e.GetValue<int>("Tex2");
            SlotType = e.GetValue<int>("SlotType");
            Description = e.GetValue<string>("Description");
            Consumable = e.HasElement("Consumable");
            Soulbound = e.HasElement("Soulbound");
            Potion = e.HasElement("Potion");
            Usable = e.HasElement("Usable");
            Resurrects = e.HasElement("Resurrects");
            RateOfFire = e.GetValue<float>("RateOfFire");
            if (e.HasElement("Tier"))
                Tier = e.GetValue<int>("Tier");
            BagType = e.GetValue<int>("BagType");
            FameBonus = e.GetValue<int>("FameBonus");
            NumProjectiles = e.GetValue<int>("NumProjectiles", 1);
            ArcGap = e.GetValue<float>("ArcGap", 11.25f);
            MpCost = e.GetValue<int>("MpCost");
            Cooldown = e.GetValue<float>("Cooldown", 0.5f);
            Doses = e.GetValue<int>("Doses");
            SuccessorId = e.GetValue<string>("SuccessorId");
            Backpack = e.HasElement("Backpack");
            LDBoosted = e.HasElement("LDBoosted");
            LTBoosted = e.HasElement("LTBoosted");
            XpBoost = e.HasElement("XpBoost");
            Timer = e.GetValue<float>("Timer");
            MpEndCost = e.GetValue<int>("MpEndCost", 0);
            InvUse = e.HasElement("InvUse");
            TypeOfConsumable = InvUse || Consumable;

            var stats = new List<KeyValuePair<int, int>>();
            foreach (XElement i in e.Elements("ActivateOnEquip"))
                stats.Add(new KeyValuePair<int, int>(
                    i.GetAttribute<int>("stat"),
                    i.GetAttribute<int>("amount")));
            StatsBoost = stats.ToArray();

            var activate = new List<ActivateEffect>();
            foreach (var i in e.Elements("Activate"))
                activate.Add(new ActivateEffect(i));
            ActivateEffects = activate.ToArray();

            var projs = new List<ProjectileDesc>();
            foreach (var i in e.Elements("Projectile"))
                projs.Add(new ProjectileDesc(i));
            Projectiles = projs.ToArray();
        }
    }

    public class ProjectileDesc
    {
        public readonly int BulletType;
        public readonly string ObjectId;
        public readonly float Speed;
        public readonly int MinDamage;
        public readonly int MaxDamage;
        public readonly float LifetimeMS;
        public readonly bool MultiHit;
        public readonly bool PassesCover;
        public readonly bool Parametric;
        public readonly bool Boomerang;
        public readonly bool ArmorPiercing;
        public readonly bool Wavy;

        public readonly ConditionEffect[] Effects;

        public readonly float Amplitude;
        public readonly float Frequency;
        public readonly float Magnitude;

        public ProjectileDesc(XElement e)
        {
            BulletType = e.GetAttribute<int>("id");
            ObjectId = e.GetValue<string>("ObjectId");
            LifetimeMS = e.GetValue<float>("LifetimeMS");
            Speed = e.GetValue<float>("Speed", 100);

            var dmg = e.Element("Damage");
            if (dmg != null)
                MinDamage = MaxDamage = e.GetValue<int>("Damage");
            else
            {
                MinDamage = e.GetValue<int>("MinDamage");
                MaxDamage = e.GetValue<int>("MaxDamage");
            }

            List<ConditionEffect> effects = new List<ConditionEffect>();
            foreach (var i in e.Elements("ConditionEffect"))
                effects.Add(new ConditionEffect(i));
            Effects = effects.ToArray();

            MultiHit = e.HasElement("MultiHit");
            PassesCover = e.HasElement("PassesCover");
            ArmorPiercing = e.HasElement("ArmorPiercing");
            Wavy = e.HasElement("Wavy");
            Parametric = e.HasElement("Parametric");
            Boomerang = e.HasElement("Boomerang");

            Amplitude = e.GetValue<float>("Amplitude", 0);
            Frequency = e.GetValue<float>("Frequency", 1);
            Magnitude = e.GetValue<float>("Magnitude", 3);
        }
    }

    public class ConditionEffect
    {
        public ConditionEffectIndex Effect;
        public int DurationMS;

        public ConditionEffect() { }
        public ConditionEffect(XElement e)
        {
            Effect = Utils.GetEffect(e.Value);
            DurationMS = (int)(e.GetAttribute<float>("duration") * 1000.0f);
        }
    }

    public class ActivateEffect
    {
        public readonly ActivateEffects Effect;
        public readonly ConditionEffectIndex? ConditionEffect;
        public readonly ConditionEffectIndex? CheckExistingEffect;

        public readonly int TotalDamage;
        public readonly float Radius;
        public readonly float EffectDuration;
        public readonly float DurationSec;
        public readonly int DurationMS;
        public readonly int Amount;
        public readonly float Range;
        public readonly float MaximumDistance;
        public readonly string ObjectId;
        public readonly string Id;
        public readonly int MaxTargets;
        public readonly uint? Color;
        public readonly int Stats;
        public readonly float Cooldown;
        public readonly bool RemoveSelf;
        public readonly string DungeonName;
        public readonly string LockedName;

        public ActivateEffect(XElement e)
        {
            Effect = (ActivateEffects)Enum.Parse(typeof(ActivateEffects), e.Value);

            if (e.HasAttribute("effect"))
                ConditionEffect = Utils.GetEffect(e.GetAttribute<string>("effect"));

            if (e.HasAttribute("condEffect"))
                ConditionEffect = Utils.GetEffect(e.GetAttribute<string>("condEffect"));

            if (e.HasAttribute("checkExistingEffect"))
                CheckExistingEffect = Utils.GetEffect(e.GetAttribute<string>("checkExistingEffect"));

            if (e.HasAttribute("color"))
            {
                Color = e.GetAttribute<uint>("color");
            }

            TotalDamage = e.GetAttribute<int>("totalDamage");
            Radius = e.GetAttribute<float>("radius");
            EffectDuration = e.GetAttribute<float>("condDuration");
            DurationSec = e.GetAttribute<float>("duration");
            DurationMS = (int)(DurationSec * 1000.0f);
            Amount = e.GetAttribute<int>("amount");
            Range = e.GetAttribute<float>("range");
            ObjectId = e.GetAttribute<string>("objectId");
            Id = e.GetAttribute<string>("id");
            MaximumDistance = e.GetAttribute<float>("maxDistance");
            MaxTargets = e.GetAttribute<int>("maxTargets");
            Stats = e.GetAttribute<int>("stat");
            Cooldown = e.GetAttribute<float>("cooldown");
            RemoveSelf = e.GetAttribute<bool>("removeSelf");
            DungeonName = e.GetAttribute<string>("dungeonName");
            LockedName = e.GetAttribute<string>("lockedName");
        }
    }

    public class TileDesc
    {
        public readonly ushort ObjectType;
        public readonly string ObjectId;
        public readonly bool NoWalk;
        public readonly bool Damaging;
        public readonly int MinDamage;
        public readonly int MaxDamage;
        public readonly float Speed;
        public readonly bool Push;
        public readonly float PushX;
        public readonly float PushY;
        public readonly bool Sink;
        public readonly bool Sinking;

        public TileDesc(ushort type, XElement e)
        {
            ObjectType = type;
            ObjectId = e.GetAttribute<string>("id");
            NoWalk = e.HasElement("NoWalk");

            if (e.HasElement("MinDamage"))
            {
                MinDamage = e.GetValue<int>("MinDamage");
                Damaging = true;
            }

            if (e.HasElement("MaxDamage"))
            {
                MaxDamage = e.GetValue<int>("MaxDamage");
                Damaging = true;
            }

            Sink = e.HasElement("Sink");
            Sinking = e.HasElement("Sinking");

            Speed = e.GetValue<float>("Speed", 1.0f);
            Push = e.HasElement("Push");
            if (Push)
            {
                var anim = e.Element("Animate");
                if (anim.HasAttribute("dx"))
                    PushX = anim.GetAttribute<float>("dx");
                if (anim.HasAttribute("dy"))
                    PushY = anim.GetAttribute<float>("dy");
            }
        }
    }
}
