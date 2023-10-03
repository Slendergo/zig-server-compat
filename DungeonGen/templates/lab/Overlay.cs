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

using DungeonGen.dungeon;

namespace DungeonGen.templates.lab;

internal class Overlay : MapRender {
    public override void Rasterize() {
        var wall = new DungeonTile {
            TileType = LabTemplate.Space,
            Object = new DungeonObject {
                ObjectType = LabTemplate.LabWall
            }
        };

        int w = Rasterizer.Width, h = Rasterizer.Height;
        var buf = Rasterizer.Bitmap;
        for (var x = 0; x < w; x++)
        for (var y = 0; y < h; y++) {
            if (buf[x, y].TileType != LabTemplate.Space || buf[x, y].Object != null)
                continue;

            var isWall = false;
            if (x == 0 || y == 0 || x + 1 == w || y + 1 == h)
                isWall = false;
            else
                for (var dx = -1; dx <= 1 && !isWall; dx++)
                for (var dy = -1; dy <= 1 && !isWall; dy++)
                    if (buf[x + dx, y + dy].TileType != LabTemplate.Space) {
                        isWall = true;
                        break;
                    }

            if (isWall)
                buf[x, y] = wall;
        }
    }
}