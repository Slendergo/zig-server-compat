using common;
using common.resources;
using System.Xml.Linq;
using wServer.realm;
using wServer.realm.entities;

namespace wServer.logic.behaviors;

class Spawn : Behavior
{
    //State storage: Spawn state
    class SpawnState
    {
        public int CurrentNumber;
        public int RemainingTime;
    }

    private readonly int _maxChildren;
    private readonly int _initialSpawn;
    private Cooldown _coolDown;
    private readonly ushort _children;
    private readonly bool _givesNoXp;

    public Spawn(XElement e)
    {
        _children = GetObjType(e.ParseString("@children"));
        _maxChildren = e.ParseInt("@maxChildren", 5);
        _initialSpawn = (int)(_maxChildren * e.ParseFloat("@initialSpawn", 0.5f));
        _coolDown = new Cooldown().Normalize(e.ParseInt("@cooldown"));
        _givesNoXp = e.ParseBool("@givesNoXp", true);
    }

    public Spawn(string children, int maxChildren = 5, double initialSpawn = 0.5, Cooldown coolDown = new(), bool givesNoXp = true)
    {
        _children = GetObjType(children);
        _maxChildren = maxChildren;
        _initialSpawn = (int)(maxChildren * initialSpawn);
        _coolDown = coolDown.Normalize(0);
        _givesNoXp = givesNoXp;
    }

    protected override void OnStateEntry(Entity host, RealmTime time, ref object state)
    {   
        state = new SpawnState()
        {
            CurrentNumber = _initialSpawn,
            RemainingTime = _coolDown.Next(Random)
        };
        for (int i = 0; i < _initialSpawn; i++)
        {
            Entity entity = Entity.Resolve(host.Manager, _children);
            entity.Move(host.X, host.Y);

            var enemyHost = host as Enemy;
            var enemyEntity = entity as Enemy;

            entity.GivesNoXp = _givesNoXp;
            if (enemyHost != null && !entity.GivesNoXp)
                entity.GivesNoXp = enemyHost.GivesNoXp;

            if (enemyHost != null && enemyEntity != null)
            {
                enemyEntity.ParentEntity = host as Enemy;
                enemyEntity.Terrain = enemyHost.Terrain;
                if (enemyHost.Spawned)
                {
                    enemyEntity.Spawned = true;
                }
            }

            host.Owner.EnterWorld(entity);
            (state as SpawnState).CurrentNumber++;
        }
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state)
    {
        var spawn = state as SpawnState;

        if (spawn == null)
            return;

        if (spawn.RemainingTime <= 0 && spawn.CurrentNumber < _maxChildren)
        {
            Entity entity = Entity.Resolve(host.Manager, _children);
            entity.Move(host.X, host.Y);

            var enemyHost = host as Enemy;
            var enemyEntity = entity as Enemy;
            if (enemyHost != null && enemyEntity != null)
            {
                enemyEntity.Terrain = enemyHost.Terrain;
                if (enemyHost.Spawned)
                {
                    enemyEntity.Spawned = true;
                }
            }

            host.Owner.EnterWorld(entity);
            spawn.RemainingTime = _coolDown.Next(Random);
            spawn.CurrentNumber++;
        }
        else
            spawn.RemainingTime -= time.ElaspedMsDelta;
    }
}