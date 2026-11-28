using System.Linq;
using Xunit;

namespace WobblyMods.BuildMode.Tests
{
    public class SerializerTests
    {
        private static BuildModel SampleHouse()
        {
            BuildModel m = T.Model();
            var ed = new BuildEditor(m);
            BuildMaterial wood = T.Catalog.GetMaterial("wood");
            ed.Fill(0, T.Catalog.GetBlock("floor"), wood, wood.DefaultColor, Rotation.None, new GridPos(-3, 0, -3), new GridPos(3, 0, 3));
            m.TryPlace(T.Spec("wall", -3, 0, -3, material: "brick"));
            m.TryPlace(T.Spec("door", 0, 0, -3, turns: 2));
            m.TryPlace(T.Spec("window", 3, 0, 0, turns: 3, material: "glass"));
            m.TryPlace(T.Spec("stairs", 1, 0, 1, turns: 1, material: "stone"));
            m.TryPlace(T.Spec("beam", -3, 3, 0, turns: 1, material: "concrete"));
            m.TryPlace(T.Spec("roof_slope", 0, 4, 0, material: "roof_tile"));
            m.TryPlace(new BlockSpec(T.Catalog.GetBlock("half_wall"), T.Catalog.GetMaterial("plaster"),
                new PaintColor(155, 93, 229, 200), new GridPos(-1000, -60, 99999), new Rotation(2)));
            return m;
        }

        [Fact]
        public void RoundTrip_IsExact()
        {
            BuildModel original = SampleHouse();
            var grid = new GridSettings(0.5f, new Vec3(123.456f, -7.25f, 0.1f));
            string name = " Skylar's castle \\ with\nnewline\tand tab ";
            string text = BuildSerializer.Write(original, grid, name);

            LoadResult r = BuildSerializer.Read(text, T.Catalog);
            Assert.True(r.Success, r.Error);
            Assert.Empty(r.Warnings);
            Assert.Equal(0, r.SkippedBlocks);
            Assert.Equal(original.Count, r.LoadedBlocks);
            Assert.Equal(name, r.Name);
            Assert.Equal(grid.CellSize, r.Grid.CellSize);
            Assert.Equal(grid.Origin, r.Grid.Origin);
            Assert.Equal(BuildSerializer.FormatVersion, r.FormatVersion);

            var a = original.BlocksInPlacementOrder().Select(b => b.Spec).ToList();
            var b2 = r.Model.BlocksInPlacementOrder().Select(b => b.Spec).ToList();
            Assert.Equal(a, b2);

            // Save again: byte-identical text.
            Assert.Equal(text, BuildSerializer.Write(r.Model, r.Grid, r.Name));
        }

        [Fact]
        public void Format_IsHumanReadable()
        {
            BuildModel m = T.Model();
            m.TryPlace(T.Spec("wall", 1, 2, -3, turns: 1, material: "brick"));
            string text = BuildSerializer.Write(m, new GridSettings(), "Test");
            string[] lines = text.Split('\n');
            Assert.Equal("wobblybuild 1", lines[0]);
            Assert.Contains("name Test", lines);
            Assert.Contains("cell 1 0 0 0", lines);
            Assert.Contains("block wall brick 1 2 -3 1 #FFFFFFFF", lines);
            Assert.Equal("end", lines[lines.Length - 2]);
            Assert.DoesNotContain("\r", text);
        }

        [Fact]
        public void UnknownIds_AreSkippedWithWarnings()
        {
            string text =
                "wobblybuild 1\r\n" +
                "name Mixed\r\n" +
                "block wall brick 0 0 0 0 #FFFFFFFF\r\n" +
                "block rocket wood 1 0 0 0 #FFFFFFFF\r\n" +
                "block rocket wood 2 0 0 0 #FFFFFFFF\r\n" +
                "block wall gold 3 0 0 0 #FFFFFFFF\r\n" +
                "block floor wood 4 0 0 0 #FFFFFFFF\r\n" +
                "end\r\n";
            LoadResult r = BuildSerializer.Read(text, T.Catalog);
            Assert.True(r.Success);
            Assert.Equal(2, r.LoadedBlocks);
            Assert.Equal(3, r.SkippedBlocks);
            Assert.Equal(2, r.Warnings.Count); // one per unknown id, not per line
            Assert.Contains(r.Warnings, w => w.Contains("rocket") && w.Contains("Line 4"));
            Assert.Contains(r.Warnings, w => w.Contains("gold"));
            Assert.NotNull(r.Model.GetBlockAt(new GridPos(4, 0, 0)));
        }

        [Fact]
        public void BadLines_Overlaps_AndUnknownKeywords_AreWarnings()
        {
            string text = string.Join("\n",
                "# a comment before the header",
                "wobblybuild 1",
                "sky blue",                                  // future keyword
                "cell banana 0 0 0",                         // bad cell line
                "block wall brick 0 0",                      // too short
                "block wall brick x 0 0 0 #FFFFFFFF",        // bad number
                "block wall brick 0 0 0 7 #FFFFFFFF",        // bad rotation
                "block wall brick 0 0 0 0 #NOPE",            // bad colour
                "block wall brick 0 0 0 0 #FFFFFFFF",        // ok
                "block pillar wood 0 0 0 0 #FFFFFFFF",       // overlaps the wall
                "block floor wood 0 0 0 0 #FFFFFFFF extra",  // ok, extra field ignored
                "end");
            LoadResult r = BuildSerializer.Read(text, T.Catalog);
            Assert.True(r.Success);
            Assert.Equal(2, r.LoadedBlocks);
            Assert.Equal(5, r.SkippedBlocks);
            Assert.Equal(1f, r.Grid.CellSize);
            Assert.Equal(7, r.Warnings.Count);
        }

        [Fact]
        public void MissingEnd_LoadsWithWarning()
        {
            LoadResult r = BuildSerializer.Read("wobblybuild 1\nblock wall wood 0 0 0 0 #FFFFFFFF\n", T.Catalog);
            Assert.True(r.Success);
            Assert.Equal(1, r.LoadedBlocks);
            Assert.Single(r.Warnings);
        }

        [Theory]
        [InlineData("")]
        [InlineData("hello world")]
        [InlineData("wobblybuild")]
        [InlineData("wobblybuild zero")]
        [InlineData("wobblybuild 0")]
        public void NotABuildFile_FailsCleanly(string text)
        {
            LoadResult r = BuildSerializer.Read(text, T.Catalog);
            Assert.False(r.Success);
            Assert.NotNull(r.Error);
            Assert.Null(r.Model);
        }

        [Fact]
        public void NewerVersion_FailsWithClearMessage()
        {
            LoadResult r = BuildSerializer.Read("wobblybuild 99\nend\n", T.Catalog);
            Assert.False(r.Success);
            Assert.Contains("newer", r.Error);
        }

        [Fact]
        public void ReadBlocks_ThenPasteAtOffset_AsOneUndoStep()
        {
            BuildModel source = T.Model();
            source.TryPlace(T.Spec("wall", 0, 0, 0));
            source.TryPlace(T.Spec("door", 1, 0, 0));
            string text = BuildSerializer.Write(source, null, "Hut");

            LoadResult r = BuildSerializer.ReadBlocks(text, T.Catalog);
            Assert.Null(r.Model);
            var ed = new BuildEditor(T.Model());
            EditResult paste = ed.PlaceMany(0, r.Blocks.Select(s => s.MovedBy(new GridPos(10, 0, 10))), "Load Hut");
            Assert.Equal(2, paste.BlocksChanged);
            Assert.NotNull(ed.Model.GetBlockAt(new GridPos(11, 1, 10)));
            ed.Undo(0);
            Assert.Equal(0, ed.Model.Count);
        }

        [Fact]
        public void LargeBuild_RoundTrips()
        {
            BuildModel m = T.Model();
            var ed = new BuildEditor(m);
            BuildMaterial brick = T.Catalog.GetMaterial("brick");
            ed.Fill(0, T.Catalog.GetBlock("wall"), brick, brick.DefaultColor, Rotation.None, new GridPos(0, 0, 0), new GridPos(49, 9, 49));
            Assert.Equal(25000, m.Count);
            string text = BuildSerializer.Write(m, null, "Big");
            LoadResult r = BuildSerializer.Read(text, T.Catalog);
            Assert.Equal(25000, r.LoadedBlocks);
            Assert.Equal(text, BuildSerializer.Write(r.Model, null, "Big"));
        }
    }
}
