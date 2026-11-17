using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace WobblyMods.BuildMode.Tests
{
    public class RoomKitTests
    {
        private const int Skylar = 0;
        private static readonly BuildMaterial Wood = T.Catalog.GetMaterial("wood");
        private static readonly PaintColor Orange = new PaintColor(255, 128, 0);

        private static List<BlockSpec> Kit(RoomKit kit, bool roof, GridPos anchor, int turns)
        {
            return RoomKits.Build(kit, roof, T.Catalog, Wood, Orange, anchor, new Rotation(turns));
        }

        private static int Count(List<BlockSpec> specs, string id)
        {
            return specs.Count(s => s.Type.Id == id);
        }

        /// <summary>All (cell, layer) slots the specs cover; fails on any overlap within the kit.</summary>
        private static HashSet<(GridPos, OccupancyLayer)> Slots(List<BlockSpec> specs)
        {
            var slots = new HashSet<(GridPos, OccupancyLayer)>();
            foreach (BlockSpec s in specs)
            {
                foreach (GridPos c in Footprint.GetCells(s.Type, s.Anchor, s.Rotation))
                {
                    Assert.True(slots.Add((c, s.Type.Layer)), "overlap at " + c + " (" + s + ")");
                }
            }
            return slots;
        }

        public static IEnumerable<object[]> AllKitsAllTurns()
        {
            foreach (RoomKit kit in RoomKits.All)
            {
                for (int t = 0; t < 4; t++)
                {
                    yield return new object[] { kit, t, true };
                    yield return new object[] { kit, t, false };
                }
            }
        }

        [Theory]
        [MemberData(nameof(AllKitsAllTurns))]
        public void NoPieceOverlapsAnother_AndTheWholeKitPlaces(RoomKit kit, int turns, bool roof)
        {
            var anchor = new GridPos(7, 2, -4);
            List<BlockSpec> specs = Kit(kit, roof, anchor, turns);
            Slots(specs);

            var ed = new BuildEditor(T.Model());
            EditResult r = ed.PlaceMany(Skylar, specs, "Kit");
            Assert.True(r.Ok);
            Assert.Equal(specs.Count, r.BlocksChanged);
            Assert.All(specs, s => Assert.Same(Wood, s.Material));
            Assert.All(specs, s => Assert.Equal(Orange, s.Paint));

            // One undo step takes the whole kit away again.
            Assert.True(ed.Undo(Skylar).Ok);
            Assert.Equal(0, ed.Model.Count);
            Assert.Equal(BuildResult.NothingToUndo, ed.Undo(Skylar).Result);
        }

        [Theory]
        [InlineData(RoomKit.SmallRoom, false, 24)]  // 9 floor, 8 corner, 6 wall, 1 door
        [InlineData(RoomKit.SmallRoom, true, 33)]   // + 9 roof
        [InlineData(RoomKit.BigRoom, false, 47)]    // 20 floor, 8 corner, 14 wall, 4 window, 1 door
        [InlineData(RoomKit.BigRoom, true, 67)]     // + 20 roof
        [InlineData(RoomKit.Floor3x3, false, 9)]
        [InlineData(RoomKit.Floor5x5, true, 25)]    // roof option ignored
        [InlineData(RoomKit.WallLine3, false, 3)]
        [InlineData(RoomKit.WallLine5, false, 5)]
        [InlineData(RoomKit.StairsUp, false, 7)]    // stairs + 3 x 2 landing
        public void PieceCounts(RoomKit kit, bool roof, int expected)
        {
            Assert.Equal(expected, Kit(kit, roof, GridPos.Zero, 0).Count);
        }

        [Fact]
        public void SmallRoom_PieceTypes()
        {
            List<BlockSpec> s = Kit(RoomKit.SmallRoom, true, GridPos.Zero, 0);
            Assert.Equal(9, Count(s, "floor"));
            Assert.Equal(8, Count(s, "corner_wall"));
            Assert.Equal(6, Count(s, "wall"));
            Assert.Equal(1, Count(s, "door"));
            Assert.Equal(0, Count(s, "window"));
            Assert.Equal(9, Count(s, "roof_flat"));
        }

        [Fact]
        public void BigRoom_HasWindowsOnBothSides_AtGroundLevel()
        {
            List<BlockSpec> s = Kit(RoomKit.BigRoom, false, GridPos.Zero, 0);
            List<BlockSpec> windows = s.Where(b => b.Type.Id == "window").ToList();
            Assert.Equal(4, windows.Count);
            Assert.All(windows, w => Assert.Equal(0, w.Anchor.Y));
            Assert.Equal(2, windows.Count(w => w.Anchor.X == -2));
            Assert.Equal(2, windows.Count(w => w.Anchor.X == 2));
            // Side pieces run along the facing direction (a quarter turn from a front wall).
            Assert.All(windows, w => Assert.Equal(1, w.Rotation.QuarterTurns));
        }

        [Theory]
        [InlineData(RoomKit.SmallRoom, 3, 3)]
        [InlineData(RoomKit.BigRoom, 5, 4)]
        [InlineData(RoomKit.Floor3x3, 3, 3)]
        [InlineData(RoomKit.Floor5x5, 5, 5)]
        public void FloorFootprint_IsCentredOnTheGhost_AndRunsForward(RoomKit kit, int width, int depth)
        {
            var anchor = new GridPos(10, 0, 10);
            for (int t = 0; t < 4; t++)
            {
                var facing = new Rotation(t);
                var floors = new HashSet<GridPos>(Kit(kit, false, anchor, t).Where(b => b.Type.Id == "floor").Select(b => b.Anchor));
                Assert.Equal(width * depth, floors.Count);
                int left = RoomKits.LeftX(width);
                for (int z = 0; z < depth; z++)
                {
                    for (int x = left; x < left + width; x++)
                    {
                        Assert.Contains(anchor + facing.Apply(new GridPos(x, 0, z)), floors);
                    }
                }
                // The ghost cell is the front middle; nothing is built behind it (toward the player).
                Assert.Contains(anchor, floors);
                Assert.DoesNotContain(anchor + facing.Apply(new GridPos(0, 0, -1)), floors);
            }
        }

        [Theory]
        [InlineData(0, 0, 0, 1)]    // facing +Z: front wall is the row nearest the player
        [InlineData(1, 1, 0, 0)]    // facing +X
        [InlineData(2, 0, 0, -1)]   // facing -Z
        [InlineData(3, -1, 0, 0)]   // facing -X
        public void Door_IsInTheMiddleOfTheFrontWall_ForEachFacing(int turns, int fx, int fy, int fz)
        {
            var anchor = new GridPos(3, 1, -2);
            var forward = new GridPos(fx, fy, fz);
            Assert.Equal(forward, new Rotation(turns).Apply(new GridPos(0, 0, 1)));

            foreach (RoomKit kit in new[] { RoomKit.SmallRoom, RoomKit.BigRoom })
            {
                List<BlockSpec> s = Kit(kit, true, anchor, turns);
                BlockSpec door = Assert.Single(s, b => b.Type.Id == "door");
                Assert.Equal(anchor, door.Anchor);
                // Runs across the facing direction, like a front wall.
                Assert.Equal(turns, door.Rotation.QuarterTurns);

                // The doorway column has nothing else in its body layer, and the cell outside it is empty.
                var model = new BuildModel(T.Catalog);
                foreach (BlockSpec b in s) Assert.Equal(BuildResult.Ok, model.TryPlace(b));
                Assert.Same(model.GetBlockAt(anchor, OccupancyLayer.Body), model.GetBlockAt(anchor + new GridPos(0, 1, 0), OccupancyLayer.Body));
                Assert.Equal("door", model.GetBlockAt(anchor, OccupancyLayer.Body).Spec.Type.Id);
                Assert.Null(model.GetBlockAt(anchor - forward));

                // The cells either side of the door are front wall (or a corner), never open.
                GridPos rightDir = new Rotation(turns).Apply(new GridPos(1, 0, 0));
                Assert.NotNull(model.GetBlockAt(anchor + rightDir, OccupancyLayer.Body));
                Assert.NotNull(model.GetBlockAt(anchor - rightDir, OccupancyLayer.Body));

                // The back wall's middle is a wall, not a second door.
                int depth = kit == RoomKit.SmallRoom ? 3 : 4;
                GridPos backMiddle = anchor + new GridPos(forward.X * (depth - 1), 0, forward.Z * (depth - 1));
                Assert.Equal("wall", model.GetBlockAt(backMiddle, OccupancyLayer.Body).Spec.Type.Id);
            }
        }

        [Fact]
        public void RoomWalls_AreDoorHeight_AndRoofSitsOnTop()
        {
            int h = RoomKits.RoomWallHeight(T.Catalog);
            Assert.Equal(T.Catalog.GetBlock("door").Size.Y, h);
            List<BlockSpec> s = Kit(RoomKit.SmallRoom, true, GridPos.Zero, 0);
            Assert.All(s.Where(b => b.Type.Id == "roof_flat"), b => Assert.Equal(h, b.Anchor.Y));
            Assert.All(s.Where(b => b.Type.Id == "wall" || b.Type.Id == "corner_wall"), b => Assert.InRange(b.Anchor.Y, 0, h - 1));
            // Every edge cell of the room has a body piece on every wall level: no holes but the door.
            var model = new BuildModel(T.Catalog);
            foreach (BlockSpec b in s) model.TryPlace(b);
            for (int y = 0; y < h; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    for (int z = 0; z <= 2; z++)
                    {
                        bool edge = x != 0 || z != 1;
                        Assert.Equal(edge, model.IsOccupied(new GridPos(x, y, z), OccupancyLayer.Body));
                    }
                }
            }
        }

        [Theory]
        [InlineData(true, true, 0)]
        [InlineData(false, true, 3)]
        [InlineData(false, false, 2)]
        [InlineData(true, false, 1)]
        public void CornerWalls_PointBothArmsIntoTheRoom(bool left, bool front, int expectedTurns)
        {
            Rotation r = RoomKits.CornerRotation(left, front);
            Assert.Equal(expectedTurns, r.QuarterTurns);
            var arms = new HashSet<GridPos> { r.Apply(new GridPos(1, 0, 0)), r.Apply(new GridPos(0, 0, 1)) };
            Assert.Contains(new GridPos(left ? 1 : -1, 0, 0), arms);
            Assert.Contains(new GridPos(0, 0, front ? 1 : -1), arms);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void WallLine_RunsForwardFromTheGhost_AlongTheFacing(int turns)
        {
            var anchor = new GridPos(-5, 0, 2);
            var facing = new Rotation(turns);
            List<BlockSpec> s = Kit(RoomKit.WallLine5, false, anchor, turns);
            for (int i = 0; i < 5; i++)
            {
                Assert.Equal(anchor + facing.Apply(new GridPos(0, 0, i)), s[i].Anchor);
                Assert.Equal("wall", s[i].Type.Id);
                // The same turn auto-turn gives a wall: facing plus a quarter.
                Assert.Equal(facing.Clockwise(), s[i].Rotation);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void StairsUp_ClimbsForward_ToALandingOneLevelUp(int turns)
        {
            var anchor = new GridPos(0, 3, 0);
            var facing = new Rotation(turns);
            List<BlockSpec> s = Kit(RoomKit.StairsUp, false, anchor, turns);
            BlockSpec stairs = Assert.Single(s, b => b.Type.Id == "stairs");
            Assert.Equal(anchor, stairs.Anchor);
            Assert.Equal(facing, stairs.Rotation);
            List<BlockSpec> landing = s.Where(b => b.Type.Id == "floor").ToList();
            Assert.Equal(6, landing.Count);
            Assert.All(landing, b => Assert.Equal(anchor.Y + 1, b.Anchor.Y));
            // The landing starts just past the top step, in line with the stairs.
            Assert.Contains(landing, b => b.Anchor == anchor + facing.Apply(new GridPos(0, 1, 2)));
            var stairCells = Footprint.GetCells(stairs.Type, stairs.Anchor, stairs.Rotation);
            Assert.All(landing, b => Assert.DoesNotContain(b.Anchor - new GridPos(0, 1, 0), stairCells));
        }

        [Fact]
        public void BlockedCells_AreSkipped_AndTheRestIsOneUndoStep()
        {
            var ed = new BuildEditor(T.Model());
            // Something already in the way: a pillar where the small room's back-left corner goes.
            Assert.True(ed.Place(1, T.Spec("pillar", -1, 0, 2)).Ok);
            List<BlockSpec> specs = Kit(RoomKit.SmallRoom, false, GridPos.Zero, 0);
            EditResult r = ed.PlaceMany(Skylar, specs, "Small room");
            Assert.True(r.Ok);
            Assert.Equal(specs.Count - 1, r.BlocksChanged);
            Assert.Equal(specs.Count, ed.Model.Count); // the pillar plus every other kit piece

            Assert.True(ed.Undo(Skylar).Ok);
            Assert.Equal(1, ed.Model.Count);
            Assert.Equal("pillar", ed.Model.GetBlockAt(new GridPos(-1, 0, 2)).Spec.Type.Id);
        }

        [Fact]
        public void TrimmedCatalog_FallsBackToWalls()
        {
            var c = new BuildCatalog();
            c.Add(new BlockType("floor", "Floor", BlockKind.Floor, OccupancyLayer.Floor, 1, 1, 1));
            c.Add(new BlockType("wall", "Wall", BlockKind.Wall, OccupancyLayer.Body, 1, 1, 1));
            BuildMaterial m = new BuildMaterial("wood", "Wood", PaintColor.White, false);
            c.Add(m);
            List<BlockSpec> s = RoomKits.Build(RoomKit.SmallRoom, true, c, m, PaintColor.White, GridPos.Zero, Rotation.None);
            Assert.All(s, b => Assert.True(b.Type.Id == "floor" || b.Type.Id == "wall"));
            Assert.Equal(1, RoomKits.RoomWallHeight(c));
        }
    }
}
