/*
    Copyright (C) 2015 creepylava

    This file is part of RotMG Dungeon Generator.

    RotMG Dungeon Generator is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.

*/

using DungeonGenerator.Dungeon;

namespace DungeonGenerator.Templates.Abyss;

internal class TreasureRoom : FixedRoom {
    private static readonly Tuple<Direction, int>[] connections = {
        Tuple.Create(Direction.South, 6)
    };

    public override RoomType Type => RoomType.Special;

    public override int Width => 15;

    public override int Height => 21;

    public override Tuple<Direction, int>[] ConnectionPoints => connections;

    public override void Rasterize(BitmapRasterizer<DungeonTile> rasterizer, Random rand) {
        rasterizer.Copy(AbyssTemplate.MapTemplate, new Rect(70, 10, 85, 31), Pos,
            tile => tile.TileType.Name == "Space");

        var bounds = Bounds;
        var buf = rasterizer.Bitmap;
        for (var x = bounds.X; x < bounds.MaxX; x++)
        for (var y = bounds.Y; y < bounds.MaxY; y++)
            if (buf[x, y].TileType != AbyssTemplate.Space)
                buf[x, y].Region = "Treasure";
    }
}