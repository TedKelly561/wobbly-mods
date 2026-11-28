using System;
using System.IO;
using System.Linq;
using WobblyMods.Core.Shapes;
using Xunit;

namespace WobblyMods.Core.Tests
{
    public class CostumeModelsTests
    {
        private static readonly Rgb CatFur = new Rgb(20, 20, 22);
        private static readonly Rgb BunnyFur = new Rgb(250, 250, 250);
        private static readonly Rgb PuppyFur = new Rgb(153, 102, 56);
        private static readonly Rgb TrexFur = new Rgb(64, 158, 51);

        private static Rgb FurFor(string id)
        {
            switch (id)
            {
                case "cat": return CatFur;
                case "bunny": return BunnyFur;
                case "puppy": return PuppyFur;
                default: return TrexFur;
            }
        }

        [Theory]
        [InlineData("dragon")]
        [InlineData("")]
        [InlineData("CAT")]
        [InlineData(null)]
        public void Build_UnknownId_ThrowsArgumentException(string id)
        {
            Assert.Throws<ArgumentException>(() => CostumeModels.Build(id, CatFur));
        }

        [Theory]
        [InlineData("cat")]
        [InlineData("bunny")]
        [InlineData("puppy")]
        [InlineData("trex")]
        public void Build_HasHeadAndBodyParts(string id)
        {
            Rgb fur = FurFor(id);
            var costume = CostumeModels.Build(id, fur);
            Assert.NotEmpty(costume.Head);
            Assert.NotEmpty(costume.Body);
        }

        [Theory]
        [InlineData("cat")]
        [InlineData("bunny")]
        [InlineData("puppy")]
        [InlineData("trex")]
        public void Build_MeshesHaveValidNormals(string id)
        {
            Rgb fur = FurFor(id);
            var costume = CostumeModels.Build(id, fur);
            foreach (var part in costume.Head.Concat(costume.Body))
            {
                foreach (var normal in part.Mesh.Normals)
                {
                    Assert.InRange(normal.Length - 1.0f, -0.001f, 0.001f);
                }
            }
        }

        [Theory]
        [InlineData("cat")]
        [InlineData("bunny")]
        [InlineData("puppy")]
        [InlineData("trex")]
        public void Build_MeshIndicesInBounds(string id)
        {
            Rgb fur = FurFor(id);
            var costume = CostumeModels.Build(id, fur);
            foreach (var part in costume.Head.Concat(costume.Body))
            {
                var mesh = part.Mesh;
                int vertexCount = mesh.Vertices.Count;
                for (int i = 0; i < mesh.Triangles.Count; i += 3)
                {
                    Assert.InRange(mesh.Triangles[i], 0, vertexCount - 1);
                    Assert.InRange(mesh.Triangles[i + 1], 0, vertexCount - 1);
                    Assert.InRange(mesh.Triangles[i + 2], 0, vertexCount - 1);
                }
            }
        }

        [Theory]
        [InlineData("cat")]
        [InlineData("bunny")]
        [InlineData("puppy")]
        [InlineData("trex")]
        public void Build_NoDegenerateTriangles(string id)
        {
            Rgb fur = FurFor(id);
            var costume = CostumeModels.Build(id, fur);
            foreach (var part in costume.Head.Concat(costume.Body))
            {
                var mesh = part.Mesh;
                for (int i = 0; i < mesh.Triangles.Count; i += 3)
                {
                    V3 a = mesh.Vertices[mesh.Triangles[i]];
                    V3 b = mesh.Vertices[mesh.Triangles[i + 1]];
                    V3 c = mesh.Vertices[mesh.Triangles[i + 2]];
                    float area = V3.Cross(b - a, c - a).Length / 2.0f;
                    Assert.True(area > 1e-7, $"Degenerate triangle in {id}");
                }
            }
        }

        [Theory]
        [InlineData("cat")]
        [InlineData("bunny")]
        [InlineData("puppy")]
        [InlineData("trex")]
        public void Build_HeadLeftRightSymmetry(string id)
        {
            Rgb fur = FurFor(id);
            var costume = CostumeModels.Build(id, fur);
            V3 min = new V3(float.MaxValue, float.MaxValue, float.MaxValue);
            V3 max = new V3(float.MinValue, float.MinValue, float.MinValue);
            foreach (var part in costume.Head)
            {
                min.X = Math.Min(min.X, part.Mesh.Min.X);
                min.Y = Math.Min(min.Y, part.Mesh.Min.Y);
                min.Z = Math.Min(min.Z, part.Mesh.Min.Z);
                max.X = Math.Max(max.X, part.Mesh.Max.X);
                max.Y = Math.Max(max.Y, part.Mesh.Max.Y);
                max.Z = Math.Max(max.Z, part.Mesh.Max.Z);
            }
            Assert.InRange(Math.Abs(min.X) - max.X, -0.02f, 0.02f);
        }

        [Theory]
        [InlineData("cat")]
        [InlineData("bunny")]
        [InlineData("puppy")]
        [InlineData("trex")]
        public void Build_HeadPartsCloseToOrigin(string id)
        {
            Rgb fur = FurFor(id);
            var costume = CostumeModels.Build(id, fur);
            foreach (var part in costume.Head)
            {
                // Nearest point of bounding box to origin
                V3 nearest = new V3(
                    Math.Max(part.Mesh.Min.X, Math.Min(0f, part.Mesh.Max.X)),
                    Math.Max(part.Mesh.Min.Y, Math.Min(0f, part.Mesh.Max.Y)),
                    Math.Max(part.Mesh.Min.Z, Math.Min(0f, part.Mesh.Max.Z))
                );
                Assert.True(nearest.Length <= 2.2f, $"Head part too far from origin in {id}");

                // Touches the head: some vertex within 1.05 of the origin, or (for teeth, nostrils and the like
                // that sit on a big part such as the T-Rex snout) some vertex within 0.15 of another head part.
                bool hasCloseVertex = part.Mesh.Vertices.Any(v => v.Length <= 1.05f);
                if (!hasCloseVertex)
                {
                    foreach (var other in costume.Head)
                    {
                        if (ReferenceEquals(other, part)) continue;
                        if (part.Mesh.Vertices.Any(v => other.Mesh.Vertices.Any(w => (v - w).Length <= 0.15f)))
                        {
                            hasCloseVertex = true;
                            break;
                        }
                    }
                }
                Assert.True(hasCloseVertex, $"A head part floats free of the head in {id}");
            }
        }

        [Theory]
        [InlineData("bunny", true, false)]
        [InlineData("puppy", false, true)]
        [InlineData("trex", false, false, true)]
        [InlineData("cat", false, false, false, true)]
        public void Build_SpecificCoordinateBounds(string id, bool bunnyHeadHigh = false, bool puppyHeadLow = false, bool trexBodyLowZ = false, bool catBodyHighY = false)
        {
            Rgb fur = FurFor(id);
            var costume = CostumeModels.Build(id, fur);
            
            if (bunnyHeadHigh)
            {
                Assert.True(costume.Head.Any(p => p.Mesh.Max.Y > 1.8f), "Bunny head should reach above y=1.8");
            }

            if (puppyHeadLow)
            {
                Assert.True(costume.Head.Any(p => p.Mesh.Min.Y < -0.3f), "Puppy head should reach below y=-0.3");
            }

            if (trexBodyLowZ)
            {
                Assert.True(costume.Body.Any(p => p.Mesh.Min.Z < -1.8f), "Trex body should reach z < -1.8");
            }

            if (catBodyHighY)
            {
                Assert.True(costume.Body.Any(p => p.Mesh.Max.Y > 0.8f), "Cat body should reach y > 0.8");
            }
        }

        [Theory]
        [InlineData("cat")]
        [InlineData("bunny")]
        [InlineData("puppy")]
        [InlineData("trex")]
        public void Build_FurColorUsed(string id)
        {
            Rgb fur = FurFor(id);
            var costume = CostumeModels.Build(id, fur);
            bool found = costume.Head.Concat(costume.Body).Any(p => p.Color.R == fur.R && p.Color.G == fur.G && p.Color.B == fur.B);
            Assert.True(found, $"Fur color not used in {id}");
        }

        [Theory]
        [InlineData("cat")]
        [InlineData("bunny")]
        [InlineData("puppy")]
        [InlineData("trex")]
        public void Build_IsDeterministic(string id)
        {
            Rgb fur = FurFor(id);
            var c1 = CostumeModels.Build(id, fur);
            var c2 = CostumeModels.Build(id, fur);

            Assert.Equal(c1.Head.Count, c2.Head.Count);
            Assert.Equal(c1.Body.Count, c2.Body.Count);

            for (int i = 0; i < c1.Head.Count; i++)
            {
                Assert.Equal(c1.Head[i].Mesh.Vertices.Count, c2.Head[i].Mesh.Vertices.Count);
            }
            for (int i = 0; i < c1.Body.Count; i++)
            {
                Assert.Equal(c1.Body[i].Mesh.Vertices.Count, c2.Body[i].Mesh.Vertices.Count);
            }
        }

        [Theory]
        [InlineData("cat")]
        [InlineData("bunny")]
        [InlineData("puppy")]
        [InlineData("trex")]
        public void StandInAndDressed_NonEmptyAndMoreParts(string id)
        {
            Rgb fur = FurFor(id);
            var standIn = CostumeModels.StandIn(fur);
            var dressed = CostumeModels.Dressed(id, fur);

            Assert.NotEmpty(standIn);
            Assert.NotEmpty(dressed);
            Assert.True(dressed.Count > standIn.Count, "Dressed should have more parts than StandIn");
        }

        [Theory]
        [InlineData("cat")]
        [InlineData("bunny")]
        [InlineData("puppy")]
        [InlineData("trex")]
        public void Preview_SaveBmp_CreatesFiles(string id)
        {
            Rgb fur = FurFor(id);
            string previewDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "preview");
            Directory.CreateDirectory(previewDir);

            var dressed = CostumeModels.Dressed(id, fur);
            int width = 480;
            int height = 480;
            Rgb bg = new Rgb(120, 170, 220);

            string[] yaws = { "front", "side", "three-quarter" };
            float[] yawValues = { 0f, 90f, 35f };

            for (int i = 0; i < yaws.Length; i++)
            {
                string fileName = $"costume-{id}-{yaws[i]}.bmp";
                string fullPath = Path.Combine(previewDir, fileName);
                
                Preview.SaveBmp(fullPath, dressed, width, height, yawValues[i], bg);

                Assert.True(File.Exists(fullPath), $"File {fileName} was not created");
                long size = new FileInfo(fullPath).Length;
                Assert.True(size > 1000, $"File {fileName} is too small ({size} bytes)");
            }
        }
    }
}
