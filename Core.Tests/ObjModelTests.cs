using System;
using System.Collections.Generic;
using WobblyMods.Core.Shapes;
using Xunit;

public class ObjModelTests
{
    // A unit cube from (0,0,0) to (1,1,1), quads, counter-clockwise seen from outside (the .obj convention).
    private const string Cube =
        "# cube\nmtllib cube.mtl\n" +
        "v 0 0 0\nv 1 0 0\nv 1 1 0\nv 0 1 0\nv 0 0 1\nv 1 0 1\nv 1 1 1\nv 0 1 1\n" +
        "usemtl wood\n" +
        "f 1/1/1 4/1/1 3/1/1 2/1/1\n" +   // back  (z = 0)
        "f 5 6 7 8\n" +                    // front (z = 1)
        "usemtl metal\n" +
        "f 1 2 6 5\n" +                    // bottom
        "f 4 8 7 3\n" +                    // top
        "f 1 5 8 4\n" +                    // x = 0
        "f 2//1 3//1 7//1 6//1\n";         // x = 1
    private const string Mtl = "newmtl wood\nKd 1.0 0.5 0.0\nnewmtl metal\nKa 0 0 0\nKd 0.2 0.2 0.25\n";
    private static readonly Rgb Grey = new Rgb(128, 128, 128);

    [Fact]
    public void CubeGivesOnePartPerMaterialInOrderOfUse()
    {
        List<ColoredMesh> parts = ObjModel.Parse(Cube, Mtl, Grey);
        Assert.Equal(2, parts.Count);
        Assert.Equal(255, parts[0].Color.R); Assert.Equal(128, parts[0].Color.G); Assert.Equal(0, parts[0].Color.B);
        Assert.Equal(51, parts[1].Color.R); Assert.Equal(64, parts[1].Color.B);
    }

    [Fact]
    public void QuadsAreSplitIntoTwoTriangles()
    {
        List<ColoredMesh> parts = ObjModel.Parse(Cube, Mtl, Grey);
        Assert.Equal(2 * 2 * 3, parts[0].Mesh.Triangles.Count);
        Assert.Equal(4 * 2 * 3, parts[1].Mesh.Triangles.Count);
    }

    [Fact]
    public void XIsMirroredSoTheModelIsNotFlippedInTheGame()
    {
        List<ColoredMesh> parts = ObjModel.Parse(Cube, Mtl, Grey);
        foreach (ColoredMesh p in parts)
        {
            Assert.True(p.Mesh.Min.X >= -1.0001f && p.Mesh.Max.X <= 0.0001f);
            Assert.True(p.Mesh.Min.Y >= -0.0001f && p.Mesh.Max.Z <= 1.0001f);
        }
    }

    [Fact]
    public void EveryTriangleFacesOutward()
    {
        var center = new V3(-0.5f, 0.5f, 0.5f);
        foreach (ColoredMesh p in ObjModel.Parse(Cube, Mtl, Grey))
        {
            ShapeMesh m = p.Mesh;
            for (int i = 0; i < m.Triangles.Count; i += 3)
            {
                V3 a = m.Vertices[m.Triangles[i]], b = m.Vertices[m.Triangles[i + 1]], c = m.Vertices[m.Triangles[i + 2]];
                V3 n = V3.Cross(b - a, c - a);
                Assert.True(V3.Dot(n, (a + b + c) / 3f - center) > 0f, "triangle " + i / 3 + " faces inward");
                Assert.True(V3.Dot(m.Normals[m.Triangles[i]], n.Normalized) > 0.999f);
            }
        }
    }

    [Fact]
    public void MissingMaterialFileUsesTheFallbackColour()
    {
        List<ColoredMesh> parts = ObjModel.Parse(Cube, null, Grey);
        Assert.All(parts, p => Assert.Equal(128, p.Color.R));
    }

    [Fact]
    public void NegativeIndicesCountFromTheEnd()
    {
        List<ColoredMesh> parts = ObjModel.Parse("v 0 0 0\nv 1 0 0\nv 0 1 0\nf -3 -2 -1\n", null, Grey);
        Assert.Single(parts);
        Assert.Equal(3, parts[0].Mesh.Triangles.Count);
    }

    [Fact]
    public void BadLinesAreSkippedNotThrown()
    {
        string text = "v 0 0 0\r\nv 1 0 0\r\nv 0 1 0\r\nv oops 1 2\r\nf 1 2 99\r\nf 1 2\r\nf a b c\r\nf 1 1 1\r\nnonsense line\r\nf 1 2 3\r\n";
        List<ColoredMesh> parts = ObjModel.Parse(text, "Kd 1 1 1\nnewmtl\nKd x y z\n", Grey);
        Assert.Single(parts);
        Assert.Equal(3, parts[0].Mesh.Triangles.Count);
    }

    [Fact]
    public void EmptyOrNullTextGivesNoParts()
    {
        Assert.Empty(ObjModel.Parse(null, null, Grey));
        Assert.Empty(ObjModel.Parse("", "", Grey));
        Assert.Empty(ObjModel.Parse("v 0 0 0\nv 1 0 0\nv 0 1 0\n", null, Grey));
    }

    [Fact]
    public void CommaDecimalsAreNotAccepted()
    {
        // Culture-proof: "0,5" must not be read as 0.5 (or as 5).
        List<ColoredMesh> parts = ObjModel.Parse("v 0 0 0\nv 0,5 0 0\nv 0 1 0\nv 1 0 0\nf 1 4 3\n", null, Grey);
        Assert.Equal(-1f, parts[0].Mesh.Min.X, 3);
    }

    [Fact]
    public void ColoursAreClampedToBytes()
    {
        Dictionary<string, Rgb> c = ObjModel.ParseMaterials("newmtl a\nKd 2 -1 0.5\n");
        Assert.Equal(255, c["a"].R); Assert.Equal(0, c["a"].G); Assert.Equal(128, c["a"].B);
    }
}
