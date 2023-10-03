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

namespace DungeonGen.templates.pirateCave;

internal class NormalRoom : Room {
    public NormalRoom(int w, int h) {
        Width = w;
        Height = h;
    }

    public override RoomType Type => RoomType.Normal;

    public override int Width { get; }

    public override int Height { get; }

    public override void Rasterize(BitmapRasterizer<DungeonTile> rasterizer, Random rand) {
        rasterizer.FillRect(Bounds, new DungeonTile {
            TileType = PirateCaveTemplate.BrownLines
        });

        var numBoss = new Range(0, 1).Random(rand);
        var numMinion = new Range(3, 5).Random(rand);
        var numPet = new Range(0, 2).Random(rand);

        var buf = rasterizer.Bitmap;
        var bounds = Bounds;
        while (numBoss > 0 || numMinion > 0 || numPet > 0) {
            var x = rand.Next(bounds.X, bounds.MaxX);
            var y = rand.Next(bounds.Y, bounds.MaxY);
            if (buf[x, y].Object != null)
                continue;

            switch (rand.Next(3)) {
                case 0:
                    if (numBoss > 0) {
                        buf[x, y].Object = new DungeonObject {
                            ObjectType = PirateCaveTemplate.Boss[rand.Next(PirateCaveTemplate.Boss.Length)]
                        };
                        numBoss--;
                    }

                    break;
                case 1:
                    if (numMinion > 0) {
                        buf[x, y].Object = new DungeonObject {
                            ObjectType = PirateCaveTemplate.Minion[rand.Next(PirateCaveTemplate.Minion.Length)]
                        };
                        numMinion--;
                    }

                    break;
                case 2:
                    if (numPet > 0) {
                        buf[x, y].Object = new DungeonObject {
                            ObjectType = PirateCaveTemplate.Pet[rand.Next(PirateCaveTemplate.Pet.Length)]
                        };
                        numPet--;
                    }

                    break;
            }
        }
    }
}