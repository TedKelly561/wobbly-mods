using System;
using System.Collections.Generic;
using System.Globalization;
using WobblyMods.Core.Shapes;
using Xunit;

public class ShapesCurvesTests
{
    // ---- helpers ----------------------------------------------------------------------------

    private static string Key(V3 v)
    {
        // "+ 0f" turns -0.0 into 0.0 so a point never gets two different texts.
        string x = (Math.Round(v.X, 4) + 0.0).ToString("F4", CultureInfo.InvariantCulture);
        string y = (Math.Round(v.Y, 4) + 0.0).ToString("F4", CultureInfo.InvariantCulture);
        string z = (Math.Round(v.Z, 4) + 0.0).ToString("F4", CultureInfo.InvariantCulture);
        return x + "," + y + "," + z;
    }

    private static void ValidateBasic(ShapeMesh mesh)
    {
        Assert.True(mesh.Triangles.Count > 0);
        Assert.Equal(0, mesh.Triangles.Count % 3);
        Assert.Equal(mesh.Vertices.Count, mesh.Normals.Count);

        foreach (int idx in mesh.Triangles)
            Assert.InRange(idx, 0, mesh.Vertices.Count - 1);

        foreach (V3 n in mesh.Normals)
            Assert.True(Math.Abs(n.Length - 1f) < 0.001f, "normal length " + n.Length);

        for (int i = 0; i < mesh.Triangles.Count; i += 3)
        {
            V3 a = mesh.Vertices[mesh.Triangles[i]];
            V3 b = mesh.Vertices[mesh.Triangles[i + 1]];
            V3 c = mesh.Vertices[mesh.Triangles[i + 2]];
            Assert.True(V3.Cross(b - a, c - a).Length / 2f > 1e-7f, "degenerate triangle " + (i / 3));
        }
    }

    // Every triangle must point away from the middle of the bounds (only valid for convex shapes).
    private static void ValidateOutwardFromCenter(ShapeMesh mesh)
    {
        V3 center = (mesh.Min + mesh.Max) / 2f;
        for (int i = 0; i < mesh.Triangles.Count; i += 3)
        {
            V3 a = mesh.Vertices[mesh.Triangles[i]];
            V3 b = mesh.Vertices[mesh.Triangles[i + 1]];
            V3 c = mesh.Vertices[mesh.Triangles[i + 2]];
            float dot = V3.Dot(V3.Cross(b - a, c - a), (a + b + c) / 3f - center);
            Assert.True(dot > 0f, "triangle " + (i / 3) + " points inward (dot " + dot + ")");
        }
    }

    // Works for any shape: the winding must agree with the vertex normals the mesh itself carries.
    private static void ValidateWindingMatchesNormals(ShapeMesh mesh)
    {
        for (int i = 0; i < mesh.Triangles.Count; i += 3)
        {
            int ia = mesh.Triangles[i];
            V3 a = mesh.Vertices[ia];
            V3 b = mesh.Vertices[mesh.Triangles[i + 1]];
            V3 c = mesh.Vertices[mesh.Triangles[i + 2]];
            V3 faceNormal = V3.Cross(b - a, c - a).Normalized;
            float agree = V3.Dot(faceNormal, mesh.Normals[ia]);
            Assert.True(agree > 0.5f, "triangle " + (i / 3) + " winding disagrees with its normal (" + agree + ")");
        }
    }

    // Every edge, by vertex POSITION rounded to 1e-4, is used by exactly 2 triangles.
    private static void ValidateClosed(ShapeMesh mesh)
    {
        var counts = new Dictionary<string, int>();
        for (int i = 0; i < mesh.Triangles.Count; i += 3)
        {
            string[] k =
            {
                Key(mesh.Vertices[mesh.Triangles[i]]),
                Key(mesh.Vertices[mesh.Triangles[i + 1]]),
                Key(mesh.Vertices[mesh.Triangles[i + 2]]),
            };
            for (int e = 0; e < 3; e++)
            {
                string p = k[e], q = k[(e + 1) % 3];
                string edge = string.CompareOrdinal(p, q) < 0 ? p + "|" + q : q + "|" + p;
                int existing;
                counts.TryGetValue(edge, out existing);
                counts[edge] = existing + 1;
            }
        }
        foreach (var pair in counts)
            Assert.True(pair.Value == 2, "edge " + pair.Key + " is used " + pair.Value + " time(s)");
    }

    private static List<V3> CatTailPath()
    {
        return new List<V3> { new V3(0f, 0f, 0f), new V3(0f, 0.3f, -0.3f), new V3(0f, 0.6f, 0f), new V3(0f, 0.5f, 0.3f) };
    }

    private static List<V3> Square(bool counterClockwise)
    {
        var pts = new List<V3> { new V3(-1f, -1f, 0f), new V3(1f, -1f, 0f), new V3(1f, 1f, 0f), new V3(-1f, 1f, 0f) };
        if (!counterClockwise) pts.Reverse();
        return pts;
    }

    // A square with a V-shaped notch in the bottom edge: not convex, but star-shaped around its centroid.
    private static List<V3> NotchedFin()
    {
        return new List<V3> { new V3(-1f, -1f, 0f), new V3(0f, -0.4f, 0f), new V3(1f, -1f, 0f), new V3(1f, 1f, 0f), new V3(-1f, 1f, 0f) };
    }

    private static int CountNear(ShapeMesh mesh, V3 p)
    {
        int count = 0;
        foreach (V3 v in mesh.Vertices)
            if ((v - p).Length < 1e-4f) count++;
        return count;
    }

    // ---- Bezier -----------------------------------------------------------------------------

    [Fact]
    public void Bezier_EndpointsAreExact()
    {
        V3 p0 = new V3(1f, 2f, 3f), p3 = new V3(10f, 11f, 12f);
        var points = Shapes.Bezier(p0, new V3(4f, 5f, 6f), new V3(7f, 8f, 9f), p3, 10);
        Assert.Equal(p0, points[0]);
        Assert.Equal(p3, points[points.Count - 1]);
    }

    [Fact]
    public void Bezier_CountIsStepsPlusOne()
    {
        Assert.Equal(11, Shapes.Bezier(V3.Zero, V3.Up, V3.Forward, V3.Right, 10).Count);
        Assert.Equal(2, Shapes.Bezier(V3.Zero, V3.Up, V3.Forward, V3.Right, 1).Count);
    }

    [Fact]
    public void Bezier_StraightLineStaysOnTheLine()
    {
        var points = Shapes.Bezier(new V3(0f, 0f, 0f), new V3(0f, 1f, 0f), new V3(0f, 2f, 0f), new V3(0f, 3f, 0f), 6);
        foreach (V3 p in points)
        {
            Assert.True(Math.Abs(p.X) < 1e-5f && Math.Abs(p.Z) < 1e-5f);
        }
        Assert.True(Math.Abs(points[3].Y - 1.5f) < 1e-4f); // the middle point of an evenly spaced line
    }

    [Fact]
    public void Bezier_BadStepsThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Shapes.Bezier(V3.Zero, V3.Up, V3.Forward, V3.Right, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Shapes.Bezier(V3.Zero, V3.Up, V3.Forward, V3.Right, -3));
    }

    // ---- Tube -------------------------------------------------------------------------------

    [Fact]
    public void Tube_StraightAlongY_HasExpectedBounds()
    {
        var mesh = Shapes.Tube(new List<V3> { V3.Zero, new V3(0f, 10f, 0f) }, new List<float> { 0.5f, 0.5f }, 16);
        Assert.True(Math.Abs(mesh.Min.X + 0.5f) < 0.01f && Math.Abs(mesh.Max.X - 0.5f) < 0.01f);
        Assert.True(Math.Abs(mesh.Min.Y) < 0.01f && Math.Abs(mesh.Max.Y - 10f) < 0.01f);
        Assert.True(Math.Abs(mesh.Min.Z + 0.5f) < 0.01f && Math.Abs(mesh.Max.Z - 0.5f) < 0.01f);
    }

    [Fact]
    public void Tube_Straight_IsClosedAndPointsOutward()
    {
        var mesh = Shapes.Tube(new List<V3> { V3.Zero, new V3(0f, 2f, 0f) }, new List<float> { 0.5f, 0.5f }, 12);
        ValidateBasic(mesh);
        ValidateClosed(mesh);
        ValidateOutwardFromCenter(mesh);
        ValidateWindingMatchesNormals(mesh);
    }

    [Fact]
    public void Tube_Curved_IsClosedWithUnitNormalsAndNoDegenerateTriangles()
    {
        var mesh = Shapes.Tube(CatTailPath(), new List<float> { 0.2f, 0.2f, 0.2f, 0.2f }, 16);
        ValidateBasic(mesh); // unit normals, no degenerate triangles, indices in range
        ValidateClosed(mesh);
    }

    [Fact]
    public void Tube_Curved_WindingAgreesWithNormals()
    {
        var mesh = Shapes.Tube(CatTailPath(), new List<float> { 0.2f, 0.2f, 0.2f, 0.2f }, 16);
        ValidateWindingMatchesNormals(mesh);
    }

    [Fact]
    public void Tube_Curved_RingsKeepTheirRadius()
    {
        var path = CatTailPath();
        var radii = new List<float> { 0.2f, 0.15f, 0.1f, 0.05f };
        int segments = 16;
        var mesh = Shapes.Tube(path, radii, segments);

        // With every radius above 0 the side rings come first, segments + 1 vertices each.
        for (int ring = 0; ring < path.Count; ring++)
        {
            for (int j = 0; j <= segments; j++)
            {
                float distance = (mesh.Vertices[ring * (segments + 1) + j] - path[ring]).Length;
                Assert.True(Math.Abs(distance - radii[ring]) < radii[ring] * 0.01f,
                    "ring " + ring + " vertex " + j + " is " + distance + " from its center");
            }
        }
    }

    [Fact]
    public void Tube_Curved_RingsDoNotTwist()
    {
        var path = CatTailPath();
        int segments = 16;
        var mesh = Shapes.Tube(path, new List<float> { 0.2f, 0.2f, 0.2f, 0.2f }, segments);

        // Vertex 0 of each ring must stay on the same side as vertex 0 of the next ring.
        for (int ring = 0; ring + 1 < path.Count; ring++)
        {
            V3 a = (mesh.Vertices[ring * (segments + 1)] - path[ring]).Normalized;
            V3 b = (mesh.Vertices[(ring + 1) * (segments + 1)] - path[ring + 1]).Normalized;
            Assert.True(V3.Dot(a, b) > 0.5f, "ring " + ring + " twists against ring " + (ring + 1));
        }
    }

    [Fact]
    public void Tube_LastRadiusZero_EndsInOnePoint()
    {
        var mesh = Shapes.Tube(new List<V3> { V3.Zero, new V3(0f, 1f, 0f) }, new List<float> { 0.5f, 0f }, 16);
        ValidateBasic(mesh);
        ValidateClosed(mesh);
        Assert.Equal(1, CountNear(mesh, new V3(0f, 1f, 0f)));
    }

    [Fact]
    public void Tube_FirstRadiusZero_StartsInOnePoint()
    {
        var mesh = Shapes.Tube(new List<V3> { V3.Zero, new V3(0f, 1f, 0f) }, new List<float> { 0f, 0.5f }, 16);
        ValidateBasic(mesh);
        ValidateClosed(mesh);
        Assert.Equal(1, CountNear(mesh, V3.Zero));
    }

    [Fact]
    public void Tube_TaperedTail_IsClosedAndOutward()
    {
        var path = Shapes.Bezier(V3.Zero, new V3(0f, 0.3f, -0.3f), new V3(0f, 0.6f, 0f), new V3(0f, 0.5f, 0.3f), 8);
        var radii = new List<float>();
        for (int i = 0; i < path.Count; i++) radii.Add(0.2f * (1f - i / (float)(path.Count - 1)));
        var mesh = Shapes.Tube(path, radii, 10);
        ValidateBasic(mesh);
        ValidateClosed(mesh);
        ValidateWindingMatchesNormals(mesh);
    }

    [Fact]
    public void Tube_BadArgumentsThrow()
    {
        var two = new List<V3> { V3.Zero, new V3(0f, 1f, 0f) };
        Assert.Throws<ArgumentException>(() => Shapes.Tube(new List<V3> { V3.Zero }, new List<float> { 1f }));
        Assert.Throws<ArgumentException>(() => Shapes.Tube(two, new List<float> { 1f }));
        Assert.Throws<ArgumentException>(() => Shapes.Tube(two, new List<float> { -1f, 1f }));
        Assert.Throws<ArgumentException>(() => Shapes.Tube(two, new List<float> { 0f, 0f }));
        Assert.Throws<ArgumentOutOfRangeException>(() => Shapes.Tube(two, new List<float> { 1f, 1f }, 2));
    }

    // ---- Extrude ----------------------------------------------------------------------------

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Extrude_Square_IsClosedAndPointsOutward(bool counterClockwise)
    {
        var mesh = Shapes.Extrude(Square(counterClockwise), 2f);
        ValidateBasic(mesh);
        ValidateClosed(mesh);
        ValidateOutwardFromCenter(mesh);
        ValidateWindingMatchesNormals(mesh);
    }

    [Fact]
    public void Extrude_Square_TriangleCount()
    {
        // Front fan: 4, back fan: 4, four side quads of 2 triangles: 8.
        Assert.Equal(16, Shapes.Extrude(Square(true), 2f).Triangles.Count / 3);
    }

    [Fact]
    public void Extrude_Square_HasTheRightThickness()
    {
        var mesh = Shapes.Extrude(Square(true), 2f);
        Assert.True(Math.Abs(mesh.Min.Z + 1f) < 1e-4f && Math.Abs(mesh.Max.Z - 1f) < 1e-4f);
        Assert.True(Math.Abs(mesh.Min.X + 1f) < 1e-4f && Math.Abs(mesh.Max.X - 1f) < 1e-4f);
    }

    [Fact]
    public void Extrude_Square_FacesAreFlat()
    {
        // Hard edges: every normal is one of the six axis directions.
        var mesh = Shapes.Extrude(Square(true), 2f);
        foreach (V3 n in mesh.Normals)
        {
            float biggest = Math.Max(Math.Abs(n.X), Math.Max(Math.Abs(n.Y), Math.Abs(n.Z)));
            Assert.True(biggest > 0.999f, "normal " + n + " is not axis aligned");
        }
    }

    [Fact]
    public void Extrude_NotchedFin_IsClosedAndWindsOutward()
    {
        var mesh = Shapes.Extrude(NotchedFin(), 0.5f);
        ValidateBasic(mesh);
        ValidateClosed(mesh);
        ValidateWindingMatchesNormals(mesh); // not convex, so check each triangle against its own normal
    }

    [Fact]
    public void Extrude_NotchedFin_SameForEitherOrder()
    {
        var forward = NotchedFin();
        var reversed = NotchedFin();
        reversed.Reverse();
        Assert.Equal(Shapes.Extrude(forward, 0.5f).Triangles.Count, Shapes.Extrude(reversed, 0.5f).Triangles.Count);
        ValidateClosed(Shapes.Extrude(reversed, 0.5f));
        ValidateWindingMatchesNormals(Shapes.Extrude(reversed, 0.5f));
    }

    [Fact]
    public void Extrude_BadArgumentsThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Shapes.Extrude(new List<V3> { V3.Zero, V3.Right }, 1f));
        Assert.Throws<ArgumentOutOfRangeException>(() => Shapes.Extrude(Square(true), 0f));
        Assert.Throws<ArgumentOutOfRangeException>(() => Shapes.Extrude(Square(true), -1f));
    }
}
