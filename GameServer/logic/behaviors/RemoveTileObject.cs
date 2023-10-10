using System.Xml.Linq;
using Shared;
using GameServer.realm;
using GameServer.realm.entities;
using GameServer.realm.entities.player;

namespace GameServer.logic.behaviors;

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
                foreach (var player in host.Owner.Players.Values)
                    if (MathsUtils.DistSqr(player.X, player.Y, x, y) < Player.RADIUS_SQR)
                        player.Sight.UpdateVisibility();

                tile.ObjectType = 0;
            tile.UpdateCount++;
            map[x, y] = tile;
        }
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state) { }
}