using System.Xml.Linq;
using Shared;
using GameServer.realm;
using GameServer.realm.entities;
using GameServer.realm.entities.player;

namespace GameServer.logic.behaviors;

internal class Flash : Behavior {
    //State storage: none

    private uint color;
    private float flashPeriod;
    private int flashRepeats;

    public Flash(XElement e) {
        color = e.ParseUInt("@color");
        flashPeriod = e.ParseFloat("@flashPeriod");
        flashRepeats = e.ParseInt("@flashRepeats");
    }

    public Flash(uint color, double flashPeriod, int flashRepeats) {
        this.color = color;
        this.flashPeriod = (float) flashPeriod;
        this.flashRepeats = flashRepeats;
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state) { }

    protected override void OnStateEntry(Entity host, RealmTime time, ref object state) {
        foreach (var player in host.Owner.Players.Values)
            if (player.DistSqr(host) < Player.RadiusSqr)
                player.Client.SendShowEffect(EffectType.Flashing, host.Id,
                    new Position {X = flashPeriod, Y = flashRepeats}, new Position(), new ARGB(color));
    }
}