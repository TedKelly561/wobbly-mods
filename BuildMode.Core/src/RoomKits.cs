using System;
using System.Collections.Generic;

namespace WobblyMods.BuildMode
{
    /// <summary>The ready-made pieces on the Build tab's "Room kits" page.</summary>
    public enum RoomKit
    {
        SmallRoom,
        BigRoom,
        Floor3x3,
        Floor5x5,
        WallLine3,
        WallLine5,
        StairsUp,
    }

    /// <summary>
    /// One block of a kit in KIT-LOCAL space: +Z is away from the player (the way they face),
    /// +X is to their right, +Y is up. The kit's anchor cell (the ghost) is local (0, 0, 0).
    /// <see cref="LocalRotation"/> is the block's own turn before the kit is turned to face the player.
    /// </summary>
    public struct KitPiece
    {
        public KitPiece(string typeId, GridPos cell, Rotation localRotation)
        {
            TypeId = typeId;
            Cell = cell;
            LocalRotation = localRotation;
        }

        public readonly string TypeId;
        public readonly GridPos Cell;
        public readonly Rotation LocalRotation;

        public override string ToString()
        {
            return TypeId + " at " + Cell + " " + LocalRotation;
        }
    }

    /// <summary>
    /// Builds the block lists for room kits (a whole room, a floor, a wall line, stairs) so a house goes
    /// up in a few presses instead of one piece at a time. Pure logic: the Unity layer gives the anchor
    /// cell (the ghost), the facing (<c>Rotation.FromDegrees(cameraYaw)</c>, the same as auto-turn), and
    /// the player's material and paint, then places the result with <see cref="BuildEditor.PlaceMany"/>
    /// (one undo step; blocked cells are skipped).
    ///
    /// Layout: kits are centred left-right on the anchor and run forward from it, so a room's front wall
    /// (with the door) is the side nearest the player. Walls sit in the edge cells of the floor (walls
    /// are drawn through the middle of their cell), with an L-shaped corner wall in each corner so there
    /// is no gap. Room walls are as tall as the door (2 levels by default), so the door fits flush.
    /// </summary>
    public static class RoomKits
    {
        /// <summary>Every kit in menu order.</summary>
        public static readonly RoomKit[] All =
        {
            RoomKit.SmallRoom, RoomKit.BigRoom, RoomKit.Floor3x3, RoomKit.Floor5x5,
            RoomKit.WallLine3, RoomKit.WallLine5, RoomKit.StairsUp,
        };

        /// <summary>Wall-like blocks run along local X at rotation 0; a quarter turn makes them run along Z.</summary>
        private static readonly Rotation Across = Rotation.None;
        private static readonly Rotation Along = new Rotation(1);

        /// <summary>Kid-friendly menu label.</summary>
        public static string DisplayName(RoomKit kit)
        {
            switch (kit)
            {
                case RoomKit.SmallRoom: return "Small room";
                case RoomKit.BigRoom: return "Big room with windows";
                case RoomKit.Floor3x3: return "Floor 3 x 3";
                case RoomKit.Floor5x5: return "Big floor 5 x 5";
                case RoomKit.WallLine3: return "Wall, 3 long";
                case RoomKit.WallLine5: return "Wall, 5 long";
                case RoomKit.StairsUp: return "Stairs up";
                default: return kit.ToString();
            }
        }

        /// <summary>True for kits that can have a flat roof.</summary>
        public static bool HasRoofOption(RoomKit kit)
        {
            return kit == RoomKit.SmallRoom || kit == RoomKit.BigRoom;
        }

        /// <summary>The kit's pieces in kit-local space. Uses the catalog's door height for room walls.</summary>
        public static List<KitPiece> Pieces(RoomKit kit, bool roof, BuildCatalog catalog)
        {
            int wallHeight = RoomWallHeight(catalog);
            switch (kit)
            {
                case RoomKit.SmallRoom: return Room(3, 3, wallHeight, wallHeight, false, roof);
                case RoomKit.BigRoom: return Room(5, 4, wallHeight, wallHeight, true, roof);
                case RoomKit.Floor3x3: return FloorRect(3, 3, 0);
                case RoomKit.Floor5x5: return FloorRect(5, 5, 0);
                case RoomKit.WallLine3: return WallLine(3);
                case RoomKit.WallLine5: return WallLine(5);
                case RoomKit.StairsUp: return StairsUp(3, 2);
                default: throw new ArgumentOutOfRangeException("kit");
            }
        }

        /// <summary>
        /// The kit as world blocks: each piece moved to <paramref name="anchor"/> + facing.Apply(cell) and
        /// turned by facing + its own turn. Unknown block ids (a trimmed catalog) fall back to a wall,
        /// or are left out if there's no wall either.
        /// </summary>
        public static List<BlockSpec> Build(RoomKit kit, bool roof, BuildCatalog catalog, BuildMaterial material, PaintColor paint,
                                            GridPos anchor, Rotation facing)
        {
            return ToSpecs(Pieces(kit, roof, catalog), catalog, material, paint, anchor, facing);
        }

        /// <summary>Turns kit-local pieces into world blocks (see <see cref="Build"/>).</summary>
        public static List<BlockSpec> ToSpecs(IList<KitPiece> pieces, BuildCatalog catalog, BuildMaterial material, PaintColor paint,
                                              GridPos anchor, Rotation facing)
        {
            if (pieces == null) throw new ArgumentNullException("pieces");
            if (catalog == null) throw new ArgumentNullException("catalog");
            if (material == null) throw new ArgumentNullException("material");
            var specs = new List<BlockSpec>(pieces.Count);
            for (int i = 0; i < pieces.Count; i++)
            {
                BlockType type = catalog.GetBlock(pieces[i].TypeId) ?? catalog.GetBlock(BuildCatalog.Ids.Wall);
                if (type == null)
                {
                    continue;
                }
                specs.Add(new BlockSpec(type, material, paint, ToWorld(anchor, facing, pieces[i].Cell), facing.Plus(pieces[i].LocalRotation)));
            }
            return specs;
        }

        /// <summary>A kit-local cell in the world.</summary>
        public static GridPos ToWorld(GridPos anchor, Rotation facing, GridPos local)
        {
            return anchor + facing.Apply(local);
        }

        /// <summary>Room walls match the door's height so the door fits flush (2 with the default catalog).</summary>
        public static int RoomWallHeight(BuildCatalog catalog)
        {
            BlockType door = catalog == null ? null : catalog.GetBlock(BuildCatalog.Ids.Door);
            return door == null ? 1 : Math.Max(1, door.Size.Y);
        }

        /// <summary>Left-most local X of a kit <paramref name="width"/> cells wide, centred on the anchor.</summary>
        public static int LeftX(int width)
        {
            return -((width - 1) / 2);
        }

        // ---------- Kit shapes ----------

        /// <summary>A floor <paramref name="width"/> across by <paramref name="depth"/> forward, at local level y.</summary>
        public static List<KitPiece> FloorRect(int width, int depth, int y)
        {
            CheckSize(width, depth);
            var list = new List<KitPiece>(width * depth);
            AddFloor(list, BuildCatalog.Ids.Floor, width, depth, y);
            return list;
        }

        /// <summary>A straight wall running forward from the anchor, one level high.</summary>
        public static List<KitPiece> WallLine(int length)
        {
            CheckSize(length, 1);
            var list = new List<KitPiece>(length);
            for (int z = 0; z < length; z++)
            {
                list.Add(new KitPiece(BuildCatalog.Ids.Wall, new GridPos(0, 0, z), Along));
            }
            return list;
        }

        /// <summary>
        /// Stairs at the anchor climbing forward one level, then a landing floor one level up just past
        /// the top step (<paramref name="landingWidth"/> across, <paramref name="landingDepth"/> forward).
        /// </summary>
        public static List<KitPiece> StairsUp(int landingWidth, int landingDepth)
        {
            CheckSize(landingWidth, landingDepth);
            var list = new List<KitPiece>(1 + landingWidth * landingDepth);
            list.Add(new KitPiece(BuildCatalog.Ids.Stairs, GridPos.Zero, Rotation.None));
            int left = LeftX(landingWidth);
            for (int z = 0; z < landingDepth; z++)
            {
                for (int x = left; x < left + landingWidth; x++)
                {
                    // The stairs cover local z 0 and 1, so the landing starts at z 2.
                    list.Add(new KitPiece(BuildCatalog.Ids.Floor, new GridPos(x, 1, 2 + z), Rotation.None));
                }
            }
            return list;
        }

        /// <summary>
        /// A room: floor, walls round the edge <paramref name="wallHeight"/> levels high, an L corner wall
        /// in each corner, a door in the middle of the front wall (the side nearest the player), optional
        /// windows on the ground level of both side walls, and an optional flat roof on top.
        /// <paramref name="doorHeight"/> is how many levels the door block covers (no wall goes there).
        /// Needs at least 3 across so the door has a middle cell between two corners.
        /// </summary>
        public static List<KitPiece> Room(int width, int depth, int wallHeight, int doorHeight, bool sideWindows, bool roof)
        {
            if (width < 3 || depth < 2)
            {
                throw new ArgumentOutOfRangeException("width", "A room needs at least 3 across and 2 deep.");
            }
            if (wallHeight < 1 || doorHeight < 1)
            {
                throw new ArgumentOutOfRangeException("wallHeight");
            }
            var list = new List<KitPiece>();
            AddFloor(list, BuildCatalog.Ids.Floor, width, depth, 0);

            int left = LeftX(width);
            int right = left + width - 1;
            int back = depth - 1;
            const int doorX = 0;

            // The door covers its column from the floor up (2 levels with the default catalog).
            list.Add(new KitPiece(BuildCatalog.Ids.Door, new GridPos(doorX, 0, 0), Across));

            for (int y = 0; y < wallHeight; y++)
            {
                for (int z = 0; z <= back; z++)
                {
                    for (int x = left; x <= right; x++)
                    {
                        bool edgeX = x == left || x == right;
                        bool edgeZ = z == 0 || z == back;
                        if (!edgeX && !edgeZ)
                        {
                            continue; // inside the room
                        }
                        var cell = new GridPos(x, y, z);
                        if (edgeX && edgeZ)
                        {
                            list.Add(new KitPiece(BuildCatalog.Ids.CornerWall, cell, CornerRotation(x == left, z == 0)));
                        }
                        else if (edgeZ)
                        {
                            if (z == 0 && x == doorX && y < doorHeight)
                            {
                                continue; // the door
                            }
                            list.Add(new KitPiece(BuildCatalog.Ids.Wall, cell, Across));
                        }
                        else
                        {
                            bool window = sideWindows && y == 0;
                            list.Add(new KitPiece(window ? BuildCatalog.Ids.Window : BuildCatalog.Ids.Wall, cell, Along));
                        }
                    }
                }
            }

            if (roof)
            {
                AddFloor(list, BuildCatalog.Ids.RoofFlat, width, depth, wallHeight);
            }
            return list;
        }

        /// <summary>
        /// The turn that points a corner wall's two arms (local +X and +Z at rotation 0) into the room:
        /// toward +X on the left side (-X on the right) and toward +Z at the front (-Z at the back).
        /// </summary>
        public static Rotation CornerRotation(bool leftSide, bool frontSide)
        {
            var inX = new GridPos(leftSide ? 1 : -1, 0, 0);
            var inZ = new GridPos(0, 0, frontSide ? 1 : -1);
            for (int t = 0; t < 4; t++)
            {
                var r = new Rotation(t);
                GridPos a = r.Apply(new GridPos(1, 0, 0));
                GridPos b = r.Apply(new GridPos(0, 0, 1));
                if ((a == inX && b == inZ) || (a == inZ && b == inX))
                {
                    return r;
                }
            }
            return Rotation.None; // unreachable: one of the four turns always matches
        }

        private static void AddFloor(List<KitPiece> list, string typeId, int width, int depth, int y)
        {
            int left = LeftX(width);
            for (int z = 0; z < depth; z++)
            {
                for (int x = left; x < left + width; x++)
                {
                    list.Add(new KitPiece(typeId, new GridPos(x, y, z), Rotation.None));
                }
            }
        }

        private static void CheckSize(int a, int b)
        {
            if (a < 1 || b < 1 || a > 64 || b > 64)
            {
                throw new ArgumentOutOfRangeException("a", "Kit sizes run from 1 to 64.");
            }
        }
    }
}
