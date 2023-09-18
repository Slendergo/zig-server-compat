using System.Xml.Linq;
using common;
using wServer.realm;
using wServer.realm.entities;

namespace wServer.logic.behaviors;

internal class TransferDamageOnDeath : Behavior {
    private readonly float _radius;
    private readonly ushort _target;

    public TransferDamageOnDeath(XElement e) {
        _target = GetObjType(e.ParseString("@target"));
        _radius = e.ParseFloat("@radius");
    }

    public TransferDamageOnDeath(string target, float radius = 50) {
        _target = GetObjType(target);
        _radius = radius;
    }

    protected internal override void Resolve(State parent) {
        parent.Death += (sender, e) => {
            if (e.Host is not Enemy enemy)
                return;

            if (e.Host.GetNearestEntity(_radius, _target) is not Enemy targetObj)
                return;

            enemy.DamageCounter.TransferData(targetObj.DamageCounter);
        };
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state) { }
}