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


namespace DungeonGenerator.Templates.PirateCave;

public class PirateCaveTemplate : DungeonTemplate {
	internal static readonly TileType LightSand = new(0x00bd, "Light Sand");
	internal static readonly TileType BrownLines = new(0x000c, "Brown Lines");
	internal static readonly TileType ShallowWater = new(0x0073, "Shallow Water");
	internal static readonly TileType Composite = new(0x00fd, "Composite");
	internal static readonly TileType Space = new(0x00fe, "Space");

	internal static readonly ObjectType CaveWall = new(0x01ce, "Cave Wall");
	internal static readonly ObjectType PalmTree = new(0x018e, "Palm Tree");
	internal static readonly ObjectType CowardicePortal = new(0x0703, "Portal of Cowardice");

	internal static readonly ObjectType PirateKing = new(0x0927, "Dreadstump the Pirate King");

	internal static readonly ObjectType[] Boss = {
		new(0x683, "Pirate Lieutenant"),
		new(0x684, "Pirate Commander"),
		new(0x685, "Pirate Captain"),
		new(0x686, "Pirate Admiral")
	};

	internal static readonly ObjectType[] Minion = {
		new(0x687, "Cave Pirate Brawler"),
		new(0x688, "Cave Pirate Sailor"),
		new(0x689, "Cave Pirate Veteran")
	};

	internal static readonly ObjectType[] Pet = {
		new(0x68a, "Cave Pirate Moll"),
		new(0x68b, "Cave Pirate Parrot"),
		new(0x68c, "Cave Pirate Macaw"),
		new(0x68d, "Cave Pirate Monkey"),
		new(0x68e, "Cave Pirate Hunchback"),
		new(0x68f, "Cave Pirate Cabin Boy")
	};

	public override int MaxDepth { get { return 10; } }

	NormDist targetDepth;
	public override NormDist TargetDepth { get { return targetDepth; } }

	public override NormDist SpecialRmCount { get { return null; } }

	public override NormDist SpecialRmDepthDist { get { return null; } }

	public override Range RoomSeparation { get { return new Range(3, 7); } }

	public override int CorridorWidth { get { return 2; } }

	public override void Initialize() {
		targetDepth = new NormDist(1, 5.5f, 4, 7, Rand.Next());
	}

	public override Room CreateStart(int depth) {
		return new StartRoom(10);
	}

	public override Room CreateTarget(int depth, Room prev) {
		return new BossRoom(10);
	}

	public override Room CreateSpecial(int depth, Room prev) {
		throw new InvalidOperationException();
	}

	public override Room CreateNormal(int depth, Room prev) {
		return new NormalRoom(Rand.Next(8, 15), Rand.Next(8, 15));
	}

	public override MapCorridor CreateCorridor() {
		return new Corridor();
	}

	public override MapRender CreateBackground() {
		return new Background();
	}

	public override MapRender CreateOverlay() {
		return new Overlay();
	}
}