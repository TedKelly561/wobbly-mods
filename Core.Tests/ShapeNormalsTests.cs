using System;
using System.Collections.Generic;
using WobblyMods.Core.Shapes;
using Xunit;

/// <summary>
/// Lighting uses the vertex normals, and which side of a triangle is its front comes from the order of its
/// corners (cross(b - a, c - a) points out of the front). These tests check the two agree: a shape whose
/// normals point one way while its faces point another is drawn, but lit wrongly.
/// </summary>
public class ShapeNormalsTests
{
    /// <summary>
    /// How many triangles have vertex normals that don't point the way the triangle faces. With everyCorner each
    /// corner's own normal must agree; without it the three together must (a cylinder's flat ends borrow the
    /// side's ring, so their rim corners point sideways while the face as a whole still points the right way).
    /// </summary>
    private static int FacesAgainstTheirNormals(ShapeMesh mesh, bool everyCorner)
    {
        int bad = 0;
        for (int i = 0; i + 2 < mesh.Triangles.Count; i += 3)
        {
            int a = mesh.Triangles[i], b = mesh.Triangles[i + 1], c = mesh.Triangles[i + 2];
            V3 face = V3.Cross(mesh.Vertices[b] - mesh.Vertices[a], mesh.Vertices[c] - mesh.Vertices[a]).Normalized;
            bool ok = everyCorner
                ? V3.Dot(face, mesh.Normals[a]) > 0.2f && V3.Dot(face, mesh.Normals[b]) > 0.2f && V3.Dot(face, mesh.Normals[c]) > 0.2f
                : V3.Dot(face, (mesh.Normals[a] + mesh.Normals[b] + mesh.Normals[c]).Normalized) > 0.2f;
            if (!ok) bad++;
        }
        return bad;
    }

    [Theory]
    [InlineData(0.5f, 2f, 16)]
    [InlineData(0.18f, 0.45f, 16)] // a cat ear
    [InlineData(0.04f, 0.12f, 8)]  // a tooth
    [InlineData(1f, 0.2f, 3)]      // wide, flat and coarse
    public void Cone_NormalsPointTheWayItsFacesDo(float radius, float height, int segments)
    {
        ShapeMesh cone = Shapes.Cone(radius, height, segments);
        Assert.Equal(0, FacesAgainstTheirNormals(cone, true));
        foreach (V3 n in cone.Normals) Assert.InRange(n.Length, 0.999f, 1.001f);
    }

    [Fact]
    public void Cone_SideNormalsLeanOutAndUp_BaseNormalsPointDown()
    {
        ShapeMesh cone = Shapes.Cone(0.5f, 2f, 16);
        for (int i = 0; i + 2 < cone.Triangles.Count; i += 3)
        {
            int a = cone.Triangles[i], b = cone.Triangles[i + 1], c = cone.Triangles[i + 2];
            bool isBase = cone.Vertices[a].Y == 0f && cone.Vertices[b].Y == 0f && cone.Vertices[c].Y == 0f;
            foreach (int v in new[] { a, b, c })
            {
                V3 n = cone.Normals[v];
                if (isBase)
                {
                    Assert.Equal(-1f, n.Y, 3);
                }
                else
                {
                    // Up a little (the slope), and away from the axis for the ring vertices.
                    Assert.True(n.Y > 0.1f, "side normal should lean up, got " + n);
                    V3 p = cone.Vertices[v];
                    if (p.Y == 0f) Assert.True(n.X * p.X + n.Z * p.Z > 0f, "side normal should point away from the axis");
                }
            }
        }
    }

    [Fact]
    public void Cone_ShapeIsStillTheSame()
    {
        const int segments = 12;
        ShapeMesh cone = Shapes.Cone(0.5f, 2f, segments);

        // One triangle per segment on the side and one on the base.
        Assert.Equal(2 * segments, cone.Triangles.Count / 3);

        // The only points are the tip, the base centre and the base circle.
        var points = new HashSet<string>();
        foreach (V3 v in cone.Vertices)
        {
            points.Add(Math.Round(v.X, 4) + "," + Math.Round(v.Y, 4) + "," + Math.Round(v.Z, 4));
            bool tip = v.X == 0f && v.Y == 2f && v.Z == 0f;
            bool centre = v.X == 0f && v.Y == 0f && v.Z == 0f;
            bool onCircle = v.Y == 0f && Math.Abs(Math.Sqrt(v.X * v.X + v.Z * v.Z) - 0.5) < 1e-4;
            Assert.True(tip || centre || onCircle, "unexpected cone point " + v);
        }
        Assert.Equal(segments + 2, points.Count);
    }

    public static IEnumerable<object[]> FurnitureIds()
    {
        foreach (string id in SkylarFurniture.Ids) yield return new object[] { id };
    }

    /// <summary>Catches inside-out triangles, like the fish rug's tail had, and wrongly lit cones (the dinosaur's spines).</summary>
    [Theory, MemberData(nameof(FurnitureIds))]
    public void SkylarFurniture_NormalsPointTheWayItsFacesDo(string id)
    {
        List<ColoredMesh> parts = SkylarFurniture.Build(id);
        for (int i = 0; i < parts.Count; i++)
        {
            Assert.True(FacesAgainstTheirNormals(parts[i].Mesh, false) == 0, id + " part " + i + " has faces that disagree with their normals");
        }
    }
}
