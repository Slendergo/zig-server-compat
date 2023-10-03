using System.Xml.Linq;
using Shared;
using GameServer.realm;
using GameServer.realm.entities;

namespace GameServer.logic.behaviors;

internal class RelativeSpawn : Behavior {
    private readonly ushort _children;
    private readonly int _initialSpawn;

    private readonly int _maxChildren;
    private readonly IntPoint _relativeTargetLoc;
    private Cooldown _coolDown;

    public RelativeSpawn(XElement e) {
        _maxChildren = e.ParseInt("@maxChildren", 5);
        _initialSpawn = (int) (_maxChildren * e.ParseFloat("@initialSpawn", 0.5f));
        _children = GetObjType(e.ParseString("@children"));
        _coolDown = new Cooldown().Normalize(e.ParseInt("@cooldown"));
        _relativeTargetLoc = new IntPoint(e.ParseInt("@x"), e.ParseInt("@y"));
    }

    public RelativeSpawn(string children, int x = 0, int y = 0, int maxChildren = 5, double initialSpawn = 0.5,
        Cooldown coolDown = new()) {
        _children = GetObjType(children);
        _maxChildren = maxChildren;
        _initialSpawn = (int) (maxChildren * initialSpawn);
        _coolDown = coolDown.Normalize(0);
        _relativeTargetLoc = new IntPoint(x, y);
    }

    protected override void OnStateEntry(Entity host, RealmTime time, ref object state) {
        state = new SpawnState {
            CurrentNumber = _initialSpawn,
            RemainingTime = _coolDown.Next(Random)
        };
        for (var i = 0; i < _initialSpawn; i++) {
            var entity = Entity.Resolve(host.Manager, _children);

            entity.Move(
                (float) (host.X + _relativeTargetLoc.X + .5),
                (float) (host.Y + _relativeTargetLoc.Y + .5));

            if (host is Enemy enemyHost && entity is Enemy enemyEntity) {
                enemyEntity.Terrain = enemyHost.Terrain;
                if (enemyHost.Spawned) enemyEntity.Spawned = true;
            }

            host.Owner.EnterWorld(entity);
            (state as SpawnState).CurrentNumber++;
        }
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state) {
        var spawn = (SpawnState) state;

        if (spawn.RemainingTime <= 0 && spawn.CurrentNumber < _maxChildren) {
            var x = host.X + _relativeTargetLoc.X + .5;
            var y = host.Y + _relativeTargetLoc.Y + .5;

            if (!host.Owner.IsPassable(x, y, true))
                return;

            var entity = Entity.Resolve(host.Manager, _children);

            entity.Move((float) x, (float) y);

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