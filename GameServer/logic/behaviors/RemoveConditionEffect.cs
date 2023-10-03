using System.Xml.Linq;
using Shared;
using Shared.resources;
using GameServer.realm;

namespace GameServer.logic.behaviors;

internal class RemoveConditionalEffect : Behavior {
    //State storage: none

    private readonly ConditionEffectIndex _effect;

    public RemoveConditionalEffect(XElement e) {
        _effect = e.ParseConditionEffect("@effect");
    }

    public RemoveConditionalEffect(ConditionEffectIndex effect) {
        _effect = effect;
    }

    protected override void OnStateEntry(Entity host, RealmTime time, ref object state) {
        host.ApplyConditionEffect(new ConditionEffect {
            Effect = _effect,
            DurationMS = 0
        });
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state) { }
}