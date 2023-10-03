using System.Text;
using Newtonsoft.Json;
using NLog;
using StackExchange.Redis;

namespace Shared;

public abstract class RedisObject {
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    //Note do not modify returning buffer
    private Dictionary<RedisValue, KeyValuePair<byte[], bool>> _entries;

    private List<HashEntry> _update;

    public IDatabase Database { get; private set; }
    public string Key { get; private set; }

    public IEnumerable<RedisValue> AllKeys => _entries.Keys;

    public bool IsNull => _entries.Count == 0;

    protected void Init(IDatabase db, string key, string field = null) {
        Key = key;
        Database = db;

        if (field == null) {
            _entries = db.HashGetAll(key)
                .ToDictionary(
                    x => x.Name,
                    x => new KeyValuePair<byte[], bool>(x.Value, false));
        }
        else {
            var entry = new HashEntry[] {new(field, db.HashGet(key, field))};
            _entries = entry.ToDictionary(x => x.Name,
                x => new KeyValuePair<byte[], bool>(x.Value, false));
        }
    }

    protected byte[] GetValueRaw(RedisValue key) {
        if (!_entries.TryGetValue(key, out var val))
            return null;

        if (val.Key == null)
            return null;

        return (byte[]) val.Key.Clone();
    }

    protected T GetValue<T>(RedisValue key, T def = default) {
        if (!_entries.TryGetValue(key, out var val) || val.Key == null)
            return def;

        if (typeof(T) == typeof(int))
            return (T) (object) int.Parse(Encoding.UTF8.GetString(val.Key));

        if (typeof(T) == typeof(uint))
            return (T) (object) uint.Parse(Encoding.UTF8.GetString(val.Key));

        if (typeof(T) == typeof(ushort))
            return (T) (object) ushort.Parse(Encoding.UTF8.GetString(val.Key));

        if (typeof(T) == typeof(bool))
            return (T) (object) (val.Key[0] != 0);

        if (typeof(T) == typeof(DateTime))
            return (T) (object) DateTime.FromBinary(BitConverter.ToInt64(val.Key, 0));

        if (typeof(T) == typeof(byte[]))
            return (T) (object) val.Key;

        if (typeof(T) == typeof(ushort[])) {
            var ret = new ushort[val.Key.Length / 2];
            Buffer.BlockCopy(val.Key, 0, ret, 0, val.Key.Length);
            return (T) (object) ret;
        }

        if (typeof(T) == typeof(int[]) ||
            typeof(T) == typeof(uint[])) {
            var ret = new int[val.Key.Length / 4];
            Buffer.BlockCopy(val.Key, 0, ret, 0, val.Key.Length);
            return (T) (object) ret;
        }

        if (typeof(T) == typeof(string))
            return (T) (object) Encoding.UTF8.GetString(val.Key);

        throw new NotSupportedException();
    }

    protected void SetValue<T>(RedisValue key, T val) {
        byte[] buff;
        if (typeof(T) == typeof(int) || typeof(T) == typeof(uint) ||
            typeof(T) == typeof(ushort) || typeof(T) == typeof(string)) {
            buff = Encoding.UTF8.GetBytes(val.ToString());
        }

        else if (typeof(T) == typeof(bool)) {
            buff = new[] {(byte) ((bool) (object) val ? 1 : 0)};
        }

        else if (typeof(T) == typeof(DateTime)) {
            buff = BitConverter.GetBytes(((DateTime) (object) val).ToBinary());
        }

        else if (typeof(T) == typeof(byte[])) {
            buff = (byte[]) (object) val;
        }

        else if (typeof(T) == typeof(ushort[])) {
            var v = (ushort[]) (object) val;
            buff = new byte[v.Length * 2];
            Buffer.BlockCopy(v, 0, buff, 0, buff.Length);
        }

        else if (typeof(T) == typeof(int[]) ||
                 typeof(T) == typeof(uint[])) {
            var v = (int[]) (object) val;
            buff = new byte[v.Length * 4];
            Buffer.BlockCopy(v, 0, buff, 0, buff.Length);
        }

        else {
            throw new NotSupportedException();
        }

        if (!_entries.ContainsKey(Key) || _entries[Key].Key == null || !buff.SequenceEqual(_entries[Key].Key))
            _entries[key] = new KeyValuePair<byte[], bool>(buff, true);
    }

    public Task FlushAsync(ITransaction transaction = null) {
        ReadyFlush();
        return transaction == null
            ? Database.HashSetAsync(Key, _update.ToArray())
            : transaction.HashSetAsync(Key, _update.ToArray());
    }

    private void ReadyFlush() {
        if (_update == null)
            _update = new List<HashEntry>();
        _update.Clear();

        foreach (var name in _entries.Keys)
            if (_entries[name].Value)
                _update.Add(new HashEntry(name, _entries[name].Key));

        foreach (var update in _update)
            _entries[update.Name] = new KeyValuePair<byte[], bool>(_entries[update.Name].Key, false);
    }

    public async Task ReloadAsync(ITransaction trans = null, string field = null) {
        if (field != null && _entries != null) {
            var tf = trans != null ? trans.HashGetAsync(Key, field) : Database.HashGetAsync(Key, field);

            try {
                await tf;
                _entries[field] = new KeyValuePair<byte[], bool>(
                    tf.Result, false);
            }
            catch { }

            return;
        }

        var t = trans != null ? trans.HashGetAllAsync(Key) : Database.HashGetAllAsync(Key);

        try {
            await t;
            _entries = t.Result.ToDictionary(
                x => x.Name, x => new KeyValuePair<byte[], bool>(x.Value, false));
        }
        catch { }
    }

    public void Reload(string field = null) {
        if (field != null && _entries != null) {
            _entries[field] = new KeyValuePair<byte[], bool>(
                Database.HashGet(Key, field), false);
            return;
        }

        _entries = Database.HashGetAll(Key)
            .ToDictionary(
                x => x.Name,
                x => new KeyValuePair<byte[], bool>(x.Value, false));
    }
}

public class DbLoginInfo {
    private IDatabase db;

    internal DbLoginInfo(IDatabase db, string uuid) {
        this.db = db;
        UUID = uuid;
        var json = (string) db.HashGet("logins", uuid.ToUpperInvariant());
        if (json == null)
            IsNull = true;
        else
            JsonConvert.PopulateObject(json, this);
    }

    [JsonIgnore] public string UUID { get; }

    [JsonIgnore] public bool IsNull { get; private set; }

    public string Salt { get; set; }
    public string HashedPassword { get; set; }
    public int AccountId { get; set; }

    public void Flush() {
        db.HashSet("logins", UUID.ToUpperInvariant(), JsonConvert.SerializeObject(this));
    }
}

public class DbAccount : RedisObject {
    public DbAccount(IDatabase db, int accId, string field = null) {
        AccountId = accId;
        Init(db, "account." + accId, field);

        if (field != null)
            return;

        var time = Utils.FromUnixTimestamp(BanLiftTime);
        if (!Banned || BanLiftTime <= -1 || time > DateTime.UtcNow) return;
        Banned = false;
        BanLiftTime = 0;
        FlushAsync();
    }

    public int AccountId { get; }

    internal string LockToken { get; set; }

    public string UUID {
        get => GetValue<string>("uuid");
        set => SetValue("uuid", value);
    }

    public string Name {
        get => GetValue<string>("name");
        set => SetValue("name", value);
    }

    public bool Admin {
        get => GetValue<bool>("admin");
        set => SetValue("admin", value);
    }

    public bool NameChosen {
        get => GetValue<bool>("nameChosen");
        set => SetValue("nameChosen", value);
    }

    public bool FirstDeath {
        get => GetValue<bool>("firstDeath");
        set => SetValue("firstDeath", value);
    }

    public int GuildId {
        get => GetValue<int>("guildId");
        set => SetValue("guildId", value);
    }

    public int GuildRank {
        get => GetValue<int>("guildRank");
        set => SetValue("guildRank", value);
    }

    public int GuildFame {
        get => GetValue<int>("guildFame");
        set => SetValue("guildFame", value);
    }

    public int VaultCount {
        get => GetValue<int>("vaultCount");
        set => SetValue("vaultCount", value);
    }

    public int MaxCharSlot {
        get => GetValue<int>("maxCharSlot");
        set => SetValue("maxCharSlot", value);
    }

    public bool Guest {
        get => GetValue<bool>("guest");
        set => SetValue("guest", value);
    }

    public int Credits {
        get => GetValue<int>("credits");
        set => SetValue("credits", value);
    }

    public int TotalCredits {
        get => GetValue<int>("totalCredits");
        set => SetValue("totalCredits", value);
    }

    public int Fame {
        get => GetValue<int>("fame");
        set => SetValue("fame", value);
    }

    public int TotalFame {
        get => GetValue<int>("totalFame");
        set => SetValue("totalFame", value);
    }

    public int NextCharId {
        get => GetValue<int>("nextCharId");
        set => SetValue("nextCharId", value);
    }

    public ushort[] Skins {
        get => GetValue<ushort[]>("skins") ?? new ushort[0];
        set => SetValue("skins", value);
    }

    public int[] LockList {
        get => GetValue<int[]>("lockList") ?? new int[0];
        set => SetValue("lockList", value);
    }

    public int[] IgnoreList {
        get => GetValue<int[]>("ignoreList") ?? new int[0];
        set => SetValue("ignoreList", value);
    }

    public bool Banned {
        get => GetValue<bool>("banned");
        set => SetValue("banned", value);
    }

    public int BanLiftTime {
        get => GetValue<int>("banLiftTime");
        set => SetValue("banLiftTime", value);
    }

    public string IP {
        get => GetValue<string>("ip");
        set => SetValue("ip", value);
    }

    public string Notes {
        get => GetValue<string>("notes");
        set => SetValue("notes", value);
    }

    public string PassResetToken {
        get => GetValue<string>("passResetToken");
        set => SetValue("passResetToken", value);
    }

    public DateTime RegTime {
        get => GetValue<DateTime>("regTime");
        set => SetValue("regTime", value);
    }

    public int LastSeen {
        get => GetValue<int>("lastSeen");
        set => SetValue("lastSeen", value);
    }

    public void RefreshLastSeen() {
        LastSeen = (int) DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
    }
}

public struct DbClassStatsEntry {
    public int BestLevel;
    public int BestFame;
}

public class DbClassStats : RedisObject {
    public DbClassStats(DbAccount acc, ushort? type = null) {
        Account = acc;
        Init(acc.Database, "classStats." + acc.AccountId, type?.ToString());
    }

    public DbAccount Account { get; private set; }

    public DbClassStatsEntry this[ushort type] {
        get {
            var v = GetValue<string>(type.ToString());
            if (v != null) return JsonConvert.DeserializeObject<DbClassStatsEntry>(v);
            return default;
        }
        set => SetValue(type.ToString(), JsonConvert.SerializeObject(value));
    }

    public void Unlock(ushort type) {
        var field = type.ToString();
        var json = GetValue<string>(field);
        if (json == null)
            SetValue(field, JsonConvert.SerializeObject(new DbClassStatsEntry {
                BestLevel = 0,
                BestFame = 0
            }));
    }

    public void Update(DbChar character) {
        var field = character.ObjectType.ToString();
        var finalFame = Math.Max(character.Fame, character.FinalFame);
        var json = GetValue<string>(field);
        if (json == null) {
            SetValue(field, JsonConvert.SerializeObject(new DbClassStatsEntry {
                BestLevel = character.Level,
                BestFame = finalFame
            }));
        }
        else {
            var entry = JsonConvert.DeserializeObject<DbClassStatsEntry>(json);
            if (character.Level > entry.BestLevel)
                entry.BestLevel = character.Level;
            if (finalFame > entry.BestFame)
                entry.BestFame = finalFame;
            SetValue(field, JsonConvert.SerializeObject(entry));
        }
    }
}

public class DbChar : RedisObject {
    public DbChar(DbAccount acc, int charId) {
        Account = acc;
        CharId = charId;
        Init(acc.Database, "char." + acc.AccountId + "." + charId);
    }

    public DbAccount Account { get; private set; }
    public int CharId { get; private set; }

    public ushort ObjectType {
        get => GetValue<ushort>("charType");
        set => SetValue("charType", value);
    }

    public int Level {
        get => GetValue<int>("level");
        set => SetValue("level", value);
    }

    public int Experience {
        get => GetValue<int>("exp");
        set => SetValue("exp", value);
    }

    public int Fame {
        get => GetValue<int>("fame");
        set => SetValue("fame", value);
    }

    public int FinalFame {
        get => GetValue<int>("finalFame");
        set => SetValue("finalFame", value);
    }

    public ushort[] Items {
        get => GetValue<ushort[]>("items");
        set => SetValue("items", value);
    }

    public int HP {
        get => GetValue<int>("hp");
        set => SetValue("hp", value);
    }

    public int MP {
        get => GetValue<int>("mp");
        set => SetValue("mp", value);
    }

    public int[] Stats {
        get => GetValue<int[]>("stats");
        set => SetValue("stats", value);
    }

    public int Tex1 {
        get => GetValue<int>("tex1");
        set => SetValue("tex1", value);
    }

    public int Tex2 {
        get => GetValue<int>("tex2");
        set => SetValue("tex2", value);
    }

    public int Skin {
        get => GetValue<int>("skin");
        set => SetValue("skin", value);
    }

    public int PetId {
        get => GetValue<int>("petId");
        set => SetValue("petId", value);
    }

    public byte[] FameStats {
        get => GetValue<byte[]>("fameStats");
        set => SetValue("fameStats", value);
    }

    public DateTime CreateTime {
        get => GetValue<DateTime>("createTime");
        set => SetValue("createTime", value);
    }

    public DateTime LastSeen {
        get => GetValue<DateTime>("lastSeen");
        set => SetValue("lastSeen", value);
    }

    public bool Dead {
        get => GetValue<bool>("dead");
        set => SetValue("dead", value);
    }

    public int HealthStackCount {
        get => GetValue<int>("hpPotCount");
        set => SetValue("hpPotCount", value);
    }

    public int MagicStackCount {
        get => GetValue<int>("mpPotCount");
        set => SetValue("mpPotCount", value);
    }

    public bool HasBackpack {
        get => GetValue<bool>("hasBackpack");
        set => SetValue("hasBackpack", value);
    }
}

public class DbDeath : RedisObject {
    public DbDeath(DbAccount acc, int charId) {
        Account = acc;
        CharId = charId;
        Init(acc.Database, "death." + acc.AccountId + "." + charId);
    }

    public DbAccount Account { get; private set; }
    public int CharId { get; private set; }

    public ushort ObjectType {
        get => GetValue<ushort>("objType");
        set => SetValue("objType", value);
    }

    public int Level {
        get => GetValue<int>("level");
        set => SetValue("level", value);
    }

    public int TotalFame {
        get => GetValue<int>("totalFame");
        set => SetValue("totalFame", value);
    }

    public string Killer {
        get => GetValue<string>("killer");
        set => SetValue("killer", value);
    }

    public bool FirstBorn {
        get => GetValue<bool>("firstBorn");
        set => SetValue("firstBorn", value);
    }

    public DateTime DeathTime {
        get => GetValue<DateTime>("deathTime");
        set => SetValue("deathTime", value);
    }
}

public struct DbNewsEntry {
    [JsonIgnore] public DateTime Date;
    public string Icon;
    public string Title;
    public string Text;
    public string Link;
}

public class DbNews // TODO. Check later, range results might be bugged...
{
    public DbNews(IDatabase db, int count) {
        Entries = db.SortedSetRangeByRankWithScores("news", 0, 10)
            .Select(x => {
                var ret = JsonConvert.DeserializeObject<DbNewsEntry>(
                    Encoding.UTF8.GetString(x.Element));
                ret.Date = new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(x.Score);
                return ret;
            }).ToArray();
    }

    public DbNewsEntry[] Entries { get; }
}

public class DbVault : RedisObject {
    public DbVault(DbAccount acc) {
        Account = acc;
        Init(acc.Database, "vault." + acc.AccountId);
    }

    public DbAccount Account { get; private set; }

    public ushort[] this[int index] {
        get =>
            GetValue<ushort[]>("vault." + index) ??
            Enumerable.Repeat((ushort) 0xffff, 8).ToArray();
        set => SetValue("vault." + index, value);
    }
}

public abstract class RInventory : RedisObject {
    public string Field { get; protected set; }

    public ushort[] Items {
        get => GetValue<ushort[]>(Field) ?? Enumerable.Repeat((ushort) 0xffff, 20).ToArray();
        set => SetValue(Field, value);
    }
}

public class DbVaultSingle : RInventory {
    public DbVaultSingle(DbAccount acc, int vaultIndex) {
        Field = "vault." + vaultIndex;
        Init(acc.Database, "vault." + acc.AccountId, Field);

        var items = GetValue<ushort[]>(Field);
        if (items != null)
            return;

        var trans = Database.CreateTransaction();
        SetValue(Field, Items);
        FlushAsync(trans);
        trans.Execute(CommandFlags.FireAndForget);
    }
}

public class DbCharInv : RInventory {
    public DbCharInv(DbAccount acc, int charId) {
        Field = "items";
        Init(acc.Database, "char." + acc.AccountId + "." + charId, Field);
    }
}

public struct DbLegendEntry {
    public readonly int AccId;
    public readonly int ChrId;

    public DbLegendEntry(int accId, int chrId) {
        AccId = accId;
        ChrId = chrId;
    }
}

public static class DbLegend {
    private const int MaxListings = 20;

    private static readonly Dictionary<string, TimeSpan> TimeSpans = new() {
        {"week", TimeSpan.FromDays(7)},
        {"month", TimeSpan.FromDays(30)},
        {"all", TimeSpan.MaxValue}
    };

    public static void Clean(IDatabase db) {
        // remove legend entries that expired
        foreach (var span in TimeSpans) {
            if (span.Value == TimeSpan.MaxValue) {
                // bound legend by count
                db.SortedSetRemoveRangeByRankAsync($"legends:{span.Key}:byFame",
                    0, -MaxListings - 1, CommandFlags.FireAndForget);
                continue;
            }

            // bound legend by time
            var outdated = db.SortedSetRangeByScore(
                $"legends:{span.Key}:byTimeOfDeath", 0,
                DateTime.UtcNow.ToUnixTimestamp());

            var trans = db.CreateTransaction();
            trans.SortedSetRemoveAsync($"legends:{span.Key}:byFame", outdated, CommandFlags.FireAndForget);
            trans.SortedSetRemoveAsync($"legends:{span.Key}:byTimeOfDeath", outdated, CommandFlags.FireAndForget);
            trans.ExecuteAsync(CommandFlags.FireAndForget);
        }

        // refresh legend hash
        db.KeyDeleteAsync("legend", CommandFlags.FireAndForget);
        foreach (var span in TimeSpans) {
            var legendTask = db.SortedSetRangeByRankAsync($"legends:{span.Key}:byFame",
                0, MaxListings - 1, Order.Descending);
            legendTask.ContinueWith(r => {
                var trans = db.CreateTransaction();
                foreach (var e in r.Result) {
                    var accId = BitConverter.ToInt32(e, 0);
                    trans.HashSetAsync("legend", accId, "",
                        flags: CommandFlags.FireAndForget);
                }

                trans.ExecuteAsync(CommandFlags.FireAndForget);
            });
        }

        db.StringSetAsync("legends:updateTime", DateTime.UtcNow.ToUnixTimestamp(),
            flags: CommandFlags.FireAndForget);
    }

    public static void Insert(IDatabase db,
        int accId, int chrId, int totalFame) {
        var buff = new byte[8];
        Buffer.BlockCopy(BitConverter.GetBytes(accId), 0, buff, 0, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(chrId), 0, buff, 4, 4);

        // add entry to each legends list
        var trans = db.CreateTransaction();
        foreach (var span in TimeSpans) {
            trans.SortedSetAddAsync($"legends:{span.Key}:byFame",
                buff, totalFame, CommandFlags.FireAndForget);

            if (span.Value == TimeSpan.MaxValue)
                continue;

            double t = DateTime.UtcNow.Add(span.Value).ToUnixTimestamp();
            trans.SortedSetAddAsync($"legends:{span.Key}:byTimeOfDeath",
                buff, t, CommandFlags.FireAndForget);
        }

        trans.ExecuteAsync();

        // add legend if character falls within MaxGlowingRank
        foreach (var span in TimeSpans)
            db.SortedSetRankAsync($"legends:{span.Key}:byFame", buff, Order.Descending)
                .ContinueWith(r => {
                    if (r.Result >= MaxListings)
                        return;

                    db.HashSetAsync("legend", accId, "",
                        flags: CommandFlags.FireAndForget);
                });

        db.StringSetAsync("legends:updateTime", DateTime.UtcNow.ToUnixTimestamp(),
            flags: CommandFlags.FireAndForget);
    }

    public static DbLegendEntry[] Get(IDatabase db, string timeSpan) {
        if (!TimeSpans.ContainsKey(timeSpan))
            return new DbLegendEntry[0];

        var listings = db.SortedSetRangeByRank(
            $"legends:{timeSpan}:byFame",
            0, MaxListings - 1, Order.Descending);

        return listings
            .Select(e => new DbLegendEntry(
                BitConverter.ToInt32(e, 0),
                BitConverter.ToInt32(e, 4)))
            .ToArray();
    }
}

public class DbGuild : RedisObject {
    internal readonly object MemberLock; // maybe use redis locking?

    internal DbGuild(IDatabase db, int id) {
        MemberLock = new object();

        Id = id;
        Init(db, "guild." + id);
    }

    public DbGuild(DbAccount acc) {
        MemberLock = new object();

        Id = acc.GuildId;
        Init(acc.Database, "guild." + Id);
    }

    public int Id { get; }

    public string Name {
        get => GetValue<string>("name");
        set => SetValue("name", value);
    }

    public int Level {
        get => GetValue<int>("level");
        set => SetValue("level", value);
    }

    public int Fame {
        get => GetValue<int>("fame");
        set => SetValue("fame", value);
    }

    public int TotalFame {
        get => GetValue<int>("totalFame");
        set => SetValue("totalFame", value);
    }

    public int[] Members // list of member account id's
    {
        get => GetValue<int[]>("members") ?? new int[0];
        set => SetValue("members", value);
    }

    public string Board {
        get => GetValue<string>("board") ?? "";
        set => SetValue("board", value);
    }
}

public class DbIpInfo {
    private readonly IDatabase _db;

    internal DbIpInfo(IDatabase db, string ip) {
        _db = db;
        IP = ip;
        var json = (string) db.HashGet("ips", ip);
        if (json == null)
            IsNull = true;
        else
            JsonConvert.PopulateObject(json, this);
    }

    [JsonIgnore] public string IP { get; }

    [JsonIgnore] public bool IsNull { get; private set; }

    public HashSet<int> Accounts { get; set; }
    public bool Banned { get; set; }
    public string Notes { get; set; }

    public void Flush() {
        _db.HashSetAsync("ips", IP, JsonConvert.SerializeObject(this));
    }
}