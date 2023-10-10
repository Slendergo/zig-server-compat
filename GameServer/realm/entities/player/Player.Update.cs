using System.Collections.Concurrent;
using System.Xml.Serialization;
using Shared;

namespace GameServer.realm.entities.player;

public class UpdatedSet : HashSet<Entity> {
    private readonly object _changeLock = new();
    private readonly Player _player;

    public UpdatedSet(Player player) {
        _player = player;
    }

    public new bool Add(Entity e) {
        using (TimedLock.Lock(_changeLock)) {
            var added = base.Add(e);
            if (added)
                e.StatChanged += _player.HandleStatChanges;

            return added;
        }
    }

    public new bool Remove(Entity e) {
        using (TimedLock.Lock(_changeLock)) {
            e.StatChanged -= _player.HandleStatChanges;
            return base.Remove(e);
        }
    }

    public new void RemoveWhere(Predicate<Entity> match) {
        using (TimedLock.Lock(_changeLock)) {
            foreach (var e in this.Where(match.Invoke))
                e.StatChanged -= _player.HandleStatChanges;

            base.RemoveWhere(match);
        }
    }

    public void Dispose() {
        RemoveWhere(e => true);
    }
}

public partial class Player
{
    public const int RADIUS = 15;
    public const int RADIUS_SQR = RADIUS * RADIUS;
    public const int STATIC_BOUNDING_BOX = RADIUS * 2;

    public int TickId { get; private set; }
    public int TickTime { get; private set; }

    public UpdatedSet ClientEntities { get; private set; }


    private readonly HashSet<IntPoint> ClientStatics = new HashSet<IntPoint>();

    private readonly List<ObjectDef> NewStatics = new List<ObjectDef>();

    private readonly object StatUpdateLock = new object();
    private readonly Dictionary<Entity, Dictionary<StatsType, object>> StatUpdates = new Dictionary<Entity, Dictionary<StatsType, object>>();

    public readonly ConcurrentQueue<Entity> ClientKilledEntity = new();

    private ObjectDef[] _newObjects;
    private int[] _removedObjects;
    private TileData[] _tiles;
    private ObjectStats[] _updateStatuses;

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

            //Log.Info($"{entity} {statChange.Stat} {statChange.Value}");
        }
    }

    private void SendNewTick(RealmTime time)
    {
        TickId++;
        TickTime = time.ElapsedMsDelta;

        using (TimedLock.Lock(StatUpdateLock))
        {
            _updateStatuses = StatUpdates.Where(_ => ClientEntities.Contains(_.Key)).Select(_ => new ObjectStats
            {
                Id = _.Key.Id,
                Position = new Position { X = _.Key.X, Y = _.Key.Y },
                Stats = _.Value.ToArray()
            }).ToArray();
            StatUpdates.Clear();
        }

        Client.SendNewTick(TickId, time.ElapsedMsDelta, _updateStatuses);
        AwaitMove(TickId);
    }

    private void SendUpdate(RealmTime time)
    {
        var tilesUpdate = new List<TileData>();
        foreach (var point in Sight.VisibleTiles)
        {
            var x = point.X;
            var y = point.Y;
            var tile = Owner.Map[x, y];

            if (tile.TileId == 255 || Tiles[x, y] >= tile.UpdateCount)
                continue;

            tilesUpdate.Add(new TileData(x, y, tile.TileId));

            Tiles[x, y] = tile.UpdateCount;
        }

        FameCounter.UncoverTiles(tilesUpdate.Count);

        // get list of new static objects to add
        var staticsUpdate = GetNewStatics().ToArray();

        // get dropped entities list
        var entitiesRemove = new HashSet<int>(GetRemovedEntities());

        // removed stale entities
        ClientEntities.RemoveWhere(e => entitiesRemove.Contains(e.Id));

        // get list of added entities
        var entitiesAdd = GetNewEntities().ToArray();

        // get dropped statics list
        var staticsRemove = new HashSet<IntPoint>(GetRemovedStatics());
        ClientStatics.ExceptWith(staticsRemove);

        if (tilesUpdate.Count > 0 || entitiesRemove.Count > 0 || staticsRemove.Count > 0 ||
            entitiesAdd.Length > 0 || staticsUpdate.Length > 0)
        {
            entitiesRemove.UnionWith(
                staticsRemove.Select(s => Owner.Map[s.X, s.Y].ObjectId));

            _tiles = tilesUpdate.ToArray();
            _newObjects = entitiesAdd.Select(_ => _.ToDefinition()).Concat(staticsUpdate).ToArray();
            _removedObjects = entitiesRemove.ToArray();
            Client.SendUpdate(_tiles, _removedObjects, _newObjects);
            AwaitUpdateAck(time.TotalElapsedMs);
        }
    }

    private IEnumerable<Entity> GetNewEntities()
    {
        while (ClientKilledEntity.TryDequeue(out var entity))
            _ = ClientEntities.Remove(entity);

        foreach (var i in Owner.Players)
            if ((i.Value == this || i.Value.Client.Account != null) && ClientEntities.Add(i.Value))
                yield return i.Value;

        foreach (var i in Owner.PlayersCollision.HitTest(X, Y, RADIUS))
            if ((i is Decoy || i is Pet) && ClientEntities.Add(i))
                yield return i;

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
                yield return i;
        }

        if (Quest?.Owner != null && ClientEntities.Add(Quest))
            yield return Quest;
    }

    private IEnumerable<int> GetRemovedEntities()
    {
        foreach (var e in ClientKilledEntity)
            yield return e.Id;

        foreach (var i in ClientEntities)
        {
            if (i.Owner == null)
                yield return i.Id;

            if (i is StaticObject so && so.Static)
                if (Math.Abs(STATIC_BOUNDING_BOX - ((int)X - i.X)) > 0 && Math.Abs(STATIC_BOUNDING_BOX - ((int)Y - i.Y)) > 0)
                    continue;

            if (i is Player || i == Quest || Sight.VisibleTiles.Contains(new IntPoint((int)i.X, (int)i.Y)))
                continue;

            yield return i.Id;
        }
    }


    private IEnumerable<ObjectDef> GetNewStatics()
    {
        NewStatics.Clear();
        foreach (var point in Sight.VisibleTiles)
        {
            var x = point.X;
            var y = point.Y;
            var tile = Owner.Map[x, y];

            if (tile.ObjectId != 0 && tile.ObjectType != 0 && ClientStatics.Add(point))
                NewStatics.Add(tile.ToDef(x, y));
        }
        return NewStatics;
    }

    private IEnumerable<IntPoint> GetRemovedStatics()
    {
        foreach (var i in ClientStatics)
        {
            var tile = Owner.Map[i.X, i.Y];
            if (STATIC_BOUNDING_BOX - ((int)X - i.X) > 0 && STATIC_BOUNDING_BOX - ((int)Y - i.Y) > 0 && tile.ObjectType != 0 && tile.ObjectId != 0)
                continue;
            yield return i;
        }
    }
}