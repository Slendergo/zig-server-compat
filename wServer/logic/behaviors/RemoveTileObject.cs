using System.Xml.Linq;
using common;
using wServer.realm;
using wServer.realm.entities;

namespace wServer.logic.behaviors;

internal class RemoveTileObject : Behavior {
    private readonly string _objName;
    private readonly int _range;

    public RemoveTileObject(XElement e) {
        _objName = e.ParseString("@type");
        _range = e.ParseInt("@range");
    }

    public RemoveTileObject(string objName, int range) {
        _objName = objName;
        _range = range;
    }

    protected override void OnStateEntry(Entity host, RealmTime time, ref object state) {
        var objType = GetObjType(_objName);

        var map = host.Owner.Map;

        var w = map.Width;
        var h = map.Height;

        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++) {
            var tile = map[x, y];

            if (tile.ObjectType != objType)
                continue;

            var dx = Math.Abs(x - (int) host.X);
            var dy = Math.Abs(y - (int) host.Y);

            if (dx > _range || dy > _range)
                continue;

            if (tile.ObjectDesc?.BlocksSight == true)
                foreach (var plr in host.Owner.Players.Values.Where(p =>
                             MathsUtils.DistSqr(p.X, p.Y, x, y) < Player.RadiusSqr))
                    plr.Sight.UpdateCount++;

            tile.ObjectType = 0;
            tile.UpdateCount++;
            map[x, y] = tile;
        }
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state) { }
}