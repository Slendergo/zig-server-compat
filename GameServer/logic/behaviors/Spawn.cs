using System.Xml.Linq;
using Shared;
using GameServer.realm;
using GameServer.realm.entities;

namespace GameServer.logic.behaviors;

internal class Spawn : Behavior {
    private readonly ushort _children;
    private readonly bool _givesNoXp;
    private readonly int _initialSpawn;

    private readonly int _maxChildren;
    private Cooldown _coolDown;

    public Spawn(XElement e) {
        _children = GetObjType(e.ParseString("@children"));
        _maxChildren = e.ParseInt("@maxChildren", 5);
        _initialSpawn = (int) (_maxChildren * e.ParseFloat("@initialSpawn", 0.5f));
        _coolDown = new Cooldown().Normalize(e.ParseInt("@cooldown"));
        _givesNoXp = e.ParseBool("@givesNoXp", true);
    }

    public Spawn(string children, int maxChildren = 5, double initialSpawn = 0.5, Cooldown coolDown = new(),
        bool givesNoXp = true) {
        _children = GetObjType(children);
        _maxChildren = maxChildren;
        _initialSpawn = (int) (maxChildren * initialSpawn);
        _coolDown = coolDown.Normalize(0);
        _givesNoXp = givesNoXp;
    }

    protected override void OnStateEntry(Entity host, RealmTime time, ref object state) {
        state = new SpawnState {
            CurrentNumber = _initialSpawn,
            RemainingTime = _coolDown.Next(Random)
        };
        for (var i = 0; i < _initialSpawn; i++) {
            var entity = Entity.Resolve(host.Manager, _children);
            entity.Move(host.X, host.Y);

            var enemyHost = host as Enemy;
            var enemyEntity = entity as Enemy;

            entity.GivesNoXp = _givesNoXp;
            if (enemyHost != null && !entity.GivesNoXp)
                entity.GivesNoXp = enemyHost.GivesNoXp;

            if (enemyHost != null && enemyEntity != null) {
                enemyEntity.ParentEntity = host as Enemy;
                enemyEntity.Terrain = enemyHost.Terrain;
                if (enemyHost.Spawned) enemyEntity.Spawned = true;
            }

            host.Owner.EnterWorld(entity);
            (state as SpawnState).CurrentNumber++;
        }
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state) {
        if (state is not SpawnState spawn)
            return;

        if (spawn.RemainingTime <= 0 && spawn.CurrentNumber < _maxChildren) {
            var entity = Entity.Resolve(host.Manager, _children);
            entity.Move(host.X, host.Y);

            if (host is Enemy enemyHost && entity is Enemy enemyEntity) {
                enemyEntity.Terrain = enemyHost.Terrain;
                if (enemyHost.Spawned) enemyEntity.Spawned = true;
            }

            host.Owner.EnterWorld(entity);
            spawn.RemainingTime = _coolDown.Next(Random);
            spawn.CurrentNumber++;
        }
        else {
            spawn.RemainingTime -= time.ElapsedMsDelta;
        }
    }

    //State storage: Spawn state
    private class SpawnState {
        public int CurrentNumber;
        public int RemainingTime;
    }
}