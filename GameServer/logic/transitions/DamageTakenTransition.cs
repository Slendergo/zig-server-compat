using System.Xml.Linq;
using GameServer.realm;
using GameServer.realm.entities;
using Shared;

namespace GameServer.logic.transitions; 

public class DamageTakenTransition : Transition {
    //State storage: none

    private readonly int _damage;
    private readonly bool _wipeProgress;
    
    public DamageTakenTransition(XElement e)
        : base(e.ParseString("@targetState", "root")) {
        _damage = e.ParseInt("@damage", 1);
        _wipeProgress = e.ParseBool("@wipeProgress");
    }

    public DamageTakenTransition(int damage, string targetState, bool wipeProgress = false)
        : base(targetState) {
        _damage = damage;
        _wipeProgress = wipeProgress;
    }

    protected override bool TickCore(Entity host, RealmTime time, ref object state) {
        var damageSoFar = 0;
        if (_wipeProgress)
            damageSoFar = 0;

        var enemy = (Enemy) host;
        if (enemy == null)
            return false;

        foreach (var i in enemy.DamageCounter.GetPlayerData())
            damageSoFar += i.Item2;

        return damageSoFar >= _damage;
    }
}