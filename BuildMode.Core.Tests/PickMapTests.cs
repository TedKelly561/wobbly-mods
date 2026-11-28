using System.Collections.Generic;
using Xunit;

namespace WobblyMods.BuildMode.Tests
{
    /// <summary>
    /// "Hold LB to remove the piece you're looking at" asks this map which block each thing on the aim ray
    /// belongs to. Keys here are plain numbers standing in for the game's colliders.
    /// </summary>
    public class PickMapTests
    {
        private static readonly ChunkPos Here = new ChunkPos(0, 0, 0);
        private static readonly ChunkPos Next = new ChunkPos(1, 0, 0);

        // Things on the ray that are not part of the build.
        private const int Ground = 900;
        private const int OwnBody = 901;

        [Fact]
        public void KnowsWhichBlockAPartBelongsTo()
        {
            var map = new BlockPickMap<int>();
            map.Add(Here, 10, 1);
            map.Add(Here, 11, 1); // a window has several solid parts
            map.Add(Here, 12, 2);

            int id;
            Assert.True(map.TryGetBlock(11, out id));
            Assert.Equal(1, id);
            Assert.True(map.TryGetBlock(12, out id));
            Assert.Equal(2, id);
            Assert.False(map.TryGetBlock(Ground, out id));
            Assert.Equal(3, map.Count);
        }

        [Fact]
        public void RedrawingAChunk_ForgetsOnlyThatChunksParts()
        {
            var map = new BlockPickMap<int>();
            map.Add(Here, 10, 1);
            map.Add(Here, 11, 2);
            map.Add(Next, 20, 3);

            map.RemoveChunk(Here);
            int id;
            Assert.False(map.TryGetBlock(10, out id));
            Assert.False(map.TryGetBlock(11, out id));
            Assert.True(map.TryGetBlock(20, out id));
            Assert.Equal(1, map.Count);

            map.RemoveChunk(Here); // nothing left there: harmless
            map.RemoveChunk(new ChunkPos(5, 5, 5));
            Assert.Equal(1, map.Count);

            map.Clear();
            Assert.Equal(0, map.Count);
            Assert.False(map.TryGetBlock(20, out id));
        }

        [Fact]
        public void AddingTheSamePartAgain_ReplacesIt_AndItIsStillForgottenWithItsChunk()
        {
            var map = new BlockPickMap<int>();
            map.Add(Here, 10, 1);
            map.Add(Here, 10, 7);
            int id;
            Assert.True(map.TryGetBlock(10, out id));
            Assert.Equal(7, id);
            Assert.Equal(1, map.Count);

            map.RemoveChunk(Here);
            Assert.Equal(0, map.Count);
        }

        [Fact]
        public void LookingAtTheFloorBesideAWall_PicksTheFloor_NotTheWallInTheSameCell()
        {
            // A floor and a wall share cell (0,0,0). GetBlockAt prefers the wall, which is why removal
            // used to take the wall when Skylar looked at the floor next to it.
            BuildModel model = T.Model();
            PlacedBlock floor, wall;
            Assert.Equal(BuildResult.Ok, model.TryPlace(T.Spec("floor", 0, 0, 0), out floor));
            Assert.Equal(BuildResult.Ok, model.TryPlace(T.Spec("wall", 0, 0, 0), out wall));
            Assert.Equal(wall.Id, model.GetBlockAt(GridPos.Zero).Id);

            var map = new BlockPickMap<int>();
            map.Add(Here, 10, floor.Id);
            map.Add(Here, 11, wall.Id);

            int id;
            // The ray meets her own body first, then the floor, then the ground under it, then the wall.
            Assert.True(map.FirstBlock(new List<int> { OwnBody, 10, Ground, 11 }, model, out id));
            Assert.Equal(floor.Id, id);

            // Looking at the wall picks the wall.
            Assert.True(map.FirstBlock(new List<int> { 11, 10 }, model, out id));
            Assert.Equal(wall.Id, id);
        }

        [Fact]
        public void ThingsThatAreNotBuildPieces_AreNeverPicked()
        {
            BuildModel model = T.Model();
            PlacedBlock wall;
            model.TryPlace(T.Spec("wall", 0, 0, 0), out wall);
            var map = new BlockPickMap<int>();
            map.Add(Here, 10, wall.Id);

            int id;
            Assert.False(map.FirstBlock(new List<int> { OwnBody, Ground }, model, out id));
            Assert.False(map.FirstBlock(new List<int>(), model, out id));
            Assert.False(map.FirstBlock(null, model, out id));
            Assert.Throws<System.ArgumentNullException>(() => map.FirstBlock(new List<int> { 10 }, null, out id));
        }

        [Fact]
        public void APartWhoseBlockWasJustRemoved_IsSkipped()
        {
            // Between a block leaving the model and its chunk being redrawn, its old part is still known.
            BuildModel model = T.Model();
            PlacedBlock near, far;
            model.TryPlace(T.Spec("wall", 0, 0, 0), out near);
            model.TryPlace(T.Spec("wall", 0, 0, 3), out far);
            var map = new BlockPickMap<int>();
            map.Add(Here, 10, near.Id);
            map.Add(Here, 11, far.Id);

            Assert.True(model.Remove(near.Id));
            int id;
            Assert.True(map.FirstBlock(new List<int> { 10, 11 }, model, out id));
            Assert.Equal(far.Id, id);

            Assert.True(model.Remove(far.Id));
            Assert.False(map.FirstBlock(new List<int> { 10, 11 }, model, out id));
        }
    }
}
