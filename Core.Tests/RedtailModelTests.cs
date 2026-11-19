using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WobblyMods.Core.Shapes;
using Xunit;

namespace WobblyMods.Core.Tests
{
    public class RedtailModelTests
    {
        [Fact]
        public void Build_ReturnsAtLeast10Parts()
        {
            var parts = RedtailModel.Build();
            Assert.True(parts.Count >= 10, $"Expected at least 10 parts, got {parts.Count}");
        }

        [Fact]
        public void Build_BoundsZWithinRange()
        {
            var parts = RedtailModel.Build();
            var combinedMin = V3.Zero;
            var combinedMax = V3.Zero;
            bool first = true;

            foreach (var part in parts)
            {
                if (part.Mesh.Vertices.Count == 0) continue;
                var min = part.Mesh.Min;
                var max = part.Mesh.Max;
                if (first)
                {
                    combinedMin = min;
                    combinedMax = max;
                    first = false;
                }
                else
                {
                    combinedMin = new V3(Math.Min(combinedMin.X, min.X), Math.Min(combinedMin.Y, min.Y), Math.Min(combinedMin.Z, min.Z));
                    combinedMax = new V3(Math.Max(combinedMax.X, max.X), Math.Max(combinedMax.Y, max.Y), Math.Max(combinedMax.Z, max.Z));
                }
            }

            Assert.True(combinedMin.Z >= -0.51f && combinedMin.Z <= -0.49f, $"Min Z was {combinedMin.Z}");
            Assert.True(combinedMax.Z >= 0.49f && combinedMax.Z <= 0.51f, $"Max Z was {combinedMax.Z}");
        }

        [Fact]
        public void Build_CenteredOnX()
        {
            var parts = RedtailModel.Build();
            float minX = float.MaxValue;
            float maxX = float.MinValue;

            foreach (var part in parts)
            {
                if (part.Mesh.Vertices.Count == 0) continue;
                var min = part.Mesh.Min;
                var max = part.Mesh.Max;
                if (min.X < minX) minX = min.X;
                if (max.X > maxX) maxX = max.X;
            }

            Assert.True(Math.Abs(minX + maxX) < 0.01f, $"Min X: {minX}, Max X: {maxX}. Sum: {minX + maxX}");
        }

        [Fact]
        public void Build_WidthUnder06()
        {
            var parts = RedtailModel.Build();
            float minX = float.MaxValue;
            float maxX = float.MinValue;

            foreach (var part in parts)
            {
                if (part.Mesh.Vertices.Count == 0) continue;
                var min = part.Mesh.Min;
                var max = part.Mesh.Max;
                if (min.X < minX) minX = min.X;
                if (max.X > maxX) maxX = max.X;
            }

            Assert.True(maxX - minX < 0.6f, $"Width was {maxX - minX}");
        }

        [Fact]
        public void Build_HeightUnder05()
        {
            var parts = RedtailModel.Build();
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            foreach (var part in parts)
            {
                if (part.Mesh.Vertices.Count == 0) continue;
                var min = part.Mesh.Min;
                var max = part.Mesh.Max;
                if (min.Y < minY) minY = min.Y;
                if (max.Y > maxY) maxY = max.Y;
            }

            Assert.True(maxY - minY < 0.5f, $"Height was {maxY - minY}");
        }

        [Fact]
        public void Build_UsesAllColors()
        {
            var parts = RedtailModel.Build();
            var usedColors = new HashSet<Rgb>();
            
            foreach (var part in parts)
            {
                usedColors.Add(part.Color);
            }

            Assert.Contains(RedtailModel.Body, usedColors);
            Assert.Contains(RedtailModel.Belly, usedColors);
            Assert.Contains(RedtailModel.Tail, usedColors);
            Assert.Contains(RedtailModel.Whisker, usedColors);
            Assert.Contains(RedtailModel.EyeWhite, usedColors);
            Assert.Contains(RedtailModel.Pupil, usedColors);
        }

        [Fact]
        public void Build_MeshesHaveUnitNormals()
        {
            var parts = RedtailModel.Build();
            foreach (var part in parts)
            {
                for (int i = 0; i < part.Mesh.Normals.Count; i++)
                {
                    float len = part.Mesh.Normals[i].Length;
                    Assert.True(Math.Abs(len - 1.0f) < 0.001f, $"Normal length at index {i} is {len}");
                }
            }
        }

        [Fact]
        public void Build_TriangleIndicesInBounds()
        {
            var parts = RedtailModel.Build();
            foreach (var part in parts)
            {
                for (int i = 0; i < part.Mesh.Triangles.Count; i++)
                {
                    int idx = part.Mesh.Triangles[i];
                    Assert.True(idx >= 0 && idx < part.Mesh.Vertices.Count, $"Index {idx} out of bounds in part with {part.Mesh.Vertices.Count} vertices");
                }
            }
        }

        [Fact]
        public void Build_NoDegenerateTriangles()
        {
            var parts = RedtailModel.Build();
            foreach (var part in parts)
            {
                for (int i = 0; i + 2 < part.Mesh.Triangles.Count; i += 3)
                {
                    V3 a = part.Mesh.Vertices[part.Mesh.Triangles[i]];
                    V3 b = part.Mesh.Vertices[part.Mesh.Triangles[i + 1]];
                    V3 c = part.Mesh.Vertices[part.Mesh.Triangles[i + 2]];
                    
                    float area = V3.Cross(b - a, c - a).Length / 2f;
                    Assert.True(area > 1e-7f, $"Degenerate triangle found with area {area}");
                }
            }
        }

        [Fact]
        public void Build_BellyLowerThanBody()
        {
            var parts = RedtailModel.Build();
            
            // Find Body mesh with largest Z extent (the main body tube)
            float bodyMinY = float.MaxValue;
            bool foundBody = false;
            
            foreach (var part in parts)
            {
                if (part.Color.R == RedtailModel.Body.R && part.Color.G == RedtailModel.Body.G && part.Color.B == RedtailModel.Body.B)
                {
                    var min = part.Mesh.Min;
                    var max = part.Mesh.Max;
                    float zExtent = max.Z - min.Z;
                    if (zExtent > 0.3f) // Main body is long
                    {
                        if (min.Y < bodyMinY) bodyMinY = min.Y;
                        foundBody = true;
                    }
                }
            }

            Assert.True(foundBody, "Could not find main body part");

            // Find lowest point of Belly parts
            float bellyMinY = float.MaxValue;
            bool foundBelly = false;
            foreach (var part in parts)
            {
                if (part.Color.R == RedtailModel.Belly.R && part.Color.G == RedtailModel.Belly.G && part.Color.B == RedtailModel.Belly.B)
                {
                    var min = part.Mesh.Min;
                    if (min.Y < bellyMinY) bellyMinY = min.Y;
                    foundBelly = true;
                }
            }

            Assert.True(foundBelly, "Could not find belly part");
            Assert.True(bellyMinY < bodyMinY - 0.01f, $"Belly lowest Y ({bellyMinY}) should be lower than Body lowest Y ({bodyMinY})");
        }

        [Fact]
        public void Build_TailFinExists()
        {
            var parts = RedtailModel.Build();
            bool foundTailFin = false;
            
            foreach (var part in parts)
            {
                if (part.Color.R == RedtailModel.Tail.R && part.Color.G == RedtailModel.Tail.G && part.Color.B == RedtailModel.Tail.B)
                {
                    var max = part.Mesh.Max;
                    if (max.Z < -0.25f)
                    {
                        foundTailFin = true;
                        break;
                    }
                }
            }

            Assert.True(foundTailFin, "No Tail-colored part with Max.Z < -0.25 found");
        }

        [Fact]
        public void Build_WhiskersReachBeyondX()
        {
            var parts = RedtailModel.Build();
            float maxXPositive = float.MinValue;
            float minXNegative = float.MaxValue;
            
            foreach (var part in parts)
            {
                if (part.Color.R == RedtailModel.Whisker.R && part.Color.G == RedtailModel.Whisker.G && part.Color.B == RedtailModel.Whisker.B)
                {
                    var max = part.Mesh.Max;
                    var min = part.Mesh.Min;
                    if (max.X > maxXPositive) maxXPositive = max.X;
                    if (min.X < minXNegative) minXNegative = min.X;
                }
            }

            Assert.True(maxXPositive > 0.15f, $"Max X of whiskers was {maxXPositive}");
            Assert.True(minXNegative < -0.15f, $"Min X of whiskers was {minXNegative}");
        }

        [Fact]
        public void Build_DeterministicVertexCounts()
        {
            var parts1 = RedtailModel.Build();
            var parts2 = RedtailModel.Build();

            Assert.Equal(parts1.Count, parts2.Count);
            
            for (int i = 0; i < parts1.Count; i++)
            {
                Assert.Equal(parts1[i].Mesh.Vertices.Count, parts2[i].Mesh.Vertices.Count);
            }
        }

        [Fact]
        public void Build_SavesPreviewImages()
        {
            var previewDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "preview");
            Directory.CreateDirectory(previewDir);

            var parts = RedtailModel.Build();
            Rgb bg = new Rgb(120, 170, 220);

            string sidePath = Path.Combine(previewDir, "redtail-side.bmp");
            Preview.SaveBmp(sidePath, parts, 400, 240, 90f, bg);

            string frontPath = Path.Combine(previewDir, "redtail-front.bmp");
            Preview.SaveBmp(frontPath, parts, 400, 240, 0f, bg);

            Assert.True(File.Exists(sidePath), $"File {sidePath} does not exist");
            Assert.True(File.Exists(frontPath), $"File {frontPath} does not exist");

            var sideBytes = File.ReadAllBytes(sidePath);
            var frontBytes = File.ReadAllBytes(frontPath);

            Assert.True(sideBytes.Length > 1000, $"Side BMP size {sideBytes.Length} <= 1000");
            Assert.True(frontBytes.Length > 1000, $"Front BMP size {frontBytes.Length} <= 1000");
        }
    }
}
