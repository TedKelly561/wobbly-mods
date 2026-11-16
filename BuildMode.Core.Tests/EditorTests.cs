using Xunit;

namespace WobblyMods.BuildMode.Tests
{
    public class UndoRedoTests
    {
        private const int Skylar = 0;
        private const int Cousin = 1;

        [Fact]
        public void PlaceUndoRedo()
        {
            var ed = new BuildEditor(T.Model());
            Assert.True(ed.Place(Skylar, T.Spec("wall", 0, 0, 0)).Ok);
            Assert.Equal(1, ed.Model.Count);

            EditResult undo = ed.Undo(Skylar);
            Assert.True(undo.Ok);
            Assert.Equal("Undo: Place Wall", undo.Message);
            Assert.Equal(0, ed.Model.Count);

            Assert.True(ed.Redo(Skylar).Ok);
            Assert.Equal("wall", ed.Model.GetBlockAt(GridPos.Zero).Spec.Type.Id);
            Assert.Equal(BuildResult.NothingToRedo, ed.Redo(Skylar).Result);
        }

        [Fact]
        public void RemoveAll_IsOneUndoStep_AndKeepsOtherHistoriesWorking()
        {
            var ed = new BuildEditor(T.Model());
            Assert.Equal(BuildResult.NotFound, ed.RemoveAll(Skylar, "Clear build").Result);

            Assert.True(ed.Place(Skylar, T.Spec("wall", 0, 0, 0)).Ok);
            Assert.True(ed.Place(Cousin, T.Spec("wall", 1, 0, 0)).Ok);
            Assert.True(ed.Place(Skylar, T.Spec("floor", 0, 0, 0)).Ok);

            EditResult clear = ed.RemoveAll(Skylar, "Clear build");
            Assert.True(clear.Ok);
            Assert.Equal(3, clear.BlocksChanged);
            Assert.Equal(0, ed.Model.Count);

            // The cousin's block is gone, so their undo can't take it away.
            Assert.Equal(BuildResult.Conflict, ed.Undo(Cousin).Result);

            Assert.True(ed.Undo(Skylar).Ok);
            Assert.Equal(3, ed.Model.Count);
            Assert.Equal("wall", ed.Model.GetBlockAt(new GridPos(1, 0, 0)).Spec.Type.Id);

            // Skylar's older steps still undo normally after the restore.
            Assert.True(ed.Undo(Skylar).Ok);
            Assert.Null(ed.Model.GetBlockAt(GridPos.Zero, OccupancyLayer.Floor));

            Assert.True(ed.Redo(Skylar).Ok);
            Assert.True(ed.Redo(Skylar).Ok);
            Assert.Equal(0, ed.Model.Count);
        }

        [Fact]
        public void FailedPlace_IsNotRecorded()
        {
            var ed = new BuildEditor(T.Model());
            ed.Place(Skylar, T.Spec("wall", 0, 0, 0));
            Assert.Equal(BuildResult.Overlap, ed.Place(Skylar, T.Spec("pillar", 0, 0, 0)).Result);
            Assert.Equal(1, ed.HistoryFor(Skylar).UndoCount);
        }

        [Fact]
        public void RemoveUndo_RestoresSameBlockAndLook()
        {
            var ed = new BuildEditor(T.Model());
            ed.Place(Skylar, T.Spec("door", 3, 0, 3, turns: 2, material: "wood"));
            var pink = new PaintColor(255, 143, 171);
            ed.PaintAt(Skylar, new GridPos(3, 1, 3), T.Catalog.GetMaterial("plaster"), pink);
            int id = ed.Model.GetBlockAt(new GridPos(3, 0, 3)).Id;

            Assert.True(ed.RemoveAt(Skylar, new GridPos(3, 1, 3)).Ok);
            Assert.Equal(0, ed.Model.Count);

            Assert.True(ed.Undo(Skylar).Ok);
            PlacedBlock back = ed.Model.GetBlockAt(new GridPos(3, 1, 3));
            Assert.Equal(id, back.Id);
            Assert.Equal("plaster", back.Spec.Material.Id);
            Assert.Equal(pink, back.Spec.Paint);
            Assert.Equal(2, back.Spec.Rotation.QuarterTurns);

            Assert.True(ed.Undo(Skylar).Ok); // the paint
            Assert.Equal("wood", back.Spec.Material.Id);
            Assert.True(ed.Undo(Skylar).Ok); // the place
            Assert.Equal(0, ed.Model.Count);
            Assert.Equal(BuildResult.NothingToUndo, ed.Undo(Skylar).Result);
        }

        [Fact]
        public void NewEdit_ClearsRedo()
        {
            var ed = new BuildEditor(T.Model());
            ed.Place(Skylar, T.Spec("wall", 0, 0, 0));
            ed.Undo(Skylar);
            Assert.Equal(1, ed.HistoryFor(Skylar).RedoCount);
            ed.Place(Skylar, T.Spec("wall", 5, 0, 0));
            Assert.Equal(0, ed.HistoryFor(Skylar).RedoCount);
        }

        [Fact]
        public void EachPlayer_UndoesOnlyTheirOwnWork()
        {
            var ed = new BuildEditor(T.Model());
            ed.Place(Skylar, T.Spec("wall", 0, 0, 0));
            ed.Place(Cousin, T.Spec("wall", 1, 0, 0));
            ed.Place(Skylar, T.Spec("wall", 2, 0, 0));

            Assert.True(ed.Undo(Cousin).Ok);
            Assert.Null(ed.Model.GetBlockAt(new GridPos(1, 0, 0)));
            Assert.NotNull(ed.Model.GetBlockAt(new GridPos(0, 0, 0)));
            Assert.NotNull(ed.Model.GetBlockAt(new GridPos(2, 0, 0)));
            Assert.Equal(BuildResult.NothingToUndo, ed.Undo(Cousin).Result);

            Assert.Equal(2, ed.HistoryFor(Skylar).UndoCount);
            Assert.Equal(0, ed.HistoryFor(3).UndoCount); // players get histories on demand
        }

        [Fact]
        public void UndoAfterSomeoneElseRemovedIt_IsAConflict_AndDropped()
        {
            var ed = new BuildEditor(T.Model());
            ed.Place(Skylar, T.Spec("wall", 0, 0, 0));
            ed.RemoveAt(Cousin, GridPos.Zero);

            EditResult r = ed.Undo(Skylar);
            Assert.Equal(BuildResult.Conflict, r.Result);
            Assert.Equal(0, ed.HistoryFor(Skylar).UndoCount);
            Assert.Equal(0, ed.HistoryFor(Skylar).RedoCount);

            // Cousin can still undo their remove: Skylar's wall comes back.
            Assert.True(ed.Undo(Cousin).Ok);
            Assert.NotNull(ed.Model.GetBlockAt(GridPos.Zero));
        }

        [Fact]
        public void UndoRemove_WhenSpotRetaken_IsAConflict()
        {
            var ed = new BuildEditor(T.Model());
            ed.Place(Skylar, T.Spec("wall", 0, 0, 0));
            ed.RemoveAt(Skylar, GridPos.Zero);
            ed.Place(Cousin, T.Spec("pillar", 0, 0, 0));

            Assert.Equal(BuildResult.Conflict, ed.Undo(Skylar).Result);
            Assert.Equal("pillar", ed.Model.GetBlockAt(GridPos.Zero).Spec.Type.Id);
        }

        [Fact]
        public void Fill_IsOneUndoStep_AndSkipsTakenCells()
        {
            var ed = new BuildEditor(T.Model());
            BuildMaterial wood = T.Catalog.GetMaterial("wood");
            ed.Place(Cousin, T.Spec("floor", 2, 0, 2));

            EditResult fill = ed.Fill(Skylar, T.Catalog.GetBlock("floor"), wood, wood.DefaultColor, Rotation.None,
                new GridPos(4, 0, 4), new GridPos(0, 0, 0));
            Assert.True(fill.Ok);
            Assert.Equal(24, fill.BlocksChanged);
            Assert.Equal(25, ed.Model.Count);
            Assert.Equal(1, ed.HistoryFor(Skylar).UndoCount);

            Assert.True(ed.Undo(Skylar).Ok);
            Assert.Equal(1, ed.Model.Count); // cousin's floor stays
            Assert.True(ed.Redo(Skylar).Ok);
            Assert.Equal(25, ed.Model.Count);
        }

        [Fact]
        public void Fill_TilesRotatedMultiCellBlocks()
        {
            var ed = new BuildEditor(T.Model());
            BuildMaterial stone = T.Catalog.GetMaterial("stone");
            // Beams are 3 long; turned once they run along -Z. A 1 x 1 x 6 box fits two.
            EditResult fill = ed.Fill(Skylar, T.Catalog.GetBlock("beam"), stone, stone.DefaultColor, new Rotation(1),
                new GridPos(0, 0, 0), new GridPos(0, 0, 5));
            Assert.Equal(2, fill.BlocksChanged);
            for (int z = 0; z <= 5; z++)
            {
                Assert.NotNull(ed.Model.GetBlockAt(new GridPos(0, 0, z)));
            }
        }

        [Fact]
        public void HistoryLimit_DropsOldest()
        {
            var ed = new BuildEditor(T.Model(), 3);
            for (int i = 0; i < 5; i++)
            {
                ed.Place(Skylar, T.Spec("wall", i, 0, 0));
            }
            Assert.Equal(3, ed.HistoryFor(Skylar).UndoCount);
            while (ed.Undo(Skylar).Ok)
            {
            }
            Assert.Equal(2, ed.Model.Count); // the two oldest can't be undone any more
        }

        [Fact]
        public void PaintSameLook_IsNotRecorded()
        {
            var ed = new BuildEditor(T.Model());
            ed.Place(Skylar, T.Spec("wall", 0, 0, 0));
            BuildMaterial wood = T.Catalog.GetMaterial("wood");
            EditResult r = ed.PaintAt(Skylar, GridPos.Zero, wood, wood.DefaultColor);
            Assert.Equal(0, r.BlocksChanged);
            Assert.Equal(1, ed.HistoryFor(Skylar).UndoCount);
            Assert.Equal(BuildResult.NotFound, ed.PaintAt(Skylar, new GridPos(9, 9, 9), wood, wood.DefaultColor).Result);
        }
    }

    public class PlayerStateTests
    {
        [Fact]
        public void Cycling_WrapsAndBuildsSpec()
        {
            var p = new PlayerBuildState(2, T.Catalog);
            Assert.Equal("floor", p.Block.Id);
            p.CycleBlock(-1);
            Assert.Equal("beam", p.Block.Id);
            p.CycleBlock(1);
            Assert.True(p.SelectBlock("door"));
            Assert.False(p.SelectBlock("rocket"));
            Assert.True(p.SelectMaterial("brick"));

            Assert.Equal(p.Material.DefaultColor, p.Paint);
            p.CyclePaint(1);
            Assert.Equal(PaintColor.Palette[0], p.Paint);
            p.CyclePaint(-2);
            Assert.Equal(PaintColor.Palette[PaintColor.Palette.Length - 1], p.Paint);

            p.RotateClockwise();
            p.Cursor = new GridPos(1, 0, 1);
            BlockSpec s = p.CurrentSpec();
            Assert.Equal("door", s.Type.Id);
            Assert.Equal("brick", s.Material.Id);
            Assert.Equal(1, s.Rotation.QuarterTurns);
            Assert.Equal(new GridPos(1, 0, 1), s.Anchor);
        }

        [Fact]
        public void CustomPalette_CyclesInGivenOrder()
        {
            var orange = new PaintColor(255, 128, 0);
            var green = new PaintColor(51, 191, 51);
            var colors = new System.Collections.Generic.List<PaintColor> { orange, green };
            var p = new PlayerBuildState(0, T.Catalog, colors);
            colors.Add(PaintColor.White); // copied: later changes don't leak in

            Assert.Equal(2, p.Palette.Count);
            Assert.Equal(-1, p.PaintIndex);
            Assert.Equal(p.Material.DefaultColor, p.Paint);
            p.CyclePaint(1);
            Assert.Equal(0, p.PaintIndex);
            Assert.Equal(orange, p.Paint);
            p.CyclePaint(1);
            Assert.Equal(green, p.Paint);
            p.CyclePaint(1);
            Assert.Equal(-1, p.PaintIndex); // wraps back to the material's own colour
            p.CyclePaint(-1);
            Assert.Equal(green, p.Paint);
        }

        [Fact]
        public void NullOrEmptyPalette_UsesDefault()
        {
            var a = new PlayerBuildState(0, T.Catalog, null);
            var b = new PlayerBuildState(0, T.Catalog, new PaintColor[0]);
            Assert.Equal(PaintColor.Palette.Length, a.Palette.Count);
            Assert.Equal(PaintColor.Palette.Length, b.Palette.Count);
            b.CyclePaint(1);
            Assert.Equal(PaintColor.Palette[0], b.Paint);
        }

        [Fact]
        public void MoveCursor_IsCameraRelative()
        {
            var p = new PlayerBuildState(0, T.Catalog);
            p.MoveCursor(0, 0, 1, 0f);
            Assert.Equal(new GridPos(0, 0, 1), p.Cursor);
            p.MoveCursor(0, 0, 1, 92f); // camera faces +X
            Assert.Equal(new GridPos(1, 0, 1), p.Cursor);
            p.MoveCursor(1, 1, 0, 180f); // right of a camera facing -Z is -X
            Assert.Equal(new GridPos(0, 1, 1), p.Cursor);
        }
    }
}
