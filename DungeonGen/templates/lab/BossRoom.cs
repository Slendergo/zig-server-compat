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

internal class BossRoom : FixedRoom {
    private static readonly Rect template = new(0, 0, 24, 50);

    private static readonly Tuple<Direction, int>[] connections = {
        Tuple.Create(Direction.South, 10)
    };

    public override RoomType Type => RoomType.Target;

    public override int Width => template.MaxX - template.X;

    public override int Height => template.MaxY - template.Y;

    public override Tuple<Direction, int>[] ConnectionPoints => connections;

    public override void Rasterize(BitmapRasterizer<DungeonTile> rasterizer, Random rand) {
        rasterizer.Copy(LabTemplate.MapTemplate, template, Pos);
        LabTemplate.DrawSpiderWeb(rasterizer, Bounds, rand);
    }
}