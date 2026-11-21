using System.Collections.Generic;
using Xunit;

namespace WobblyMods.BuildMode.Tests
{
    internal static class T
    {
        public static readonly BuildCatalog Catalog = BuildCatalog.CreateDefault();

        public static BlockSpec Spec(string block, int x, int y, int z, int turns = 0, string material = "wood")
        {
            BuildMaterial m = Catalog.GetMaterial(material);
            return new BlockSpec(Catalog.GetBlock(block), m, m.DefaultColor, new GridPos(x, y, z), new Rotation(turns));
        }

        public static BuildModel Model()
        {
            return new BuildModel(Catalog);
        }
    }

    public class OccupancyTests
    {
        [Fact]
        public void Place_ThenQueryEveryCell()
        {
            BuildModel m = T.Model();
            PlacedBlock door;
            Assert.Equal(BuildResult.Ok, m.TryPlace(T.Spec("door", 2, 0, 3), out door));
            Assert.Same(door, m.GetBlockAt(new GridPos(2, 0, 3)));
            Assert.Same(door, m.GetBlockAt(new GridPos(2, 1, 3)));
            Assert.Null(m.GetBlockAt(new GridPos(2, 2, 3)));
            Assert.Same(door, m.GetBlock(door.Id));
            Assert.Equal(1, m.Count);
        }

        [Fact]
        public void Overlap_IsRejected_AndModelUnchanged()
        {
            BuildModel m = T.Model();
            PlacedBlock beam;
            m.TryPlace(T.Spec("beam", 0, 0, 0), out beam); // cells x = 0..2

            int blocking;
            Assert.Equal(BuildResult.Overlap, m.CanPlace(T.Catalog.GetBlock("wall"), new GridPos(2, 0, 0), Rotation.None, out blocking));
            Assert.Equal(beam.Id, blocking);

            // A second beam turned so it crosses the first one's last cell.
            Assert.Equal(BuildResult.Overlap, m.TryPlace(T.Spec("beam", 2, 0, 2, turns: 1))); // cells z = 2, 1, 0 at x = 2
            Assert.Equal(1, m.Count);
            Assert.Null(m.GetBlockAt(new GridPos(2, 0, 2)));
            Assert.Equal(BuildResult.Ok, m.TryPlace(T.Spec("beam", 3, 0, 2, turns: 1)));
        }

        [Fact]
        public void FloorAndBodyLayers_ShareACell()
        {
            BuildModel m = T.Model();
            Assert.Equal(BuildResult.Ok, m.TryPlace(T.Spec("floor", 0, 0, 0)));
            Assert.Equal(BuildResult.Ok, m.TryPlace(T.Spec("wall", 0, 0, 0)));
            Assert.Equal(BuildResult.Overlap, m.TryPlace(T.Spec("roof_flat", 0, 0, 0)));
            Assert.Equal(BuildResult.Overlap, m.TryPlace(T.Spec("pillar", 0, 0, 0)));
            Assert.Equal("wall", m.GetBlockAt(GridPos.Zero).Spec.Type.Id); // body wins for "what's here"
            Assert.Equal("floor", m.GetBlockAt(GridPos.Zero, OccupancyLayer.Floor).Spec.Type.Id);
        }

        [Fact]
        public void Remove_FreesAllCells()
        {
            BuildModel m = T.Model();
            PlacedBlock stairs;
            m.TryPlace(T.Spec("stairs", 0, 0, 0), out stairs);
            Assert.True(m.Remove(stairs.Id));
            Assert.False(m.Remove(stairs.Id));
            Assert.False(m.IsOccupied(new GridPos(0, 0, 1), OccupancyLayer.Body));
            Assert.Equal(BuildResult.Ok, m.TryPlace(T.Spec("wall", 0, 0, 1)));
        }

        [Fact]
        public void HeightLimitsAndBlockCap()
        {
            BuildModel m = T.Model();
            m.MaxY = 5;
            Assert.Equal(BuildResult.OutOfBounds, m.TryPlace(T.Spec("door", 0, 5, 0))); // top half at y = 6
            Assert.Equal(BuildResult.Ok, m.TryPlace(T.Spec("door", 0, 4, 0)));
            m.MaxBlocks = 1;
            Assert.Equal(BuildResult.LimitReached, m.TryPlace(T.Spec("wall", 9, 0, 9)));
        }

        [Fact]
        public void SetLook_ChangesMaterialAndPaint()
        {
            BuildModel m = T.Model();
            PlacedBlock wall;
            m.TryPlace(T.Spec("wall", 0, 0, 0), out wall);
            var red = new PaintColor(255, 0, 0);
            Assert.Equal(BuildResult.Ok, m.SetLook(wall.Id, T.Catalog.GetMaterial("brick"), red));
            Assert.Equal("brick", wall.Spec.Material.Id);
            Assert.Equal(red, wall.Spec.Paint);
            Assert.Equal(BuildResult.NotFound, m.SetLook(999, T.Catalog.GetMaterial("brick"), red));
        }

        [Fact]
        public void BigBuild_HundredThousandBlocks()
        {
            BuildModel m = T.Model();
            int placed = 0;
            for (int x = 0; x < 100; x++)
            {
                for (int z = 0; z < 100; z++)
                {
                    for (int y = 0; y < 10; y++)
                    {
                        if (m.TryPlace(T.Spec("wall", x, y, z)) == BuildResult.Ok)
                        {
                            placed++;
                        }
                    }
                }
            }
            Assert.Equal(100000, placed);
            Assert.Equal(100000, m.Count);
            // 100 x 10 x 100 cells = 7 x 1 x 7 chunks.
            Assert.Equal(49, m.DirtyChunkCount);
            Assert.Equal(16 * 16 * 10, m.CountInChunk(new ChunkPos(0, 0, 0)));
        }
    }

    public class ChunkTests
    {
        [Theory]
        [InlineData(0, 0, 0, 0, 0, 0)]
        [InlineData(15, 16, 31, 0, 1, 1)]
        [InlineData(-1, -16, -17, -1, -1, -2)]
        public void FromCell_FloorsBy16(int x, int y, int z, int cx, int cy, int cz)
        {
            Assert.Equal(new ChunkPos(cx, cy, cz), ChunkPos.FromCell(new GridPos(x, y, z)));
        }

        [Fact]
        public void PlaceRemoveRepaint_MarkAnchorChunkDirty()
        {
            BuildModel m = T.Model();
            PlacedBlock a, b;
            m.TryPlace(T.Spec("wall", 1, 0, 1), out a);
            m.TryPlace(T.Spec("wall", 20, 0, -3), out b);
            Assert.Equal(2, m.DirtyChunkCount);
            Assert.True(m.IsDirty(new ChunkPos(0, 0, 0)));
            Assert.True(m.IsDirty(new ChunkPos(1, 0, -1)));

            m.ClearDirty();
            m.SetLook(a.Id, T.Catalog.GetMaterial("stone"), PaintColor.White);
            Assert.Equal(new[] { new ChunkPos(0, 0, 0) }, m.GetDirtyChunks());

            m.ClearDirty();
            m.Remove(b.Id);
            Assert.Equal(new[] { new ChunkPos(1, 0, -1) }, m.GetDirtyChunks());
            Assert.Equal(0, m.CountInChunk(new ChunkPos(1, 0, -1)));
        }

        [Fact]
        public void BlockAcrossChunkBorder_BelongsToAnchorChunk()
        {
            BuildModel m = T.Model();
            PlacedBlock beam;
            m.TryPlace(T.Spec("beam", 15, 0, 0), out beam); // x = 15..17 crosses into chunk 1
            Assert.Equal(new ChunkPos(0, 0, 0), beam.Chunk);
            var inChunk = new List<PlacedBlock>();
            m.GetBlocksInChunk(new ChunkPos(0, 0, 0), inChunk);
            Assert.Single(inChunk);
            Assert.Equal(0, m.CountInChunk(new ChunkPos(1, 0, 0)));
            Assert.Equal(1, m.DirtyChunkCount);
        }

        [Fact]
        public void TakeDirtyChunks_LimitsPerCall()
        {
            BuildModel m = T.Model();
            for (int i = 0; i < 5; i++)
            {
                m.TryPlace(T.Spec("wall", i * 16, 0, 0));
            }
            var batch = new List<ChunkPos>();
            Assert.Equal(2, m.TakeDirtyChunks(batch, 2));
            Assert.Equal(3, m.DirtyChunkCount);
            Assert.Equal(3, m.TakeDirtyChunks(batch, 10));
            Assert.Equal(0, m.DirtyChunkCount);
            Assert.Equal(5, new HashSet<ChunkPos>(batch).Count);
            Assert.Equal(0, m.TakeDirtyChunks(batch, 10));
        }

        [Fact]
        public void TakeDirtyChunks_NearestFirst()
        {
            BuildModel m = T.Model();
            m.TryPlace(T.Spec("wall", 0, 0, 0));
            m.TryPlace(T.Spec("wall", 160, 0, 0));
            m.TryPlace(T.Spec("wall", 80, 0, 0));
            var batch = new List<ChunkPos>();
            m.TakeDirtyChunks(batch, 2, new GridPos(150, 0, 0));
            Assert.Equal(new[] { new ChunkPos(10, 0, 0), new ChunkPos(5, 0, 0) }, batch);
            Assert.True(m.IsDirty(new ChunkPos(0, 0, 0)));
        }

        [Fact]
        public void Clear_DirtiesOldChunks_AndMarkAllDirtyRestores()
        {
            BuildModel m = T.Model();
            m.TryPlace(T.Spec("wall", 0, 0, 0));
            m.TryPlace(T.Spec("wall", 40, 0, 0));
            m.ClearDirty();
            m.MarkAllDirty();
            Assert.Equal(2, m.DirtyChunkCount);
            m.ClearDirty();
            m.Clear();
            Assert.Equal(0, m.Count);
            Assert.Equal(2, m.DirtyChunkCount);
            Assert.Empty(m.NonEmptyChunks);
        }
    }
}
