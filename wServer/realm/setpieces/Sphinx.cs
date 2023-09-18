﻿using wServer.realm.entities;
using wServer.realm.worlds;

namespace wServer.realm.setpieces;

internal class Sphinx : ISetPiece {
    private static readonly byte[,] Center = {
        {0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0},
        {0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0},
        {0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0},
        {0, 0, 0, 1, 1, 1, 0, 1, 1, 1, 0, 1, 1, 1, 0, 0, 0},
        {0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0},
        {0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0},
        {0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0},
        {0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0},
        {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
        {0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0},
        {0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0},
        {0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0},
        {0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0},
        {0, 0, 0, 1, 1, 1, 0, 1, 1, 1, 0, 1, 1, 1, 0, 0, 0},
        {0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0},
        {0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0},
        {0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0}
    };

    private static readonly string Floor = "Gold Sand";
    private static readonly string Central = "Sand Tile";
    private static readonly string Pillar = "Tomb Wall";

    private Random rand = new();

    public int Size => 81;

    public void RenderSetPiece(World world, IntPoint pos) {
        var t = new int[81, 81];
        for (var x = 0; x < Size; x++) //Flooring
        for (var y = 0; y < Size; y++) {
            var dx = x - Size / 2.0;
            var dy = y - Size / 2.0;
            var r = Math.Sqrt(dx * dx + dy * dy) + rand.NextDouble() * 4 - 2;
            if (r <= 35)
                t[x, y] = 1;
        }

        for (var x = 0; x < 17; x++) //Center
        for (var y = 0; y < 17; y++)
            if (Center[x, y] != 0)
                t[32 + x, 32 + y] = 2;

        t[36, 36] = t[44, 36] = t[36, 44] = t[44, 44] = 3; //Pillars
        t[30, 30] = t[50, 30] = t[30, 50] = t[50, 50] = 4;

        t[40, 26] = t[40, 27] = t[39, 27] = t[41, 27] = 4;
        t[40, 54] = t[40, 53] = t[39, 53] = t[41, 53] = 4;
        t[26, 40] = t[27, 40] = t[27, 39] = t[27, 41] = 4;
        t[54, 40] = t[53, 40] = t[53, 39] = t[53, 41] = 4;

        var dat = world.Manager.Resources.GameData;
        for (var x = 0; x < Size; x++) //Rendering
        for (var y = 0; y < Size; y++)
            if (t[x, y] == 1) {
                var tile = world.Map[x + pos.X, y + pos.Y];
                tile.TileId = dat.IdToTileType[Floor];
                tile.ObjectType = 0;
                tile.UpdateCount++;
            }
            else if (t[x, y] == 2) {
                var tile = world.Map[x + pos.X, y + pos.Y];
                tile.TileId = dat.IdToTileType[Central];
                tile.ObjectType = 0;
                tile.UpdateCount++;
            }
            else if (t[x, y] == 3) {
                var tile = world.Map[x + pos.X, y + pos.Y];
                tile.TileId = dat.IdToTileType[Central];
                tile.ObjectType = dat.IdToObjectType[Pillar];
                tile.ObjectDesc = dat.ObjectDescs[tile.ObjectType];
                tile.ObjectNameConfiguration = ConnectionComputer.GetConnString((_x, _y) => t[x + _x, y + _y] == 3);
                if (tile.ObjectId == 0) tile.ObjectId = world.GetNextEntityId();
                tile.UpdateCount++;
            }
            else if (t[x, y] == 4) {
                var tile = world.Map[x + pos.X, y + pos.Y];
                tile.TileId = dat.IdToTileType[Floor];
                tile.ObjectType = dat.IdToObjectType[Pillar];
                tile.ObjectDesc = dat.ObjectDescs[tile.ObjectType];
                tile.ObjectNameConfiguration = ConnectionComputer.GetConnString((_x, _y) => t[x + _x, y + _y] == 4);
                if (tile.ObjectId == 0) tile.ObjectId = world.GetNextEntityId();
                tile.UpdateCount++;
            }

        var sphinx = Entity.Resolve(world.Manager, "Grand Sphinx");
        sphinx.Move(pos.X + 40.5f, pos.Y + 40.5f);
        world.EnterWorld(sphinx);
    }
}