using System.Collections.Generic;
using Xunit;

namespace WobblyMods.Core.Tests
{
    public class UndoStackTests
    {
        [Fact]
        public void EmptyStack_UndoLastReturnsNull()
        {
            var undo = new UndoStack();
            Assert.Null(undo.UndoLast());
            Assert.Equal(0, undo.Count);
            Assert.Null(undo.PeekDescription);
        }

        [Fact]
        public void UndoLast_RevertsNewestFirst()
        {
            var undo = new UndoStack();
            var log = new List<string>();
            undo.Push("a", () => log.Add("a"));
            undo.Push("b", () => log.Add("b"));

            Assert.Equal("b", undo.PeekDescription);
            Assert.Equal("b", undo.UndoLast());
            Assert.Equal("a", undo.UndoLast());
            Assert.Null(undo.UndoLast());
            Assert.Equal(new[] { "b", "a" }, log);
        }

        [Fact]
        public void UndoAll_RevertsEverythingAndCounts()
        {
            var undo = new UndoStack();
            int value = 0;
            undo.Push("one", () => value -= 1); value += 1;
            undo.Push("two", () => value -= 2); value += 2;
            undo.Push("three", () => value -= 3); value += 3;

            Assert.Equal(3, undo.Count);
            Assert.Equal(3, undo.UndoAll());
            Assert.Equal(0, value);
            Assert.Equal(0, undo.Count);
            Assert.Equal(0, undo.UndoAll());
        }

        [Fact]
        public void NullDescription_StillCountsAsUndone()
        {
            var undo = new UndoStack();
            bool reverted = false;
            undo.Push(null, () => reverted = true);
            Assert.NotNull(undo.UndoLast());
            Assert.True(reverted);
        }

        [Fact]
        public void Clear_ForgetsWithoutReverting()
        {
            var undo = new UndoStack();
            bool reverted = false;
            undo.Push("x", () => reverted = true);
            undo.Clear();
            Assert.Equal(0, undo.Count);
            Assert.False(reverted);
        }

        [Fact]
        public void Push_NullRevertThrows()
        {
            Assert.Throws<System.ArgumentNullException>(() => new UndoStack().Push("x", null));
        }
    }
}
