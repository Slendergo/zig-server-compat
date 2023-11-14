using Shared;
using System.Collections.Concurrent;
using System.Diagnostics;
using static GameServer.realm.Sight;

namespace GameServer.realm.entities.player;

public class UpdatedSet : HashSet<Entity>
{
    private readonly object _changeLock = new();
    private readonly Player _player;

    public UpdatedSet(Player player)
    {
        _player = player;
    }

    public new bool Add(Entity e)
    {
        using (TimedLock.Lock(_changeLock))
        {
            var added = base.Add(e);
            if (added)
                e.StatChanged += _player.HandleStatChanges;

            return added;
        }
    }

    public new bool Remove(Entity e)
    {
        using (TimedLock.Lock(_changeLock))
        {
            e.StatChanged -= _player.HandleStatChanges;
            return base.Remove(e);
        }
    }

    public new void RemoveWhere(Predicate<Entity> match)
    {
        using (TimedLock.Lock(_changeLock))
        {
            foreach (var e in this.Where(match.Invoke))
                e.StatChanged -= _player.HandleStatChanges;

            base.RemoveWhere(match);
        }
    }

    public void Dispose()
    {
        RemoveWhere(e => true);
    }
}

struct StaticIntPoint
{
    public int Id;
    public IntPoint Point;
}

public partial class Player
{
    public const int RADIUS = 15;
    public const int RADIUS_SQR = RADIUS * RADIUS;
    public const int CIRCUMFERENCE_SQR = (RADIUS - 1) * (RADIUS - 1);
    public const int STATIC_BOUNDING_BOX = RADIUS + 5;

    public int TickId { get; private set; }
    public int TickTime { get; private set; }

    public UpdatedSet ClientEntities { get; private set; }

    //private Dictionary<int, Entity> VisibleClientEntities = new Dictionary<int, Entity>();
    //private Dictionary<int, IntPoint> VisibleClientStaticEntities = new Dictionary<int, IntPoint>();

    private readonly HashSet<IntPoint> ClientStatics = new HashSet<IntPoint>();

    private readonly object StatUpdateLock = new object();
    private readonly Dictionary<Entity, Dictionary<StatsType, object>> StatUpdates = new Dictionary<Entity, Dictionary<StatsType, object>>();

    public readonly ConcurrentQueue<Entity> ClientKilledEntity = new();

    private ObjectStats[] UpdateStatuses;

    public Sight Sight { get; }

    public void HandleStatChanges(object entity, StatChangedEventArgs statChange)
    {
        if (entity is not Entity e || e != this && statChange.UpdateSelfOnly)
            return;

        using (TimedLock.Lock(StatUpdateLock))
        {
            if (e == this && statChange.Stat == StatsType.None)
                return;

            if (!StatUpdates.ContainsKey(e))
                StatUpdates[e] = new Dictionary<StatsType, object>();

            if (statChange.Stat != StatsType.None)
                StatUpdates[e][statChange.Stat] = statChange.Value;

            //SLog.Info($"{entity} {statChange.Stat} {statChange.Value}");
        }
    }

    private void SendNewTick(RealmTime time)
    {
        TickId++;
        TickTime = time.ElapsedMsDelta;

        using (TimedLock.Lock(StatUpdateLock))
        {
            UpdateStatuses = StatUpdates.Where(_ => ClientEntities.Contains(_.Key)).Select(_ => new ObjectStats
            {
                Id = _.Key.Id,
                Position = new Position { X = _.Key.X, Y = _.Key.Y },
                Stats = _.Value.ToArray()
            }).ToArray();
            StatUpdates.Clear();
        }

        Client.SendNewTick(TickId, time.ElapsedMsDelta, UpdateStatuses);
        AwaitMove(TickId);
    }

    // should be more than enough for most cases
    private readonly List<TileData> TilesToAdd = new List<TileData>(768); // enough for default sight with spare
    private readonly List<ObjectDef> ToAdd = new List<ObjectDef>(128);
    private readonly List<int> ToRemove = new List<int>(128);

    private void SendUpdate(RealmTime time)
    {
        AddTiles();
        CheckObjectsToAdd();
        CheckObjectsToRemove();

        if (TilesToAdd.Count == 0 && ToAdd.Count == 0 && ToRemove.Count == 0)
            return;

        Client.SendUpdate(TilesToAdd, ToAdd, ToRemove);
        AwaitUpdateAck(time.TotalElapsedMs);
    }

    private void AddTiles()
    {
        TilesToAdd.Clear();
        foreach (var point in Sight.VisibleTiles)
        {
            var x = point.X;
            var y = point.Y;
            var tile = Owner.Map[x, y];

            if (tile.TileId == 255 || Tiles[x, y] >= tile.UpdateCount)
                continue;

            TilesToAdd.Add(new TileData(x, y, tile.TileId));

            Tiles[x, y] = tile.UpdateCount;
        }

        FameCounter.UncoverTiles(TilesToAdd.Count);
    }

    private void CheckObjectsToAdd()
    {
        ToAdd.Clear();

        foreach (var i in Owner.Players)
            if ((i.Value == this || i.Value.Client.Account != null) && ClientEntities.Add(i.Value))
                ToAdd.Add(i.Value.ToDefinition());

        foreach (var i in Owner.PlayersCollision.HitTest(X, Y, RADIUS))
            if ((i is Decoy || i is Pet) && ClientEntities.Add(i))
                ToAdd.Add(i.ToDefinition());

        var p = new IntPoint(0, 0);
        foreach (var i in Owner.EnemiesCollision.HitTest(X, Y, RADIUS))
        {
            if (i is Container)
            {
                var owners = (i as Container).BagOwners;
                if (owners.Length > 0 && Array.IndexOf(owners, AccountId) == -1)
                    continue;
            }

            p.X = (int)i.X;
            p.Y = (int)i.Y;
            if (Sight.VisibleTiles.Contains(p) && ClientEntities.Add(i))
                ToAdd.Add(i.ToDefinition());
        }

        if (Quest?.Owner != null && ClientEntities.Add(Quest))
            ToAdd.Add(Quest.ToDefinition());

        foreach (var point in Sight.VisibleTiles)
        {
            var x = point.X;
            var y = point.Y;

            var tile = Owner.Map[x, y];
            if (tile.ObjectId != 0 && tile.ObjectType != 0 && ClientStatics.Add(point))
                ToAdd.Add(tile.ToDefinition(x, y));
        }
    }

    private void CheckObjectsToRemove()
    {
        ToRemove.Clear();
        while (ClientKilledEntity.TryDequeue(out var entity))
            ToRemove.Add(entity.Id);

        foreach (var i in ClientEntities)
        {
            if (i.Owner == null)
            {
                ToRemove.Add(i.Id);
                continue;
            }

            if (i is StaticObject so && so.Static)
                if (Math.Abs(STATIC_BOUNDING_BOX - ((int)X - i.X)) > 0 && Math.Abs(STATIC_BOUNDING_BOX - ((int)Y - i.Y)) > 0)
                    continue;

            if (i is Player || i == Quest || Sight.VisibleTiles.Contains(new IntPoint((int)i.X, (int)i.Y)))
                continue;

            var removed = ClientEntities.Remove(i);
            if (removed)
                ToRemove.Add(i.Id);
        }

        foreach (var i in ClientStatics)
        {
            var tile = Owner.Map[i.X, i.Y];
            if (tile.ObjectType != 0 && tile.ObjectId != 0 && STATIC_BOUNDING_BOX - ((int)X - i.X) > 0 && STATIC_BOUNDING_BOX - ((int)Y - i.Y) > 0)
                continue;

            var removed = ClientStatics.Remove(i);
            if (removed)
                ToRemove.Add(tile.ObjectId);
        }
    }

    //private readonly UpdateList<TileData> TilesToAdd = new UpdateList<TileData>(768);
    //private readonly UpdateList<ObjectDef> ToAdd = new UpdateList<ObjectDef>(64);
    //private readonly UpdateList<int> ToRemove = new UpdateList<int>(64);

    //private void SendUpdate(RealmTime time)
    //{
    //    AddTiles();
    //    CheckObjectsToAdd();
    //    CheckObjectsToRemove();

    //    if (TilesToAdd.Empty && ToAdd.Empty && ToRemove.Empty)
    //        return;

    //    Client.SendUpdate(TilesToAdd.Items, ToAdd.Items, ToRemove.Items);
    //    AwaitUpdateAck(time.TotalElapsedMs);
    //}

    //private void AddTiles()
    //{
    //    TilesToAdd.Clear();
    //    foreach (var point in Sight.VisibleTiles)
    //    {
    //        var x = point.X;
    //        var y = point.Y;
    //        var tile = Owner.Map[x, y];

    //        if (tile.TileId == 255 || Tiles[x, y] >= tile.UpdateCount)
    //            continue;

    //        TilesToAdd.Add(new TileData(x, y, tile.TileId));

    //        Tiles[x, y] = tile.UpdateCount;
    //    }

    //    FameCounter.UncoverTiles(TilesToAdd.Count);
    //}

    //private void CheckObjectsToAdd()
    //{
    //    var sw = Stopwatch.StartNew();

    //    ToAdd.Clear();
    //    GetNewEntities();
    //    GetNewStatics();

    //    sw.Stop();
    //    var t = sw.Elapsed;
    //    var ms = sw.Elapsed.TotalMilliseconds;
    //    Console.WriteLine($"CheckObjectsToRemove - Elapsed: {t} ({ms}ms)");
    //}

    //private void CheckObjectsToRemove()
    //{
    //    ToRemove.Clear();

    //    var toRemove = new UpdateList<Entity>();
    //    GetRemovedEntities(toRemove);

    //    for (var i = 0; i < toRemove.Count; i++)
    //    {
    //        var e = toRemove.Items[i];
    //        _ = ClientEntities.Remove(e);
    //        ToRemove.Add(e.Id);
    //    }

    //    var points = new UpdateList<IntPoint>();
    //    GetRemovedStatics(points);

    //    for (var i = 0; i < points.Count; i++)
    //        _ = ClientStatics.Remove(points.Items[i]);
    //}

    //private void GetNewEntities()
    //{
    //    foreach (var i in Owner.Players)
    //        if ((i.Value == this || i.Value.Client.Account != null) && ClientEntities.Add(i.Value))
    //            ToAdd.Add(i.Value.ToDefinition());

    //    foreach (var i in Owner.PlayersCollision.HitTest(X, Y, RADIUS))
    //        if ((i is Decoy || i is Pet) && ClientEntities.Add(i))
    //            ToAdd.Add(i.ToDefinition());

    //    var p = new IntPoint(0, 0);
    //    foreach (var i in Owner.EnemiesCollision.HitTest(X, Y, RADIUS))
    //    {
    //        if (i is Container)
    //        {
    //            var owners = (i as Container).BagOwners;
    //            if (owners.Length > 0 && Array.IndexOf(owners, AccountId) == -1)
    //                continue;
    //        }

    //        p.X = (int)i.X;
    //        p.Y = (int)i.Y;
    //        if (Sight.VisibleTiles.Contains(p) && ClientEntities.Add(i))
    //            ToAdd.Add(i.ToDefinition());
    //    }

    //    if (Quest?.Owner != null && ClientEntities.Add(Quest))
    //        ToAdd.Add(Quest.ToDefinition());
    //}

    //private void GetNewStatics()
    //{
    //    foreach (var point in Sight.VisibleTiles)
    //    {
    //        var x = point.X;
    //        var y = point.Y;
    //        var tile = Owner.Map[x, y];

    //        if (tile.ObjectId != 0 && tile.ObjectType != 0 && ClientStatics.Add(point))
    //            ToAdd.Add(tile.ToDefinition(x, y));
    //    }
    //}

    //private void GetRemovedEntities(UpdateList<Entity> objs)
    //{
    //    while (ClientKilledEntity.TryDequeue(out var entity))
    //        objs.Add(entity);

    //    foreach (var i in ClientEntities)
    //    {
    //        if (i.Owner == null)
    //            objs.Add(i);

    //        if (i is StaticObject so && so.Static)
    //            if (Math.Abs(STATIC_BOUNDING_BOX - ((int)X - i.X)) > 0 && Math.Abs(STATIC_BOUNDING_BOX - ((int)Y - i.Y)) > 0)
    //                continue;

    //        if (i is Player || i == Quest || Sight.VisibleTiles.Contains(new IntPoint((int)i.X, (int)i.Y)))
    //            continue;

    //        objs.Add(i);
    //    }
    //}

    //private void GetRemovedStatics(UpdateList<IntPoint> points)
    //{
    //    foreach (var i in ClientStatics)
    //    {
    //        var tile = Owner.Map[i.X, i.Y];
    //        if (STATIC_BOUNDING_BOX - ((int)X - i.X) > 0 && STATIC_BOUNDING_BOX - ((int)Y - i.Y) > 0 && tile.ObjectType != 0 && tile.ObjectId != 0)
    //            continue;

    //        points.Add(i);
    //        ToRemove.Add(tile.ObjectId);
    //    }
    //}

    //public sealed class UpdateList<T>
    //{
    //    public bool IsFull => Count >= _items.Length;
    //    public bool Empty => Count == 0;

    //    public T[] Items => _items[0..Count];

    //    private T[] _items;
    //    public int Count;

    //    public UpdateList()
    //    {
    //        Count = 0;
    //        _items = new T[1];
    //    }

    //    public UpdateList(int capacity)
    //    {
    //        Count = 0;
    //        _items = new T[Math.Max(1, capacity)];
    //    }

    //    public void Add(T item)
    //    {
    //        if (IsFull)
    //            Array.Resize(ref _items, Count * 2);

    //        _items[Count] = item;
    //        Count++;
    //    }

    //    public T Remove(int index)
    //    {
    //        Count--;

    //        var old = _items[index];
    //        _items[index] = _items[Count];
    //        return old;
    //    }

    //    public void Clear() => Count = 0;
    //}
}