using common.resources;
using NLog;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using static common.DbClassStats;

namespace common
{
    public class Database : IDisposable
    {
        static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private const int _lockTTL = 60;
        public readonly int DatabaseIndex;

        private readonly ConnectionMultiplexer _redis;
        private readonly IDatabase _db;
        private readonly IServer _server;

        private ISManager _isManager;
        private readonly Resources _resources;
        private readonly ServerConfig _config;

        public IDatabase Conn => _db;
        public readonly ISubscriber Sub;

        public Database(Resources resources, ServerConfig config)
        {
            Log.Info("Initializing Database...");
            _resources = resources;
            _config = config;

            DatabaseIndex = config.dbInfo.index;
            var conString = config.dbInfo.host + ":" + config.dbInfo.port + ",syncTimeout=60000";
            if (!string.IsNullOrWhiteSpace(config.dbInfo.auth))
                conString += ",password=" + config.dbInfo.auth;

            _redis = ConnectionMultiplexer.Connect(conString);
            _server = _redis.GetServer(_redis.GetEndPoints(true)[0]);
            _db = _redis.GetDatabase(DatabaseIndex);
            Sub = _redis.GetSubscriber();
        }

        public void SetISManager(ISManager isManager)
        {
            _isManager = isManager;
        }

        public void Dispose()
        {
            _redis.Dispose();
        }

        public static readonly string[] GuestNames =
        {
            "Darq", "Deyst", "Drac", "Drol",
            "Eango", "Eashy", "Eati", "Eendi", "Ehoni",
            "Gharr", "Iatho", "Iawa", "Idrae", "Iri", "Issz", "Itani",
            "Laen", "Lauk", "Lorz",
            "Oalei", "Odaru", "Oeti", "Orothi", "Oshyu",
            "Queq", "Radph", "Rayr", "Ril", "Rilr", "Risrr",
            "Saylt", "Scheev", "Sek", "Serl", "Seus",
            "Tal", "Tiar", "Uoro", "Urake", "Utanu",
            "Vorck", "Vorv", "Yangu", "Yimi", "Zhiar"
        };

        public DbAccount CreateGuestAccount(string uuid)
        {
            var newAccounts = _resources.Settings.NewAccounts;

            var acnt = new DbAccount(_db, 0)
            {
                UUID = uuid,
                Name = GuestNames[(uint)uuid.GetHashCode() % GuestNames.Length],
                Admin = false,
                NameChosen = false,
                FirstDeath = true,
                GuildId = 0,
                GuildRank = 0,
                VaultCount = newAccounts.VaultCount,
                MaxCharSlot = newAccounts.MaxCharSlot,
                RegTime = DateTime.Now,
                Guest = true,
                Fame = newAccounts.Fame,
                TotalFame = newAccounts.Fame,
                Credits = newAccounts.Credits,
                TotalCredits = newAccounts.Credits,
                PassResetToken = ""
            };

            // make sure guest have all classes if they are supposed to
            var stats = new DbClassStats(acnt);
            if (newAccounts.ClassesUnlocked)
            {
                foreach (var @class in _resources.GameData.Classes.Keys)
                    stats.Unlock(@class);
                stats.FlushAsync();
            }
            else
                _db.KeyDelete("classStats.0");

            // make sure guests have all skins if they are supposed to
            if (newAccounts.SkinsUnlocked)
            {
                acnt.Skins = (from skin in _resources.GameData.Skins.Values
                              select skin.Type).ToArray();
            }

            return acnt;
        }

        public LoginStatus Verify(string uuid, string password, out DbAccount acc)
        {
            acc = null;

            //check login
            var info = new DbLoginInfo(_db, uuid);
            if (info.IsNull)
                return LoginStatus.AccountNotExists;

            byte[] userPass = Utils.SHA1(password + info.Salt);
            if (Convert.ToBase64String(userPass) != info.HashedPassword)
                return LoginStatus.InvalidCredentials;

            acc = new DbAccount(_db, info.AccountId);

            // make sure account has all classes if they are supposed to
            var stats = new DbClassStats(acc);
            if (_resources.Settings.NewAccounts.ClassesUnlocked)
                foreach (var @class in _resources.GameData.Classes.Keys)
                    stats.Unlock(@class);
            stats.FlushAsync();

            // make sure account has all skins if they are supposed to
            if (_resources.Settings.NewAccounts.SkinsUnlocked)
            {
                acc.Skins = (from skin in _resources.GameData.Skins.Values
                             select skin.Type).ToArray();
            }

            return LoginStatus.OK;
        }

        // basic account locking functions
        public bool AcquireLock(DbAccount acc)
        {
            var tran = _db.CreateTransaction();

            var lockToken = Guid.NewGuid().ToString();

            var aKey = $"lock:{acc.AccountId}";
            tran.AddCondition(Condition.KeyNotExists(aKey));
            tran.StringSetAsync(aKey, lockToken, TimeSpan.FromSeconds(_lockTTL));
            var committed = tran.Execute();

            acc.LockToken = committed ? lockToken : null;
            return committed;
        }

        public TimeSpan? GetLockTime(DbAccount acc)
        {
            return _db.KeyTimeToLive($"lock:{acc.AccountId}");
        }

        public bool RenewLock(DbAccount acc)
        {
            var tran = _db.CreateTransaction();

            var aKey = $"lock:{acc.AccountId}";
            tran.AddCondition(Condition.StringEqual(aKey, acc.LockToken));
            tran.KeyExpireAsync(aKey, TimeSpan.FromSeconds(_lockTTL));
            return tran.Execute();
        }

        public void ReleaseLock(DbAccount acc)
        {
            var tran = _db.CreateTransaction();

            string aKey = $"lock:{acc.AccountId}";
            tran.AddCondition(Condition.StringEqual(aKey, acc.LockToken));
            tran.KeyDeleteAsync(aKey);

            tran.ExecuteAsync(CommandFlags.FireAndForget);
        }

        public bool AccountLockExists(int accId)
        {
            return _db.KeyExists($"lock:{accId}");
        }

        // abstracted account locking funcs
        private struct l : IDisposable
        {
            private Database db;
            private DbAccount acc;
            internal bool lockOk;

            public l(Database db, DbAccount acc)
            {
                this.db = db;
                this.acc = acc;
                lockOk = db.AcquireLock(acc);
            }

            public void Dispose()
            {
                if (lockOk)
                    db.ReleaseLock(acc);
            }
        }

        public IDisposable Lock(DbAccount acc)
        {
            return new l(this, acc);
        }

        public bool LockOk(IDisposable l)
        {
            return ((l)l).lockOk;
        }

        public const string REG_LOCK = "regLock";
        public const string NAME_LOCK = "nameLock";

        public string AcquireLock(string key)
        {
            string lockToken = Guid.NewGuid().ToString();

            var tran = _db.CreateTransaction();
            tran.AddCondition(Condition.KeyNotExists(key));
            tran.StringSetAsync(key, lockToken, TimeSpan.FromSeconds(_lockTTL));

            return tran.Execute() ? lockToken : null;
        }

        public void ReleaseLock(string key, string token)
        {
            var tran = _db.CreateTransaction();
            tran.AddCondition(Condition.StringEqual(key, token));
            tran.KeyDeleteAsync(key);
            tran.Execute();
        }

        public bool RenameUUID(DbAccount acc, string newUuid, string lockToken)
        {
            string p = _db.HashGet("logins", acc.UUID.ToUpperInvariant());
            var trans = _db.CreateTransaction();
            trans.AddCondition(Condition.StringEqual(REG_LOCK, lockToken));
            trans.AddCondition(Condition.HashNotExists("logins", newUuid.ToUpperInvariant()));
            trans.HashDeleteAsync("logins", acc.UUID.ToUpperInvariant());
            trans.HashSetAsync("logins", newUuid.ToUpperInvariant(), p);
            if (!trans.Execute()) return false;

            acc.UUID = newUuid;
            acc.FlushAsync();
            return true;
        }

        public bool RenameIGN(DbAccount acc, string newName, string lockToken)
        {
            var trans = _db.CreateTransaction();
            trans.AddCondition(Condition.StringEqual(NAME_LOCK, lockToken));
            trans.HashDeleteAsync("names", acc.Name.ToUpperInvariant());
            trans.HashSetAsync("names", newName.ToUpperInvariant(), acc.AccountId.ToString());
            if (!trans.Execute()) return false;

            acc.Name = newName;
            acc.NameChosen = true;
            acc.FlushAsync();
            return true;
        }

        public bool UnnameIGN(DbAccount acc, string lockToken)
        {
            var trans = _db.CreateTransaction();
            trans.AddCondition(Condition.StringEqual(NAME_LOCK, lockToken));
            trans.HashDeleteAsync("names", acc.Name.ToUpperInvariant());
            if (!trans.Execute()) return false;

            acc.Name = GuestNames[acc.AccountId % GuestNames.Length];
            acc.NameChosen = false;
            acc.FlushAsync();
            return true;
        }

        private static RandomNumberGenerator gen = RNGCryptoServiceProvider.Create();

        public void ChangePassword(string uuid, string password)
        {
            DbLoginInfo login = new DbLoginInfo(_db, uuid);

            byte[] x = new byte[0x10];
            gen.GetNonZeroBytes(x);
            string salt = Convert.ToBase64String(x);
            string hash = Convert.ToBase64String(Utils.SHA1(password + salt));

            login.HashedPassword = hash;
            login.Salt = salt;
            login.Flush();
        }

        public void Guest(DbAccount acc, bool isGuest)
        {
            acc.Guest = isGuest;
            acc.FlushAsync();
        }

        public RegisterStatus Register(string uuid, string password, bool isGuest, out DbAccount acc)
        {
            var newAccounts = _resources.Settings.NewAccounts;

            acc = null;
            if (!_db.HashSet("logins", uuid.ToUpperInvariant(), "{}", When.NotExists))
                return RegisterStatus.UsedName;

            int newAccId = (int)_db.StringIncrement("nextAccId");

            acc = new DbAccount(_db, newAccId)
            {
                UUID = uuid,
                Name = GuestNames[(uint)uuid.GetHashCode() % GuestNames.Length],
                Admin = false,
                NameChosen = false,
                FirstDeath = true,
                GuildId = 0,
                GuildRank = 0,
                VaultCount = newAccounts.VaultCount,
                MaxCharSlot = newAccounts.MaxCharSlot,
                RegTime = DateTime.Now,
                Guest = isGuest,
                Fame = newAccounts.Fame,
                TotalFame = newAccounts.Fame,
                Credits = newAccounts.Credits,
                TotalCredits = newAccounts.Credits,
                PassResetToken = "",
                LastSeen = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds
            };

            if (newAccounts.SkinsUnlocked)
            {
                acc.Skins = (from skin in _resources.GameData.Skins.Values
                             select skin.Type).ToArray();
            }

            acc.FlushAsync();

            DbLoginInfo login = new DbLoginInfo(_db, uuid);

            byte[] x = new byte[0x10];
            gen.GetNonZeroBytes(x);
            string salt = Convert.ToBase64String(x);
            string hash = Convert.ToBase64String(Utils.SHA1(password + salt));

            login.HashedPassword = hash;
            login.Salt = salt;
            login.AccountId = acc.AccountId;
            login.Flush();

            DbClassStats stats = new DbClassStats(acc);
            if (newAccounts.ClassesUnlocked)
                foreach (var @class in _resources.GameData.Classes.Keys)
                    stats.Unlock(@class);
            stats.FlushAsync();

            return RegisterStatus.OK;
        }

        public bool HasUUID(string uuid)
        {
            return _db.HashExists("logins", uuid.ToUpperInvariant());
        }

        public DbAccount GetAccount(int id, string field = null)
        {
            var ret = new DbAccount(_db, id, field);
            if (ret.IsNull) return null;
            return ret;
        }

        public DbAccount GetAccount(string uuid)
        {
            DbLoginInfo info = new DbLoginInfo(_db, uuid);
            if (info.IsNull)
                return null;
            DbAccount ret = new DbAccount(_db, info.AccountId);
            if (ret.IsNull)
                return null;
            return ret;
        }

        public void LockAccount(DbAccount target, DbAccount acc, bool add)
        {
            var lockList = target.LockList.ToList();
            if (lockList.Contains(acc.AccountId) && add)
                return;

            if (add)
                lockList.Add(acc.AccountId);
            else
                lockList.Remove(acc.AccountId);

            target.LockList = lockList.ToArray();
            target.FlushAsync();
        }

        public void IgnoreAccount(DbAccount target, DbAccount acc, bool add)
        {
            var ignoreList = target.IgnoreList.ToList();
            if (ignoreList.Contains(acc.AccountId) && add)
                return;

            if (add)
                ignoreList.Add(acc.AccountId);
            else
                ignoreList.Remove(acc.AccountId);

            target.IgnoreList = ignoreList.ToArray();
            target.FlushAsync();
        }

        public void ReloadAccount(DbAccount acc)
        {
            acc.FlushAsync();
            acc.Reload();
        }

        public GuildCreateStatus CreateGuild(string guildName, out DbGuild guild)
        {
            guild = null;

            if (String.IsNullOrWhiteSpace(guildName))
                return GuildCreateStatus.InvalidName;

            // remove excessive whitespace
            var rgx = new Regex(@"\s+");
            guildName = rgx.Replace(guildName, " ");
            guildName = guildName.Trim();

            // check if valid
            rgx = new Regex(@"^[A-Za-z\s]{1,20}$");
            if (!rgx.IsMatch(guildName))
                return GuildCreateStatus.InvalidName;

            // add guild to guild list
            var newGuildId = (int)_db.StringIncrement("nextGuildId");
            if (!_db.HashSet("guilds", guildName.ToUpperInvariant(), newGuildId, When.NotExists))
                return GuildCreateStatus.UsedName;

            // create guild data structure
            guild = new DbGuild(_db, newGuildId)
            {
                Name = guildName,
                Level = 0,
                Fame = 0,
                TotalFame = 0
            };

            // save
            guild.FlushAsync();

            return GuildCreateStatus.OK;
        }

        public DbGuild GetGuild(int id)
        {
            var ret = new DbGuild(_db, id);
            if (ret.IsNull) return null;
            return ret;
        }

        public AddGuildMemberStatus AddGuildMember(DbGuild guild, DbAccount acc, bool founder = false)
        {
            if (acc == null)
                return AddGuildMemberStatus.Error;

            if (acc.NameChosen == false)
                return AddGuildMemberStatus.NameNotChosen;

            if (acc.GuildId == guild.Id)
                return AddGuildMemberStatus.AlreadyInGuild;

            if (acc.GuildId > 0)
                return AddGuildMemberStatus.InAnotherGuild;

            using (TimedLock.Lock(guild.MemberLock))
            {
                int guildSize = 50;
                // probably not best to lock this up but should be ok
                if (guild.Members.Length >= guildSize)
                    return AddGuildMemberStatus.GuildFull;

                var members = guild.Members.ToList();
                if (members.Contains(acc.AccountId))
                    return AddGuildMemberStatus.IsAMember; // this should not happen...
                members.Add(acc.AccountId);
                guild.Members = members.ToArray();
                guild.FlushAsync();
            }

            // set account guild info
            acc.GuildId = guild.Id;
            acc.GuildRank = (founder) ? 40 : 0;
            acc.FlushAsync();

            return AddGuildMemberStatus.OK;
        }

        public bool RemoveFromGuild(DbAccount acc)
        {
            var guild = GetGuild(acc.GuildId);

            if (guild == null)
                return false;

            List<int> members;
            using (TimedLock.Lock(guild.MemberLock))
            {
                members = guild.Members.ToList();
                if (members.Contains(acc.AccountId))
                {
                    members.Remove(acc.AccountId);
                    guild.Members = members.ToArray();
                    guild.FlushAsync();
                }
            }

            // remove guild name from guilds if there are no members
            if (members.Count <= 0)
                _db.HashDeleteAsync("guilds", guild.Name.ToUpperInvariant(), CommandFlags.FireAndForget);

            acc.GuildId = 0;
            acc.GuildRank = 0;
            acc.GuildFame = 0;
            acc.FlushAsync();
            return true;
        }

        public bool ChangeGuildRank(DbAccount acc, int rank)
        {
            if (acc.GuildId <= 0 || !(new Int16[] { 0, 10, 20, 30, 40 }).Any(r => r == rank))
                return false;

            acc.GuildRank = rank;
            acc.FlushAsync();
            return true;
        }

        public bool SetGuildBoard(DbGuild guild, string text)
        {
            if (guild.IsNull)
                return false;

            guild.Board = text;
            guild.FlushAsync();
            return true;
        }

        public bool ChangeGuildLevel(DbGuild guild, int level)
        {
            // supported guild levels
            if (level != 1 &&
                level != 2 &&
                level != 3)
                return false;

            guild.Level = level;
            guild.FlushAsync();
            return true;
        }

        public int ResolveId(string ign)
        {
            string val = (string)_db.HashGet("names", ign.ToUpperInvariant());
            if (val == null)
                return 0;
            return int.Parse(val);
        }

        public string ResolveIgn(int accId)
        {
            return _db.HashGet("account." + accId, "name");
        }

        public void UnlockClass(DbAccount acc, ushort type)
        {
            var cs = ReadClassStats(acc);
            cs.Unlock(type);
            cs.FlushAsync();
        }

        public void PurchaseSkin(DbAccount acc, ushort skinType, int cost)
        {
            if (cost > 0)
                acc.TotalCredits = (int)_db.HashIncrement(acc.Key, "totalCredits", cost);
            acc.Credits = (int)_db.HashIncrement(acc.Key, "credits", cost);

            // not thread safe
            var ownedSkins = acc.Skins.ToList();
            ownedSkins.Add(skinType);
            acc.Skins = ownedSkins.ToArray();

            acc.FlushAsync();
        }

        private static readonly Dictionary<CurrencyType, string[]> CurrencyKey = new Dictionary<CurrencyType, string[]>
        {
            { CurrencyType.Gold, new [] { "totalCredits", "credits" } },
            { CurrencyType.Fame, new [] { "totalFame", "fame" } },
            { CurrencyType.GuildFame, new [] { "totalFame", "fame" } }
        };

        public void UpdateCurrency(int accountId, int amount, CurrencyType currency, ITransaction transaction = null)
        {
            var trans = transaction ?? _db.CreateTransaction();

            string key = $"account.{accountId}";
            string[] fields = CurrencyKey[currency];

            if (currency == CurrencyType.GuildFame)
            {
                var guildId = (int)_db.HashGet(key, "guildId");
                if (guildId <= 0)
                    return;
                key = $"guild.{guildId}";
            }

            if (amount > 0)
                trans.HashIncrementAsync(key, fields[0], amount);
            trans.HashIncrementAsync(key, fields[1], amount);

            if (transaction == null)
                trans.Execute();
        }

        public Task UpdateCurrency(DbAccount acc, int amount, CurrencyType currency, ITransaction transaction = null)
        {
            var trans = transaction ?? _db.CreateTransaction();

            string key = acc.Key;
            string[] fields = CurrencyKey[currency];

            if (currency == CurrencyType.GuildFame)
            {
                var guildId = (int)_db.HashGet(key, "guildId");
                if (guildId <= 0)
                    return Task.FromResult(false);
                key = $"guild.{guildId}";
            }

            // validate acc value before setting - TODO check guild fame
            var currentAmount = GetCurrencyAmount(acc, currency);
            if (currentAmount != null)
                trans.AddCondition(Condition.HashEqual(acc.Key, fields[1], currentAmount.Value));

            if (amount > 0)
                trans.HashIncrementAsync(key, fields[0], amount)
                    .ContinueWith(t =>
                    {
                        if (!t.IsCanceled)
                            UpdateAccountCurrency(acc, currency, (int)t.Result, true);
                    });
            var task = trans.HashIncrementAsync(key, fields[1], amount)
                .ContinueWith(t =>
                {
                    if (!t.IsCanceled)
                        UpdateAccountCurrency(acc, currency, (int)t.Result);
                });

            if (transaction == null)
                trans.Execute();

            return task;
        }

        private int? GetCurrencyAmount(DbAccount acc, CurrencyType currency)
        {
            switch (currency)
            {
                case CurrencyType.Gold:
                    return acc.Credits;
                case CurrencyType.Fame:
                    return acc.Fame;
                default:
                    return null;
            }
        }

        private void UpdateAccountCurrency(DbAccount acc, CurrencyType type, int value, bool total = false)
        {
            switch (type)
            {
                case CurrencyType.Gold:
                    if (total)
                        acc.TotalCredits = value;
                    else
                        acc.Credits = value;
                    break;

                case CurrencyType.Fame:
                    if (total)
                        acc.TotalFame = value;
                    else
                        acc.Fame = value;
                    break;
            }
        }

        public Task UpdateCredit(DbAccount acc, int amount, ITransaction transaction = null)
        {
            var trans = transaction ?? _db.CreateTransaction();

            if (amount > 0)
                trans.HashIncrementAsync(acc.Key, "totalCredits", amount)
                    .ContinueWith(t =>
                    {
                        if (!t.IsCanceled)
                            acc.TotalCredits = (int)t.Result;
                    });

            var task = trans.HashIncrementAsync(acc.Key, "credits", amount)
                .ContinueWith(t =>
                {
                    if (!t.IsCanceled)
                        acc.Credits = (int)t.Result;
                });

            if (transaction == null)
                trans.Execute();

            return task;
        }

        public Task UpdateFame(DbAccount acc, int amount, ITransaction transaction = null)
        {
            var trans = transaction ?? _db.CreateTransaction();

            if (amount > 0)
                trans.HashIncrementAsync(acc.Key, "totalFame", amount)
                    .ContinueWith(t =>
                    {
                        if (!t.IsCanceled)
                            acc.TotalFame = (int)t.Result;
                    });

            var task = trans.HashIncrementAsync(acc.Key, "fame", amount)
                .ContinueWith(t =>
                {
                    if (!t.IsCanceled)
                        acc.Fame = (int)t.Result;
                });

            if (transaction == null)
                trans.Execute();

            return task;
        }

        public Task UpdateGuildFame(DbGuild guild, int amount, ITransaction transaction = null)
        {
            var guildKey = $"guild.{guild.Id}";

            var trans = transaction ?? _db.CreateTransaction();

            if (amount > 0)
                trans.HashIncrementAsync(guildKey, "totalFame", amount)
                    .ContinueWith(t =>
                    {
                        if (!t.IsCanceled)
                            guild.TotalFame = (int)t.Result;
                    });

            var task = trans.HashIncrementAsync(guildKey, "fame", amount)
                .ContinueWith(t =>
                {
                    if (!t.IsCanceled)
                        guild.Fame = (int)t.Result;
                });

            if (transaction == null)
                trans.Execute();

            return task;
        }

        private void UpdatePlayerGuildFame(DbAccount acc, int amount)
        {
            acc.GuildFame = (int)_db.HashIncrement(acc.Key, "guildFame", amount);
        }

        public DbClassStats ReadClassStats(DbAccount acc)
        {
            return new DbClassStats(acc);
        }

        public DbVault ReadVault(DbAccount acc)
        {
            return new DbVault(acc);
        }

        public int? CreateChest(DbAccount acc, ITransaction tran = null)
        {
            if (tran == null)
                return (int)_db.HashIncrement(acc.Key, "vaultCount");
            tran.HashIncrementAsync(acc.Key, "vaultCount");
            return null;
        }

        public int CreateGiftChest(DbAccount acc)
        {
            var id = (int)_db.HashIncrement(acc.Key, "giftCount");
            return id;
        }

        public IEnumerable<int> GetAliveCharacters(DbAccount acc)
        {
            foreach (var i in _db.SetMembers("alive." + acc.AccountId))
                yield return BitConverter.ToInt32(i, 0);
        }

        public IEnumerable<int> GetDeadCharacters(DbAccount acc)
        {
            foreach (var i in _db.ListRange("dead." + acc.AccountId, 0, int.MaxValue))
                yield return BitConverter.ToInt32(i, 0);
        }

        public bool IsAlive(DbChar character)
        {
            return _db.SetContains("alive." + character.Account.AccountId,
                BitConverter.GetBytes(character.CharId));
        }

        private ushort[] InitInventory(ushort[] givenItems)
        {
            var inv = Utils.ResizeArray(givenItems, 20);
            for (var i = givenItems.Length; i < inv.Length; i++)
                inv[i] = 0xffff;

            return inv;
        }

        public CreateStatus CreateCharacter(DbAccount acc, ushort type, ushort skinType, out DbChar character)
        {
            if (_db.SetLength("alive." + acc.AccountId) >= acc.MaxCharSlot)
            {
                character = null;
                return CreateStatus.ReachCharLimit;
            }

            // check skin requirements
            if (skinType != 0)
            {
                var skinDesc = _resources.GameData.Skins[skinType];
                if (!acc.Skins.Contains(skinType) ||
                    skinDesc.PlayerClassType != type)
                {
                    character = null;
                    return CreateStatus.SkinUnavailable;
                }
            }

            var objDesc = _resources.GameData.ObjectDescs[type];
            var playerDesc = _resources.GameData.Classes[type];
            var classStats = ReadClassStats(acc);
            var unlockClass = (playerDesc.Unlock != null)
                ? playerDesc.Unlock.Type
                : null;

            // check to see if account has unlocked via gold
            if (classStats.AllKeys
                .All(x => (ushort)(int)x != type))
            {
                // check to see if account meets unlock requirements
                if ((unlockClass != null && classStats[(ushort)unlockClass].BestLevel < playerDesc.Unlock.Level))
                {
                    character = null;
                    return CreateStatus.Locked;
                }
            }

            var newId = (int)_db.HashIncrement(acc.Key, "nextCharId");

            var newCharacters = _resources.Settings.NewCharacters;
            character = new DbChar(acc, newId)
            {
                ObjectType = type,
                Level = newCharacters.Level,
                Experience = 0,
                Fame = 0,
                Items = InitInventory(playerDesc.Equipment),
                Stats = new int[]
                {
                    playerDesc.Stats[0].StartingValue,
                    playerDesc.Stats[1].StartingValue,
                    playerDesc.Stats[2].StartingValue,
                    playerDesc.Stats[3].StartingValue,
                    playerDesc.Stats[4].StartingValue,
                    playerDesc.Stats[5].StartingValue,
                    playerDesc.Stats[6].StartingValue,
                    playerDesc.Stats[7].StartingValue,
                },
                HP = playerDesc.Stats[0].StartingValue,
                MP = playerDesc.Stats[1].StartingValue,
                Tex1 = 0,
                Tex2 = 0,
                Skin = skinType,
                PetId = 0,
                FameStats = new byte[0],
                CreateTime = DateTime.Now,
                LastSeen = DateTime.Now
            };

            if (newCharacters.Maxed)
            {
                character.Stats = new int[]
                {
                    playerDesc.Stats[0].MaxValue,
                    playerDesc.Stats[1].MaxValue,
                    playerDesc.Stats[2].MaxValue,
                    playerDesc.Stats[3].MaxValue,
                    playerDesc.Stats[4].MaxValue,
                    playerDesc.Stats[5].MaxValue,
                    playerDesc.Stats[6].MaxValue,
                    playerDesc.Stats[7].MaxValue,
                };
                character.HP = character.Stats[0];
                character.MP = character.Stats[1];
            }

            character.FlushAsync();
            _db.SetAdd("alive." + acc.AccountId, BitConverter.GetBytes(newId));
            return CreateStatus.OK;
        }

        public DbChar LoadCharacter(DbAccount acc, int charId)
        {
            DbChar ret = new DbChar(acc, charId);
            if (ret.IsNull) return null;
            else return ret;
        }

        public DbChar LoadCharacter(int accId, int charId)
        {
            DbAccount acc = new DbAccount(_db, accId);
            if (acc.IsNull) return null;
            DbChar ret = new DbChar(acc, charId);
            if (ret.IsNull) return null;
            else return ret;
        }

        public Task<bool> SaveCharacter(
            DbAccount acc, DbChar character, DbClassStats stats, bool lockAcc)
        {
            var trans = _db.CreateTransaction();
            if (lockAcc)
                trans.AddCondition(Condition.StringEqual(
                    $"lock:{acc.AccountId}", acc.LockToken));
            character.FlushAsync(trans);
            stats.Update(character);
            stats.FlushAsync(trans);
            return trans.ExecuteAsync();
        }

        public void DeleteCharacter(DbAccount acc, int charId)
        {
            _db.KeyDeleteAsync("char." + acc.AccountId + "." + charId);
            var buff = BitConverter.GetBytes(charId);
            _db.SetRemoveAsync("alive." + acc.AccountId, buff);
            _db.ListRemoveAsync("dead." + acc.AccountId, buff);
        }

        public void Death(XmlData dat, DbAccount acc, DbChar character, FameStats stats, string killer)
        {
            character.Dead = true;
            var classStats = new DbClassStats(acc);

            // calculate total fame given bonuses
            bool firstBorn;
            var finalFame = stats.CalculateTotal(dat, character,
                classStats, out firstBorn);

            // save character
            character.FinalFame = finalFame;
            SaveCharacter(acc, character, classStats, acc.LockToken != null);

            var death = new DbDeath(acc, character.CharId)
            {
                ObjectType = character.ObjectType,
                Level = character.Level,
                TotalFame = finalFame,
                Killer = killer,
                FirstBorn = firstBorn,
                DeathTime = DateTime.UtcNow
            };
            death.FlushAsync();

            var idBuff = BitConverter.GetBytes(character.CharId);
            _db.SetRemoveAsync(
                "alive." + acc.AccountId, idBuff, CommandFlags.FireAndForget);
            _db.ListLeftPushAsync(
                "dead." + acc.AccountId, idBuff, When.Always, CommandFlags.FireAndForget);

            UpdateFame(acc, finalFame);

            var guild = new DbGuild(acc);
            if (!guild.IsNull)
            {
                UpdateGuildFame(guild, finalFame);
                UpdatePlayerGuildFame(acc, finalFame);
            }

            if (!acc.Admin)
            {
                DbLegend.Insert(_db, acc.AccountId, character.CharId, finalFame);
            }
        }

        public void LogAccountByIp(string ip, int accountId)
        {
            var abi = new DbIpInfo(_db, ip);

            if (!abi.IsNull)
                abi.Accounts.Add(accountId);
            else
                abi.Accounts = new HashSet<int> { accountId };

            abi.Flush();
        }

        public void Mute(string ip, TimeSpan? timeSpan = null)
        {
            _db.StringSetAsync($"mutes:{ip}", "", timeSpan);
        }

        public Task<bool> IsMuted(string ip)
        {
            return _db.KeyExistsAsync($"mutes:{ip}");
        }

        public void Ban(int accId, string reason = "", int liftTime = -1)
        {
            var acc = new DbAccount(_db, accId)
            {
                Banned = true,
                Notes = reason,
                BanLiftTime = liftTime
            };
            acc.FlushAsync();
        }

        public bool UnBan(int accId)
        {
            var acc = new DbAccount(_db, accId);
            if (acc.Banned)
            {
                acc.Banned = false;
                acc.FlushAsync();
                return true;
            }

            return false;
        }

        public void BanIp(string ip, string notes = "")
        {
            var abi = new DbIpInfo(_db, ip)
            {
                Banned = true,
                Notes = notes
            };
            abi.Flush();
        }

        public bool UnBanIp(string ip)
        {
            var abi = new DbIpInfo(_db, ip);
            if (!abi.IsNull && abi.Banned)
            {
                abi.Banned = false;
                abi.Flush();
                return true;
            }
            return false;
        }

        public bool IsIpBanned(string ip)
        {
            var abi = new DbIpInfo(_db, ip);
            return abi.Banned;
        }

        public int LastLegendsUpdateTime()
        {
            var time = _db.StringGet("legends:updateTime");
            if (time.IsNullOrEmpty)
                return -1;
            return int.Parse(time);
        }

        public DbChar[] GetLegendsBoard(string timeSpan)
        {
            return DbLegend
                .Get(_db, timeSpan)
                .Select(e => LoadCharacter(e.AccId, e.ChrId))
                .Where(e => e != null)
                .ToArray();
        }

        public void CleanLegends()
        {
            DbLegend.Clean(_db);
        }

        public Task<bool> IsLegend(int accId)
        {
            return _db.HashExistsAsync("legend", accId);
        }
    }
}
