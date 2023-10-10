using System.Xml.Linq;
using GameServer.realm;
using GameServer.realm.entities.player;
using Shared;
using Shared.resources;

namespace GameServer.logic.behaviors;

class RemoveObjectOnDeath : Behavior
{
    private readonly string _objName;
    private readonly int _range;

    public RemoveObjectOnDeath(XElement e)
    {
        _objName = e.ParseString("@type");
        _range = e.ParseInt("@range");
    }

    public RemoveObjectOnDeath(string objName, int range)
    {
        _objName = objName;
        _range = range;
    }

    protected internal override void Resolve(State parent)
    {
        parent.Death += (_, e) =>
        {
            XmlData dat = e.Host.Manager.Resources.GameData;
            var objType = dat.IdToObjectType[_objName];

            var map = e.Host.Owner.Map;

            var w = map.Width;
            var h = map.Height;

            for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                {
                    var tile = map[x, y];

                    if (tile.ObjectType != objType)
                        continue;

                    var dx = Math.Abs(x - (int)e.Host.X);
                    var dy = Math.Abs(y - (int)e.Host.Y);

                    if (dx > _range || dy > _range)
                        continue;

                    if (tile.ObjectDesc.BlocksSight)
                        foreach (var player in e.Host.Owner.Players.Values)
                            if (MathsUtils.DistSqr(player.X, player.Y, x, y) < Player.RADIUS_SQR)
                                player.Sight.UpdateVisibility();

                    tile.ObjectType = 0;
                    tile.UpdateCount++;
                    map[x, y] = tile;
                }
        };
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state)
    {
    }
}