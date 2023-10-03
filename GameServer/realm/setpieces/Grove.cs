﻿using GameServer.realm.worlds;

namespace GameServer.realm.setpieces;

internal class Grove : ISetPiece {
    private static readonly string Floor = "Light Grass";
    private static readonly string Tree = "Cherry Tree";

    private Random rand = new();

    public int Size => 25;

    public void RenderSetPiece(World world, IntPoint pos) {
        var radius = rand.Next(Size - 5, Size + 1) / 2;
        var border = new List<IntPoint>();

        var t = new int[Size, Size];
        for (var y = 0; y < Size; y++)
        for (var x = 0; x < Size; x++) {
            var dx = x - Size / 2.0;
            var dy = y - Size / 2.0;
            var r = Math.Sqrt(dx * dx + dy * dy);
            if (r <= radius) {
                t[x, y] = 1;
                if (radius - r < 1.5)
                    border.Add(new IntPoint(x, y));
            }
        }

        var trees = new HashSet<IntPoint>();
        while (trees.Count < border.Count * 0.5)
            trees.Add(border[rand.Next(0, border.Count)]);

        foreach (var i in trees)
            t[i.X, i.Y] = 2;

        var dat = world.Manager.Resources.GameData;
        for (var x = 0; x < Size; x++)
        for (var y = 0; y < Size; y++)
            if (t[x, y] == 1) {
                var tile = world.Map[x + pos.X, y + pos.Y];
                tile.TileId = dat.IdToTileType[Floor];
                tile.ObjectType = 0;
                tile.UpdateCount++;
            }
            else if (t[x, y] == 2) {
                var tile = world.Map[x + pos.X, y + pos.Y];
                tile.TileId = dat.IdToTileType[Floor];
                tile.ObjectType = dat.IdToObjectType[Tree];
                tile.ObjectDesc = dat.ObjectDescs[tile.ObjectType];
                tile.ObjectNameConfiguration = "size:" + (rand.Next() % 2 == 0 ? 120 : 140);
                if (tile.ObjectId == 0) tile.ObjectId = world.GetNextEntityId();
                tile.UpdateCount++;
            }

        var ent = Entity.Resolve(world.Manager, "Ent Ancient");
        ent.SetDefaultSize(140);
        ent.Move(pos.X + Size / 2 + 1, pos.Y + Size / 2 + 1);
        world.EnterWorld(ent);
    }
}