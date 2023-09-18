using System.Xml.Linq;
using common;
using common.resources;
using wServer.realm;

namespace wServer.logic.behaviors;

internal class ConditionEffectRegion : Behavior {
    private readonly int _duration;
    private readonly ConditionEffectIndex[] _effects;
    private readonly int _range;

    public ConditionEffectRegion(XElement e) {
        _effects = e.ParseStringArray("@effects", ',', new[] {"Dead"})
            .Select(x => (ConditionEffectIndex) Enum.Parse(typeof(ConditionEffectIndex), x)).ToArray();
        _range = e.ParseInt("@range", 2);
        _duration = e.ParseInt("@duration", -1);
    }

    public ConditionEffectRegion(ConditionEffectIndex[] effects, int range = 2, int duration = -1) {
        _range = range;
        _effects = effects;
        _duration = duration;
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state) {
        if (host.Owner == null) return;

        if (host.HasConditionEffect(ConditionEffects.Paused))
            return;

        var hx = (int) host.X;
        var hy = (int) host.Y;

        var players = host.Owner.Players.Values
            .Where(p => Math.Abs(hx - (int) p.X) < _range && Math.Abs(hy - (int) p.Y) < _range);

        foreach (var player in players)
        foreach (var effect in _effects)
            player.ApplyConditionEffect(new ConditionEffect {
                Effect = effect,
                DurationMS = _duration
            });
    }
}