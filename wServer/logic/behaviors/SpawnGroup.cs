using System.Xml.Linq;
using common;
using wServer.realm;
using wServer.realm.entities;

namespace wServer.logic.behaviors;

internal class SpawnGroup : Behavior {
    private ushort[] children;
    private Cooldown coolDown;
    private int initialSpawn;

    private int maxChildren;
    private double radius;

    public SpawnGroup(XElement e) {
        children = BehaviorDb.InitGameData.ObjectDescs.Values
            .Where(x => x.Group == e.ParseString("@group"))
            .Select(x => x.ObjectType).ToArray();
        maxChildren = e.ParseInt("@maxChildren", 5);
        initialSpawn = (int) (maxChildren * e.ParseFloat("@initialSpawn", 0.5f));
        coolDown = new Cooldown().Normalize(e.ParseInt("@cooldown", 1000));
        radius = e.ParseFloat("@radius");
    }

    public SpawnGroup(string group, int maxChildren = 5, double initialSpawn = 0.5, Cooldown coolDown = new(),
        double radius = 0) {
        children = BehaviorDb.InitGameData.ObjectDescs.Values
            .Where(x => x.Group == group)
            .Select(x => x.ObjectType).ToArray();
        this.maxChildren = maxChildren;
        this.initialSpawn = (int) (maxChildren * initialSpawn);
        this.coolDown = coolDown.Normalize(0);
        this.radius = radius;
    }

    protected override void OnStateEntry(Entity host, RealmTime time, ref object state) {
        state = new SpawnState {
            CurrentNumber = initialSpawn,
            RemainingTime = coolDown.Next(Random)
        };
        for (var i = 0; i < initialSpawn; i++) {
            var x = host.X + (float) (Random.NextDouble() * radius);
            var y = host.Y + (float) (Random.NextDouble() * radius);

            if (!host.Owner.IsPassable(x, y, true))
                continue;

            var entity = Entity.Resolve(host.Manager, children[Random.Next(children.Length)]);
            entity.Move(x, y);

            if (host is Enemy enemyHost && entity is Enemy enemyEntity) {
                enemyEntity.Terrain = enemyHost.Terrain;
                if (enemyHost.Spawned) enemyEntity.Spawned = true;
            }

            host.Owner.EnterWorld(entity);
        }
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state) {
        var spawn = (SpawnState) state;

        if (spawn.RemainingTime <= 0 && spawn.CurrentNumber < maxChildren) {
            var x = host.X + (float) (Random.NextDouble() * radius);
            var y = host.Y + (float) (Random.NextDouble() * radius);

            if (!host.Owner.IsPassable(x, y, true)) {
                spawn.RemainingTime = coolDown.Next(Random);
                spawn.CurrentNumber++;
                return;
            }

            var entity = Entity.Resolve(host.Manager, children[Random.Next(children.Length)]);
            entity.Move(x, y);

            if (host is Enemy enemyHost && entity is Enemy enemyEntity) {
                enemyEntity.Terrain = enemyHost.Terrain;
                if (enemyHost.Spawned) enemyEntity.Spawned = true;
            }

            host.Owner.EnterWorld(entity);
            spawn.RemainingTime = coolDown.Next(Random);
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