using System.Xml.Linq;
using Shared;
using GameServer.realm;
using GameServer.realm.entities;

namespace GameServer.logic.transitions;

internal class OnParentDeathTransition : Transition {
    private bool init;
    private bool parentDead;

    public OnParentDeathTransition(XElement e)
        : base(e.ParseString("@targetState", "root")) { }

    public OnParentDeathTransition(string targetState)
        : base(targetState) {
        parentDead = false;
        init = false;
    }

    protected override bool TickCore(Entity host, RealmTime time, ref object state) {
        if (!init && host is Enemy) {
            init = true;
            var enemyHost = host as Enemy;
            if (enemyHost.ParentEntity != null)
                (host as Enemy).ParentEntity.OnDeath +=
                    (sender, e) => parentDead = true;
        }

        return parentDead;
    }
}