using common;
using common.resources;
using System.Xml.Linq;
using wServer.realm;

namespace wServer.logic.behaviors;

class RemoveConditionalEffect : Behavior
{
    //State storage: none

    readonly ConditionEffectIndex _effect;

    public RemoveConditionalEffect(XElement e)
    {
        _effect = e.ParseConditionEffect("@effect");
    }

    public RemoveConditionalEffect(ConditionEffectIndex effect)
    {
        _effect = effect;
    }

    protected override void OnStateEntry(Entity host, RealmTime time, ref object state)
    {
        host.ApplyConditionEffect(new ConditionEffect()
        {
            Effect = _effect,
            DurationMS = 0
        });
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state) { }
}