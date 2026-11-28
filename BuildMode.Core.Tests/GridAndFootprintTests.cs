using System.Collections.Generic;
using Xunit;

namespace WobblyMods.BuildMode.Tests
{
    public class GridTests
    {
        [Theory]
        [InlineData(0.0f, 0.0f, 0.0f, 0, 0, 0)]
        [InlineData(0.99f, 1.5f, 2.01f, 0, 1, 2)]
        [InlineData(-0.01f, -1.0f, -1.01f, -1, -1, -2)]
        [InlineData(2.9999999f, 0f, 0f, 3, 0, 0)] // float noise on a boundary snaps to the cell above
        public void WorldToCell_FloorsWith1mCells(float x, float y, float z, int cx, int cy, int cz)
        {
            var grid = new GridSettings();
            Assert.Equal(new GridPos(cx, cy, cz), grid.WorldToCell(new Vec3(x, y, z)));
        }

        [Fact]
        public void HalfStepGrid_UsesHalfMetreCells()
        {
            GridSettings grid = GridSettings.HalfStep();
            Assert.Equal(new GridPos(3, 0, -1), grid.WorldToCell(new Vec3(1.6f, 0.4f, -0.2f)));
            Assert.Equal(new Vec3(1.75f, 0.25f, -0.25f), grid.CellCenter(new GridPos(3, 0, -1)));
        }

        [Fact]
        public void Origin_ShiftsTheGrid()
        {
            var grid = new GridSettings(1f, new Vec3(10f, 0f, -5f));
            Assert.Equal(new GridPos(0, 0, 0), grid.WorldToCell(new Vec3(10.5f, 0.5f, -4.5f)));
            Assert.Equal(new Vec3(10.5f, 0f, -4.5f), grid.BlockPivot(GridPos.Zero));
            Assert.Equal(new Vec3(12f, 1f, -2f), grid.CellMinCorner(new GridPos(2, 1, 3)));
        }

        [Fact]
        public void SnapToCellCenter()
        {
            var grid = new GridSettings();
            Assert.Equal(new Vec3(2.5f, 0.5f, -0.5f), grid.SnapToCellCenter(new Vec3(2.1f, 0.9f, -0.1f)));
        }

        [Fact]
        public void CellAgainstSurface_PicksNeighbourCell()
        {
            var grid = new GridSettings();
            // Aiming at the top of a floor block in cell (0,0,0): hit at y = 1, normal up.
            Assert.Equal(new GridPos(0, 1, 0), grid.CellAgainstSurface(new Vec3(0.3f, 1f, 0.7f), new Vec3(0, 1, 0)));
            // Aiming at the +X side of a wall in cell (0,0,0).
            Assert.Equal(new GridPos(1, 0, 0), grid.CellAgainstSurface(new Vec3(1f, 0.5f, 0.5f), new Vec3(1, 0, 0)));
            Assert.Equal(new GridPos(0, 0, 0), grid.CellBehindSurface(new Vec3(1f, 0.5f, 0.5f), new Vec3(1, 0, 0)));
        }

        [Fact]
        public void CellBehindSurface_PicksTheCellOfTheSolidThatWasHit()
        {
            var grid = new GridSettings();
            // The top of a full block in cell (0,0,0): hit at y = 1, normal up.
            Assert.Equal(new GridPos(0, 0, 0), grid.CellBehindSurface(new Vec3(0.3f, 1f, 0.7f), new Vec3(0, 1, 0)));
            // Its underside, seen from below.
            Assert.Equal(new GridPos(0, 0, 0), grid.CellBehindSurface(new Vec3(0.3f, 0f, 0.7f), new Vec3(0, -1, 0)));
            // Its -Z side, in a cell with negative numbers.
            Assert.Equal(new GridPos(-2, 0, -1), grid.CellBehindSurface(new Vec3(-1.5f, 0.5f, -1f), new Vec3(0, 0, -1)));

            // Bigger pieces and a moved grid: the +X side of cell (1,0,0), which spans x = 12 to 14.
            var big = new GridSettings(2f, new Vec3(10f, 5f, 0f));
            Assert.Equal(new GridPos(1, 0, 0), big.CellBehindSurface(new Vec3(14f, 6f, 1f), new Vec3(1, 0, 0)));
            // It is always the opposite step to CellAgainstSurface.
            Assert.Equal(new GridPos(2, 0, 0), big.CellAgainstSurface(new Vec3(14f, 6f, 1f), new Vec3(1, 0, 0)));
        }

        [Fact]
        public void CellBehindSurface_GoesTooFarForThinPieces()
        {
            // A floor is a thin slab at the bottom of its cell. With 2 m pieces its top is 0.2 m up, and half
            // a cell (1 m) behind that is the cell BELOW the floor. So this can't be trusted to find the piece
            // someone is looking at; the game layer asks BlockPickMap instead (see PickMapTests).
            var grid = new GridSettings(2f);
            Assert.Equal(new GridPos(0, -1, 0), grid.CellBehindSurface(new Vec3(1f, 0.2f, 1f), new Vec3(0, 1, 0)));
        }

        [Fact]
        public void BadCellSize_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new GridSettings(0f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new GridSettings(float.NaN));
        }
    }

    public class RotationTests
    {
        [Theory]
        [InlineData(0f, 0)]
        [InlineData(44f, 0)]
        [InlineData(46f, 1)]
        [InlineData(90f, 1)]
        [InlineData(180f, 2)]
        [InlineData(269f, 3)]
        [InlineData(359f, 0)]
        [InlineData(-90f, 3)]
        [InlineData(-170f, 2)]
        [InlineData(720f + 95f, 1)]
        [InlineData(float.NaN, 0)]
        public void FromDegrees_SnapsToNearestQuarterTurn(float degrees, int turns)
        {
            Assert.Equal(turns, Rotation.FromDegrees(degrees).QuarterTurns);
        }

        [Fact]
        public void Turns_WrapAround()
        {
            Assert.Equal(0, new Rotation(4).QuarterTurns);
            Assert.Equal(3, new Rotation(-1).QuarterTurns);
            Assert.Equal(0, new Rotation(3).Clockwise().QuarterTurns);
            Assert.Equal(3, Rotation.None.CounterClockwise().QuarterTurns);
            Assert.Equal(270, new Rotation(3).Degrees);
        }

        [Fact]
        public void Apply_MatchesUnityYaw()
        {
            // Unity: Quaternion.Euler(0, 90, 0) * forward = right, * right = back.
            var r = new Rotation(1);
            Assert.Equal(new GridPos(1, 0, 0), r.Apply(new GridPos(0, 0, 1)));
            Assert.Equal(new GridPos(0, 0, -1), r.Apply(new GridPos(1, 0, 0)));
            Assert.Equal(new GridPos(0, 5, 0), r.Apply(new GridPos(0, 5, 0)));
            Assert.Equal(new Vec3(1f, 0f, 0f), r.Apply(new Vec3(0f, 0f, 1f)));
        }

        [Fact]
        public void FourTurns_IsIdentity()
        {
            var p = new GridPos(2, 1, 3);
            GridPos q = p;
            var r = new Rotation(1);
            for (int i = 0; i < 4; i++)
            {
                q = r.Apply(q);
            }
            Assert.Equal(p, q);
        }
    }

    public class FootprintTests
    {
        private static readonly BuildCatalog Catalog = BuildCatalog.CreateDefault();

        [Fact]
        public void SingleCell_IsJustTheAnchor()
        {
            List<GridPos> cells = Footprint.GetCells(Catalog.GetBlock("wall"), new GridPos(4, 2, -3), new Rotation(2));
            Assert.Equal(new[] { new GridPos(4, 2, -3) }, cells);
        }

        [Fact]
        public void Door_IsTwoTall()
        {
            List<GridPos> cells = Footprint.GetCells(Catalog.GetBlock("door"), new GridPos(1, 0, 1), new Rotation(1));
            Assert.Equal(new[] { new GridPos(1, 0, 1), new GridPos(1, 1, 1) }, cells);
        }

        [Theory]
        [InlineData(0, 0, 0, 2, 0)]   // beam 3 long along +X
        [InlineData(1, 0, -2, 0, 0)]  // a quarter turn: along -Z
        [InlineData(2, -2, 0, 0, 0)]  // along -X
        [InlineData(3, 0, 0, 0, 2)]   // along +Z
        public void Beam_RotatesAroundAnchor(int turns, int minX, int minZ, int maxX, int maxZ)
        {
            BlockType beam = Catalog.GetBlock("beam");
            List<GridPos> cells = Footprint.GetCells(beam, GridPos.Zero, new Rotation(turns));
            Assert.Equal(3, cells.Count);
            Assert.Contains(GridPos.Zero, cells); // the anchor never moves

            GridPos min, max;
            Footprint.GetBounds(beam, GridPos.Zero, new Rotation(turns), out min, out max);
            Assert.Equal(new GridPos(minX, 0, minZ), min);
            Assert.Equal(new GridPos(maxX, 0, maxZ), max);
            foreach (GridPos c in cells)
            {
                Assert.InRange(c.X, minX, maxX);
                Assert.InRange(c.Z, minZ, maxZ);
            }
        }

        [Fact]
        public void Stairs_DepthTurnsWithRotation()
        {
            BlockType stairs = Catalog.GetBlock("stairs"); // 1 x 1 x 2 (deep along Z)
            Assert.Equal(new[] { new GridPos(5, 0, 5), new GridPos(5, 0, 6) },
                Footprint.GetCells(stairs, new GridPos(5, 0, 5), Rotation.None));
            Assert.Equal(new[] { new GridPos(5, 0, 5), new GridPos(6, 0, 5) },
                Footprint.GetCells(stairs, new GridPos(5, 0, 5), new Rotation(1)));
        }

        [Fact]
        public void Custom2x2x2_HasEightDistinctCellsInEveryRotation()
        {
            var big = new BlockType("big_cube", "Big cube", BlockKind.Wall, OccupancyLayer.Body, 2, 2, 2);
            for (int t = 0; t < 4; t++)
            {
                var cells = new HashSet<GridPos>(Footprint.GetCells(big, new GridPos(10, 0, 10), new Rotation(t)));
                Assert.Equal(8, cells.Count);
            }
        }
    }

    public class CatalogTests
    {
        [Fact]
        public void Default_HasAllRequestedBlocksAndMaterials()
        {
            BuildCatalog c = BuildCatalog.CreateDefault();
            foreach (string id in new[] { "floor", "wall", "half_wall", "roof_slope", "roof_flat", "stairs", "door", "window", "pillar", "beam" })
            {
                Assert.NotNull(c.GetBlock(id));
            }
            foreach (string id in new[] { "wood", "brick", "stone", "glass", "concrete", "roof_tile", "plaster" })
            {
                Assert.NotNull(c.GetMaterial(id));
            }
            Assert.True(c.GetMaterial("glass").IsTransparent);
            Assert.Null(c.GetBlock("nope"));
            Assert.Null(c.GetMaterial(null));
        }

        [Fact]
        public void DuplicateOrBadIds_Throw()
        {
            BuildCatalog c = BuildCatalog.CreateDefault();
            Assert.Throws<System.ArgumentException>(() => c.Add(new BlockType("wall", "Again", BlockKind.Wall, OccupancyLayer.Body, 1, 1, 1)));
            Assert.Throws<System.ArgumentException>(() => new BuildMaterial("Has Space", "x", PaintColor.White, false));
        }

        [Theory]
        [InlineData("#FF8000", 255, 128, 0, 255)]
        [InlineData("#11223344", 0x11, 0x22, 0x33, 0x44)]
        [InlineData("aabbcc", 0xAA, 0xBB, 0xCC, 255)]
        public void PaintColor_ParsesHex(string hex, int r, int g, int b, int a)
        {
            PaintColor c;
            Assert.True(PaintColor.TryParseHex(hex, out c));
            Assert.Equal(new PaintColor((byte)r, (byte)g, (byte)b, (byte)a), c);
        }

        [Theory]
        [InlineData("")]
        [InlineData("#FFF")]
        [InlineData("#GG0000")]
        [InlineData(null)]
        public void PaintColor_RejectsBadHex(string hex)
        {
            PaintColor c;
            Assert.False(PaintColor.TryParseHex(hex, out c));
        }

        [Fact]
        public void PaintColor_HexRoundTrips()
        {
            var c = new PaintColor(1, 2, 254, 128);
            PaintColor back;
            Assert.True(PaintColor.TryParseHex(c.ToHex(), out back));
            Assert.Equal(c, back);
            Assert.Equal("#0102FE80", c.ToHex());
        }
    }
}
