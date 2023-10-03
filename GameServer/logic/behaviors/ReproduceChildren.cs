using System.Xml.Linq;
using Shared;
using GameServer.realm;
using GameServer.realm.entities;

namespace GameServer.logic.behaviors;

internal class ReproduceChildren : Behavior {
    private readonly ushort[] _children;
    private readonly int _initialSpawn;

    private readonly int _maxChildren;
    private Cooldown _coolDown;

    public ReproduceChildren(XElement e) {
        _children = e.ParseStringArray("@children", ',', new string[0]).Select(x => GetObjType(x)).ToArray();
        _maxChildren = e.ParseInt("@maxChildren", 5);
        _initialSpawn = (int) (_maxChildren * e.ParseFloat("@initialSpawn", 0.5f));
        _coolDown = new Cooldown().Normalize(e.ParseInt("@cooldown", 1000));
    }

    public ReproduceChildren(int maxChildren = 5, double initialSpawn = 0.5, Cooldown coolDown = new(),
        params string[] children) {
        _children = children.Select(GetObjType).ToArray();
        _maxChildren = maxChildren;
        _initialSpawn = (int) (maxChildren * initialSpawn);
        _coolDown = coolDown.Normalize(0);
    }

    protected override void OnStateEntry(Entity host, RealmTime time, ref object state) {
        state = new SpawnState {
            livingChildren = new List<Enemy>(),
            RemainingTime = _coolDown.Next(Random)
        };
        for (var i = 0; i < _initialSpawn; i++) {
            var entity = Entity.Resolve(host.Manager, _children[Random.Next(0, _children.Count())]);
            entity.GivesNoXp = true;
            entity.Move(host.X, host.Y);

            var enemyEntity = entity as Enemy;
            if (host is Enemy enemyHost && enemyEntity != null) {
                enemyEntity.Terrain = enemyHost.Terrain;
                if (enemyHost.Spawned) enemyEntity.Spawned = true;
            }

            (state as SpawnState).livingChildren.Add(enemyEntity);
            host.Owner.EnterWorld(entity);
        }
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state) {
        if (state is not SpawnState spawn)
            return;

        var toRemove = new List<Enemy>();
        foreach (var child in spawn.livingChildren)
            if (child.HP < 0)
                toRemove.Add(child);
        foreach (var child in toRemove)
            spawn.livingChildren.Remove(child);

        if (spawn.RemainingTime <= 0 && spawn.livingChildren.Count() < _maxChildren) {
            var entity = Entity.Resolve(host.Manager, _children[Random.Next(0, _children.Count())]);
            entity.Move(host.X, host.Y);

            var enemyEntity = entity as Enemy;
            if (host is Enemy enemyHost && enemyEntity != null) {
                enemyEntity.Terrain = enemyHost.Terrain;
                if (enemyHost.Spawned) enemyEntity.Spawned = true;
            }

            host.Owner.EnterWorld(entity);
            spawn.RemainingTime = _coolDown.Next(Random);
            spawn.livingChildren.Add(enemyEntity);
        }
        else {
            spawn.RemainingTime -= time.ElapsedMsDelta;
        }
    }

    //State storage: Spawn state
    private class SpawnState {
        public List<Enemy> livingChildren;
        public int RemainingTime;
    }
}