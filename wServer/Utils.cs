using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using common;
using common.resources;
using wServer.realm;
using wServer.realm.entities;
using wServer.realm.worlds;

namespace wServer;

public static class PacketUtils {
    public const int LENGTH_PREFIX = 2;
    public const int SEND_BUFFER_LEN = ushort.MaxValue;
    public const int RECV_BUFFER_LEN = ushort.MaxValue;

    public static byte ReadByte(ref int ptr, ref byte spanRef, int len) {
        if (ptr + 1 > len)
            throw new Exception("Receive buffer attempted to read out of bounds");
        return Unsafe.ReadUnaligned<byte>(ref Unsafe.Add(ref spanRef, ptr++));
    }

    public static bool ReadBool(ref int ptr, ref byte spanRef, int len) {
        if (ptr + 1 > len)
            throw new Exception("Receive buffer attempted to read out of bounds");

        return Unsafe.ReadUnaligned<bool>(ref Unsafe.Add(ref spanRef, ptr++));
    }

    public static char ReadChar(ref int ptr, ref byte spanRef, int len) {
        if (ptr + 1 > len)
            throw new Exception("Receive buffer attempted to read out of bounds");

        return Unsafe.ReadUnaligned<char>(ref Unsafe.Add(ref spanRef, ptr++));
    }

    public static short ReadShort(ref int ptr, ref byte spanRef, int len) {
        if (ptr + 2 > len)
            throw new Exception("Receive buffer attempted to read out of bounds");

        var i = Unsafe.ReadUnaligned<short>(ref Unsafe.Add(ref spanRef, ptr));
        ptr += 2;
        return i;
    }

    public static ushort ReadUShort(ref int ptr, ref byte spanRef, int len) {
        if (ptr + 2 > len)
            throw new Exception("Receive buffer attempted to read out of bounds");

        var i = Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref spanRef, ptr));
        ptr += 2;
        return i;
    }

    public static int ReadInt(ref int ptr, ref byte spanRef, int len) {
        if (ptr + 4 > len) throw new Exception("Receive buffer attempted to read out of bounds");

        var i = Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref spanRef, ptr));
        ptr += 4;
        return i;
    }

    public static uint ReadUInt(ref int ptr, ref byte spanRef, int len) {
        if (ptr + 4 > len)
            throw new Exception("Receive buffer attempted to read out of bounds");

        var i = Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref spanRef, ptr));
        ptr += 4;
        return i;
    }

    public static float ReadFloat(ref int ptr, ref byte spanRef, int len) {
        if (ptr + 4 > len)
            throw new Exception("Receive buffer attempted to read out of bounds");

        var i = Unsafe.ReadUnaligned<float>(ref Unsafe.Add(ref spanRef, ptr));
        ptr += 4;
        return i;
    }

    public static long ReadLong(ref int ptr, ref byte spanRef, int len) {
        if (ptr + 8 > len)
            throw new Exception("Receive buffer attempted to read out of bounds");

        var i = Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref spanRef, ptr));
        ptr += 8;
        return i;
    }

    public static ulong ReadULong(ref int ptr, ref byte spanRef, int len) {
        if (ptr + 8 > len)
            throw new Exception("Receive buffer attempted to read out of bounds");

        var i = Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref spanRef, ptr));
        ptr += 8;
        return i;
    }

    // doable but might be slower than manually calling
    //public static unsafe T Read<T>(ref int ptr, ref byte spanRef, int len) where T : unmanaged
    //{
    //    if (ptr + sizeof(T) > len)
    //        throw new Exception("Receive buffer attempted to read out of bounds");

    //    var i = Unsafe.ReadUnaligned<T>(ref Unsafe.Add(ref spanRef, ptr));
    //    ptr += sizeof(T);
    //    return i;
    //}

    public static string ReadString(ref int ptr, ref byte spanRef, int len) {
        if (ptr + 2 > len)
            throw new Exception("Receive buffer attempted to read out of bounds");

        var strLen = Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref spanRef, ptr));
        ptr += 2;
        if (ptr + strLen > len)
            throw new Exception("Receive buffer attempted to read out of bounds");

        Span<byte> s = stackalloc byte[strLen];
        Unsafe.CopyBlockUnaligned(ref MemoryMarshal.GetReference(s), ref Unsafe.Add(ref spanRef, ptr), strLen);
        ptr += strLen;
        return Encoding.ASCII.GetString(s);
    }

    public static bool[] ReadBoolArray(ref int ptr, ref byte spanRef, int len) {
        if (ptr + 2 > len)
            throw new Exception("Receive buffer attempted to read out of bounds");

        var arrLen = Unsafe.ReadUnaligned<short>(ref Unsafe.Add(ref spanRef, ptr));
        ptr += 2;
        if (ptr + arrLen > len)
            throw new Exception("Receive buffer attempted to read out of bounds");

        var ret = new bool[arrLen];
        for (var i = 0; i < arrLen; i++)
            ret[i] = Unsafe.ReadUnaligned<bool>(ref Unsafe.Add(ref spanRef, ptr++));

        return ret;
    }

    public static MoveRecord[] ReadMoveRecordArray(ref int ptr, ref byte spanRef, int len) {
        if (ptr + 2 > len)
            throw new Exception("Receive buffer attempted to read out of bounds");

        var arrLen = Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref spanRef, ptr));
        ptr += 2;
        if (ptr + arrLen * 16 > len)
            throw new Exception("Receive buffer attempted to read out of bounds");

        var ret = new MoveRecord[arrLen];
        for (var i = 0; i < arrLen; i++)
            ret[i] = new MoveRecord {
                Time = ReadLong(ref ptr, ref spanRef, len),
                X = ReadFloat(ref ptr, ref spanRef, len),
                Y = ReadFloat(ref ptr, ref spanRef, len)
            };

        return ret;
    }

    public static void WriteByte(ref int ptr, ref byte spanRef, byte i) {
        if (ptr + 1 > SEND_BUFFER_LEN)
            throw new Exception("Send buffer attempted to write out of bounds");

        Unsafe.WriteUnaligned(ref Unsafe.Add(ref spanRef, ptr++), i);
    }

    public static void WriteSByte(ref int ptr, ref byte spanRef, sbyte i) {
        if (ptr + 1 > SEND_BUFFER_LEN)
            throw new Exception("Send buffer attempted to write out of bounds");

        Unsafe.WriteUnaligned(ref Unsafe.Add(ref spanRef, ptr++), i);
    }

    public static void WriteBool(ref int ptr, ref byte spanRef, bool b) {
        if (ptr + 1 > SEND_BUFFER_LEN)
            throw new Exception("Send buffer attempted to write out of bounds");

        Unsafe.WriteUnaligned(ref Unsafe.Add(ref spanRef, ptr++), b);
    }

    public static void WriteChar(ref int ptr, ref byte spanRef, char i) {
        if (ptr + 1 > SEND_BUFFER_LEN)
            throw new Exception("Send buffer attempted to write out of bounds");

        Unsafe.WriteUnaligned(ref Unsafe.Add(ref spanRef, ptr++), i);
    }

    public static void WriteShort(ref int ptr, ref byte spanRef, short i) {
        if (ptr + 2 > SEND_BUFFER_LEN)
            throw new Exception("Send buffer attempted to write out of bounds");

        Unsafe.WriteUnaligned(ref Unsafe.Add(ref spanRef, ptr), i);
        ptr += 2;
    }

    public static void WriteUShort(ref int ptr, ref byte spanRef, ushort i) {
        if (ptr + 2 > SEND_BUFFER_LEN)
            throw new Exception("Send buffer attempted to write out of bounds");

        Unsafe.WriteUnaligned(ref Unsafe.Add(ref spanRef, ptr), i);
        ptr += 2;
    }

    public static void WriteInt(ref int ptr, ref byte spanRef, int i) {
        if (ptr + 4 > SEND_BUFFER_LEN)
            throw new Exception("Send buffer attempted to write out of bounds");

        Unsafe.WriteUnaligned(ref Unsafe.Add(ref spanRef, ptr), i);
        ptr += 4;
    }

    public static void WriteUInt(ref int ptr, ref byte spanRef, uint i) {
        if (ptr + 4 > SEND_BUFFER_LEN)
            throw new Exception("Send buffer attempted to write out of bounds");

        Unsafe.WriteUnaligned(ref Unsafe.Add(ref spanRef, ptr), i);
        ptr += 4;
    }

    public static void WriteLong(ref int ptr, ref byte spanRef, long i) {
        if (ptr + 8 > SEND_BUFFER_LEN)
            throw new Exception("Send buffer attempted to write out of bounds");

        Unsafe.WriteUnaligned(ref Unsafe.Add(ref spanRef, ptr), i);
        ptr += 8;
    }

    public static void WriteULong(ref int ptr, ref byte spanRef, ulong i) {
        if (ptr + 8 > SEND_BUFFER_LEN)
            throw new Exception("Send buffer attempted to write out of bounds");

        Unsafe.WriteUnaligned(ref Unsafe.Add(ref spanRef, ptr), i);
        ptr += 8;
    }

    public static void WriteFloat(ref int ptr, ref byte spanRef, float i) {
        if (ptr + 4 > SEND_BUFFER_LEN)
            throw new Exception("Send buffer attempted to write out of bounds");

        Unsafe.WriteUnaligned(ref Unsafe.Add(ref spanRef, ptr), i);
        ptr += 4;
    }

    public static void WriteString(ref int ptr, ref byte spanRef, string s) {
        if (ptr + 2 + (ushort) s.Length > SEND_BUFFER_LEN)
            throw new Exception("Send buffer attempted to write out of bounds");

        WriteUShort(ref ptr, ref spanRef, (ushort) s.Length);
        foreach (var b in s)
            WriteChar(ref ptr, ref spanRef, b);
    }
}

internal static class EntityUtils {
    public static double DistSqr(this Entity a, Entity b) {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }

    public static double Dist(this Entity a, Entity b) {
        return Math.Sqrt(a.DistSqr(b));
    }

    public static bool AnyPlayerNearby(this Entity entity, int radius = Player.Radius) {
        foreach (var i in entity.Owner.PlayersCollision.HitTest(entity.X, entity.Y, radius).Where(e => e is Player)) {
            var d = i.DistSqr(entity);
            if (d < radius * radius)
                return true;
        }

        return false;
    }

    public static bool AnyPlayerNearby(this World world, double x, double y, int radius = Player.Radius) {
        foreach (var i in world.PlayersCollision.HitTest(x, y, radius).Where(e => e is Player)) {
            var d = MathsUtils.DistSqr(i.X, i.Y, x, y);
            if (d < radius * radius)
                return true;
        }

        return false;
    }

    public static bool AnyEnemyNearby(this Entity entity, int radius = Player.Radius) {
        foreach (var i in entity.Owner.EnemiesCollision.HitTest(entity.X, entity.Y, radius)) {
            if (!(i is Enemy) || entity == i)
                continue;

            var d = i.DistSqr(entity);
            if (d < radius * radius)
                return true;
        }

        return false;
    }

    public static bool AnyEnemyNearby(this World world, double x, double y, int radius = Player.Radius) {
        foreach (var i in world.EnemiesCollision.HitTest(x, y, radius)) {
            if (!(i is Enemy))
                continue;

            var d = MathsUtils.DistSqr(i.X, i.Y, x, y);
            if (d < radius * radius)
                return true;
        }

        return false;
    }

    public static Entity
        GetLowestHpEntity(this Entity entity, double dist, ushort? objType,
            bool seeInvis = false) // objType = null for player
    {
        var entities = entity.GetNearestEntities(dist, objType, seeInvis).OfType<Character>();
        if (!entities.Any())
            return null;

        var lowestHp = entities.Min(e => e.HP);
        return entities.FirstOrDefault(e => e.HP == lowestHp);
    }

    public static Entity
        GetNearestEntity(this Entity entity, double dist, ushort? objType, bool seeInvis = false) //Null for player
    {
        //return entity.GetNearestEntities(dist, objType).FirstOrDefault();

        // function speed might be a problem
        var entities = entity.GetNearestEntities(dist, objType, seeInvis).ToArray();
        if (entities.Length <= 0)
            return null;
        return entities.Aggregate(
            (curmin, x) => curmin == null || x.DistSqr(entity) < curmin.DistSqr(entity) ? x : curmin);
    }

    public static IEnumerable<Entity> GetNearestEntities(this Entity entity, double dist, ushort? objType,
        bool seeInvis = false) //Null for player
    {
        if (entity.Owner == null) yield break;
        if (objType == null)
            foreach (var i in entity.Owner.PlayersCollision.HitTest(entity.X, entity.Y, dist)
                         .Where(e => e is IPlayer)) {
                if (!seeInvis && !(i as IPlayer).IsVisibleToEnemy()) continue;
                var d = i.Dist(entity);
                if (d < dist)
                    yield return i;
            }
        else
            foreach (var i in entity.Owner.EnemiesCollision.HitTest(entity.X, entity.Y, dist)) {
                if (i.ObjectType != objType.Value) continue;
                var d = i.Dist(entity);
                if (d < dist)
                    yield return i;
            }
    }

    public static IEnumerable<Entity> GetNearestEntitiesBySquare(this Entity entity, double dist, ushort? objType,
        bool seeInvis = false) //Null for player
    {
        if (entity.Owner == null) yield break;
        if (objType == null)
            foreach (var i in entity.Owner.PlayersCollision.HitTest(entity.X, entity.Y, dist)
                         .Where(e => e is IPlayer)) {
                if (!seeInvis && !(i as IPlayer).IsVisibleToEnemy()) continue;
                var d = i.Dist(entity);
                if (d < dist)
                    yield return i;
            }
        else
            foreach (var i in entity.Owner.EnemiesCollision.HitTest(entity.X, entity.Y, dist)) {
                if (i.ObjectType != objType.Value) continue;
                var d = i.Dist(entity);
                if (d < dist)
                    yield return i;
            }
    }

    public static Entity GetNearestEntity(this Entity entity, double dist, bool players,
        Predicate<Entity> predicate = null) {
        if (entity.Owner == null) return null;
        Entity ret = null;
        if (players)
            foreach (var i in entity.Owner.PlayersCollision.HitTest(entity.X, entity.Y, dist)
                         .Where(e => e is IPlayer)) {
                if (!(i as IPlayer).IsVisibleToEnemy() ||
                    i == entity) continue;
                var d = i.Dist(entity);
                if (d < dist) {
                    if (predicate != null && !predicate(i))
                        continue;
                    dist = d;
                    ret = i;
                }
            }
        else
            foreach (var i in entity.Owner.EnemiesCollision.HitTest(entity.X, entity.Y, dist)) {
                if (i == entity) continue;
                var d = i.Dist(entity);
                if (d < dist) {
                    if (predicate != null && !predicate(i))
                        continue;
                    dist = d;
                    ret = i;
                }
            }

        return ret;
    }

    public static Entity GetNearestEntityByGroup(this Entity entity, double dist, string group) {
        //return entity.GetNearestEntitiesByGroup(dist, group).FirstOrDefault();

        // function speed might be a problem
        var entities = entity.GetNearestEntitiesByGroup(dist, group).ToArray();
        if (entities.Length <= 0)
            return null;
        return entities.Aggregate(
            (curmin, x) => curmin == null || x.DistSqr(entity) < curmin.DistSqr(entity) ? x : curmin);
    }

    public static IEnumerable<Entity> GetNearestEntitiesByGroup(this Entity entity, double dist, string group) {
        if (entity.Owner == null)
            yield break;
        foreach (var i in entity.Owner.EnemiesCollision.HitTest(entity.X, entity.Y, dist)) {
            if (i.ObjectDesc == null ||
                i.ObjectDesc.Group == null ||
                !i.ObjectDesc.Group.Equals(
                    group, StringComparison.InvariantCultureIgnoreCase))
                continue;
            var d = i.Dist(entity);
            if (d < dist)
                yield return i;
        }
    }

    public static Entity GetNearestEntityByName(this Entity entity, double dist, string id) {
        //return entity.GetNearestEntitiesByName(dist, id).FirstOrDefault();

        // function speed might be a problem
        var entities = entity.GetNearestEntitiesByName(dist, id).ToArray();
        if (entities.Length <= 0)
            return null;
        return entities.Aggregate(
            (curmin, x) => curmin == null || x.DistSqr(entity) < curmin.DistSqr(entity) ? x : curmin);
    }

    public static IEnumerable<Entity> GetNearestEntitiesByName(this Entity entity, double dist, string id) {
        if (entity.Owner == null)
            yield break;
        foreach (var i in entity.Owner.EnemiesCollision.HitTest(entity.X, entity.Y, dist)) {
            if (i.ObjectDesc == null || (id != null && !i.ObjectDesc.ObjectId.ContainsIgnoreCase(id)))
                continue;

            var d = i.Dist(entity);
            if (d < dist)
                yield return i;
        }
    }

    public static int CountEntity(this Entity entity, double dist, ushort? objType) {
        if (entity.Owner == null) return 0;
        var ret = 0;
        if (objType == null)
            foreach (var i in entity.Owner.PlayersCollision.HitTest(entity.X, entity.Y, dist).Where(e => e is Player)) {
                if (!(i as IPlayer).IsVisibleToEnemy()) continue;
                var d = i.Dist(entity);
                if (d < dist)
                    ret++;
            }
        else
            foreach (var i in entity.Owner.EnemiesCollision.HitTest(entity.X, entity.Y, dist)) {
                if (i.ObjectType != objType.Value) continue;
                var d = i.Dist(entity);
                if (d < dist)
                    ret++;
            }

        return ret;
    }

    public static int CountEntity(this Entity entity, double dist, string group) {
        if (entity.Owner == null) return 0;
        var ret = 0;
        foreach (var i in entity.Owner.EnemiesCollision.HitTest(entity.X, entity.Y, dist)) {
            if (i.ObjectDesc == null || i.ObjectDesc.Group != group) continue;
            var d = i.Dist(entity);
            if (d < dist)
                ret++;
        }

        return ret;
    }

    public static float GetSpeed(this Entity entity, float spd) {
        return entity.HasConditionEffect(ConditionEffects.Slowed) ? (5.55f * spd + 0.74f) / 2 : 5.55f * spd + 0.74f;
    }

    public static void AOE(this Entity entity, float radius, ushort? objType, Action<Entity> callback) //Null for player
    {
        if (objType == null)
            foreach (var i in entity.Owner.PlayersCollision.HitTest(entity.X, entity.Y, radius)
                         .Where(e => e is Player)) {
                var d = i.Dist(entity);
                if (d < radius)
                    callback(i);
            }
        else
            foreach (var i in entity.Owner.EnemiesCollision.HitTest(entity.X, entity.Y, radius)) {
                if (i.ObjectType != objType.Value) continue;
                var d = i.Dist(entity);
                if (d < radius)
                    callback(i);
            }
    }

    public static void AOE(this Entity entity, float radius, bool players, Action<Entity> callback) //Null for player
    {
        if (players)
            foreach (var i in entity.Owner.PlayersCollision.HitTest(entity.X, entity.Y, radius)
                         .Where(e => e is Player)) {
                var d = i.Dist(entity);
                if (d < radius)
                    callback(i);
            }
        else
            foreach (var i in entity.Owner.EnemiesCollision.HitTest(entity.X, entity.Y, radius)) {
                if (!(i is Enemy)) continue;
                var d = i.Dist(entity);
                if (d < radius)
                    callback(i);
            }
    }

    public static void
        AOE(this World world, Position pos, float radius, bool players, Action<Entity> callback) //Null for player
    {
        if (players)
            foreach (var i in world.PlayersCollision.HitTest(pos.X, pos.Y, radius).Where(e => e is Player)) {
                var d = MathsUtils.Dist(i.X, i.Y, pos.X, pos.Y);
                if (d < radius)
                    callback(i);
            }
        else
            foreach (var i in world.EnemiesCollision.HitTest(pos.X, pos.Y, radius)) {
                if (i is not Enemy e || e.ObjectDesc.Static)
                    continue;

                var d = MathsUtils.Dist(i.X, i.Y, pos.X, pos.Y);
                if (d < radius)
                    callback(i);
            }
    }

    public static void ForceUpdate(this Entity e, int slot) {
        if (e == null || (!(e is Player) && slot >= 8))
            return;

        switch (slot) {
            case 0:
            case 1:
            case 2:
            case 3:
            case 4:
            case 5:
            case 6:
            case 7:
            case 8:
            case 9:
            case 10:
            case 11:
                e.InvokeStatChange(
                    (StatsType) ((int) StatsType.Inventory0 + slot),
                    (e as IContainer).Inventory[slot]?.ObjectType ?? -1);
                break;
            case 12:
            case 13:
            case 14:
            case 15:
            case 16:
            case 17:
            case 18:
            case 19:
                e.InvokeStatChange(
                    (StatsType) ((int) StatsType.BackPack0 + slot),
                    (e as IContainer).Inventory[slot]?.ObjectType ?? -1);
                break;
            case 254:
                e.InvokeStatChange(
                    StatsType.HealthStackCount, (e as Player).HealthPots.Count);
                break;
            case 255:
                e.InvokeStatChange(
                    StatsType.MagicStackCount, (e as Player).MagicPots.Count);
                break;
        }
    }
}

internal static class ItemUtils {
    public static bool AuditItem(this IContainer container, Item item, int slot) {
        if (container is OneWayContainer && item != null)
            return false;

        return item == null || container.SlotTypes[slot] == 0 || item.SlotType == container.SlotTypes[slot];
    }
}

internal static class EnumerableUtils {
    public static T RandomElement<T>(this IEnumerable<T> source,
        Random rng) {
        var current = default(T);
        var count = 0;
        foreach (var element in source) {
            count++;
            if (rng.Next(count) == 0) current = element;
        }

        if (count == 0) throw new InvalidOperationException("Sequence was empty");
        return current;
    }
}

internal static class MathsUtils {
    public static double Dist(double x1, double y1, double x2, double y2) {
        return Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
    }

    public static double DistSqr(double x1, double y1, double x2, double y2) {
        return (x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2);
    }

    public static double DistSqr(int x1, int y1, int x2, int y2) {
        return (x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2);
    }

    // http://stackoverflow.com/questions/1042902/most-elegant-way-to-generate-prime-numbers/1072205#1072205
    public static List<int> GeneratePrimes(int n) {
        var limit = ApproximateNthPrime(n);
        var bits = SieveOfEratosthenes(limit);
        var primes = new List<int>(n);
        for (int i = 0, found = 0; i < limit && found < n; i++)
            if (bits[i]) {
                primes.Add(i);
                found++;
            }

        return primes;
    }

    private static int ApproximateNthPrime(int nn) {
        var n = (double) nn;
        double p;
        if (nn >= 7022)
            p = n * Math.Log(n) + n * (Math.Log(Math.Log(n)) - 0.9385);
        else if (nn >= 6)
            p = n * Math.Log(n) + n * Math.Log(Math.Log(n));
        else if (nn > 0)
            p = new[] {2, 3, 5, 7, 11}[nn - 1];
        else
            p = 0;

        return (int) p;
    }

    private static BitArray SieveOfEratosthenes(int limit) {
        var bits = new BitArray(limit + 1, true);
        bits[0] = false;
        bits[1] = false;
        for (var i = 0; i * i <= limit; i++)
            if (bits[i])
                for (var j = i * i; j <= limit; j += i)
                    bits[j] = false;

        return bits;
    }
}