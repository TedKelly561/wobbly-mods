using System;
using System.Collections.Generic;
using System.Linq;
using WobblyMods.Core.Shapes;
using Xunit;

public class ShapesTests
{
    private static void ValidateBasic(ShapeMesh mesh)
    {
        Assert.True(mesh.Triangles.Count % 3 == 0, "Triangle count must be a multiple of 3");
        Assert.Equal(mesh.Vertices.Count, mesh.Normals.Count);
        
        foreach (int idx in mesh.Triangles)
        {
            Assert.InRange(idx, 0, mesh.Vertices.Count - 1);
        }

        foreach (V3 norm in mesh.Normals)
        {
            float len = norm.Length;
            Assert.True(Math.Abs(len - 1f) < 0.001f, $"Normal length {len} is not 1");
        }

        for (int i = 0; i < mesh.Triangles.Count; i += 3)
        {
            V3 a = mesh.Vertices[mesh.Triangles[i]];
            V3 b = mesh.Vertices[mesh.Triangles[i + 1]];
            V3 c = mesh.Vertices[mesh.Triangles[i + 2]];
            
            V3 edge1 = b - a;
            V3 edge2 = c - a;
            V3 cross = V3.Cross(edge1, edge2);
            float area = cross.Length / 2f;
            Assert.True(area > 1e-7f, "Degenerate triangle detected");
        }
    }

    private static void ValidateWinding(ShapeMesh mesh)
    {
        V3 center = (mesh.Min + mesh.Max) / 2f;
        
        for (int i = 0; i < mesh.Triangles.Count; i += 3)
        {
            int idxA = mesh.Triangles[i];
            int idxB = mesh.Triangles[i + 1];
            int idxC = mesh.Triangles[i + 2];

            V3 a = mesh.Vertices[idxA];
            V3 b = mesh.Vertices[idxB];
            V3 c = mesh.Vertices[idxC];

            V3 edge1 = b - a;
            V3 edge2 = c - a;
            V3 normal = V3.Cross(edge1, edge2); // Outward if winding is correct
            
            V3 centerToTri = (a + b + c) / 3f - center;
            
            float dot = V3.Dot(normal, centerToTri);
            Assert.True(dot > 0.001f, $"Winding failed for triangle {i/3}. Dot: {dot}");
        }
    }

    private static void ValidateClosed(ShapeMesh mesh)
    {
        // Group edges by rounded position
        var edgeCounts = new Dictionary<(int v1, int v2), int>();
        
        for (int i = 0; i < mesh.Triangles.Count; i += 3)
        {
            int a = mesh.Triangles[i];
            int b = mesh.Triangles[i + 1];
            int c = mesh.Triangles[i + 2];

            V3 va = mesh.Vertices[a];
            V3 vb = mesh.Vertices[b];
            V3 vc = mesh.Vertices[c];

            // Helper to add edge key
            void AddEdge(int i1, int i2)
            {
                V3 p1 = mesh.Vertices[i1];
                V3 p2 = mesh.Vertices[i2];
                
                // Round positions to 1e-4 to match vertices
                string k1 = $"{p1.X.ToString("F4")},{p1.Y.ToString("F4")},{p1.Z.ToString("F4")}";
                string k2 = $"{p2.X.ToString("F4")},{p2.Y.ToString("F4")},{p2.Z.ToString("F4")}";
                
                // Create a canonical key (smaller index first? No, position based)
                // Actually, the requirement says "by vertex POSITION". So we match positions.
                // But Dictionary keys need to be consistent. Let's use string representation of rounded coords.
                // To make it unique and order-independent for an edge:
                if (k1.CompareTo(k2) < 0)
                    edgeCounts[(i1, i2)] = edgeCounts.GetValueOrDefault((i1, i2), 0) + 1; // This is wrong logic for position matching.
                
                // Correct approach: Map rounded position to a representative index or just count occurrences of the pair of rounded positions.
            }
            
            // Simpler: Just check that every edge defined by rounded coordinates appears exactly twice in the mesh.
        }
        
        var posEdgeCounts = new Dictionary<(string p1, string p2), int>();
        
        for (int i = 0; i < mesh.Triangles.Count; i += 3)
        {
            int a = mesh.Triangles[i];
            int b = mesh.Triangles[i + 1];
            int c = mesh.Triangles[i + 2];

            V3 va = mesh.Vertices[a];
            V3 vb = mesh.Vertices[b];
            V3 vc = mesh.Vertices[c];

            string sA = $"{va.X.ToString("F4")},{va.Y.ToString("F4")},{va.Z.ToString("F4")}";
            string sB = $"{vb.X.ToString("F4")},{vb.Y.ToString("F4")},{vb.Z.ToString("F4")}";
            string sC = $"{vc.X.ToString("F4")},{vc.Y.ToString("F4")},{vc.Z.ToString("F4")}";

            void AddEdge(string p1, string p2)
            {
                if (p1.CompareTo(p2) < 0)
                {
                    posEdgeCounts[(p1, p2)] = posEdgeCounts.GetValueOrDefault((p1, p2), 0) + 1;
                }
                else
                {
                    posEdgeCounts[(p2, p1)] = posEdgeCounts.GetValueOrDefault((p2, p1), 0) + 1;
                }
            }

            AddEdge(sA, sB);
            AddEdge(sB, sC);
            AddEdge(sC, sA);
        }

        foreach (var kvp in posEdgeCounts)
        {
            Assert.Equal(2, kvp.Value);
        }
    }

    [Fact]
    public void Ellipsoid_BasicValidation()
    {
        var mesh = Shapes.Ellipsoid(new V3(1f, 2f, 3f), 16, 10);
        ValidateBasic(mesh);
        ValidateWinding(mesh);
        ValidateClosed(mesh);
        
        Assert.True(Math.Abs(mesh.Min.X - (-1f)) < 0.01f);
        Assert.True(Math.Abs(mesh.Max.X - 1f) < 0.01f);
        Assert.True(Math.Abs(mesh.Min.Y - (-2f)) < 0.01f);
        Assert.True(Math.Abs(mesh.Max.Y - 2f) < 0.01f);
        Assert.True(Math.Abs(mesh.Min.Z - (-3f)) < 0.01f);
        Assert.True(Math.Abs(mesh.Max.Z - 3f) < 0.01f);
    }

    [Fact]
    public void Cone_BasicValidation()
    {
        var mesh = Shapes.Cone(0.5f, 2f, 16);
        ValidateBasic(mesh);
        ValidateWinding(mesh);
        ValidateClosed(mesh);
        
        Assert.True(Math.Abs(mesh.Max.Y - 2f) < 0.01f);
        Assert.True(Math.Abs(mesh.Min.Y - 0f) < 0.01f);
    }

    [Fact]
    public void Cylinder_BasicValidation()
    {
        var mesh = Shapes.Cylinder(1f, 5f, 12);
        ValidateBasic(mesh);
        ValidateWinding(mesh);
        ValidateClosed(mesh);
        
        Assert.True(Math.Abs(mesh.Max.Y - 5f) < 0.01f);
        Assert.True(Math.Abs(mesh.Min.Y - 0f) < 0.01f);
    }

    [Fact]
    public void Ellipsoid_Arguments()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Shapes.Ellipsoid(new V3(1, 1, 1), segments: 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => Shapes.Ellipsoid(new V3(1, 1, 1), rings: 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Shapes.Ellipsoid(new V3(-1, 1, 1)));
    }

    [Fact]
    public void Cone_Arguments()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Shapes.Cone(1f, 1f, segments: 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => Shapes.Cone(0f, 1f));
        Assert.Throws<ArgumentOutOfRangeException>(() => Shapes.Cone(1f, -1f));
    }

    [Fact]
    public void Cylinder_Arguments()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Shapes.Cylinder(1f, 1f, segments: 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => Shapes.Cylinder(0f, 1f));
        Assert.Throws<ArgumentOutOfRangeException>(() => Shapes.Cylinder(1f, 0f));
    }

    [Fact]
    public void Append_OffsetsIndices()
    {
        var m1 = Shapes.Ellipsoid(new V3(1, 1, 1), 4, 2);
        var m2 = Shapes.Cone(1f, 1f, 4);
        
        int countBefore = m1.Vertices.Count;
        int trianglesBefore = m1.Triangles.Count;
        m1.Append(m2);

        Assert.Equal(countBefore + m2.Vertices.Count, m1.Vertices.Count);
        Assert.Equal(trianglesBefore + m2.Triangles.Count, m1.Triangles.Count);

        // m2's triangles come after m1's own, and their indices are offset by countBefore
        for (int i = 0; i < m2.Triangles.Count; i++)
        {
            int expectedIdx = m2.Triangles[i] + countBefore;
            Assert.Equal(expectedIdx, m1.Triangles[trianglesBefore + i]);
        }
    }

    [Fact]
    public void Transformed_Yaw90()
    {
        // Create a simple mesh with a vertex at Forward (0,0,1)
        var mesh = new ShapeMesh();
        int idx = mesh.AddVertex(new V3(0f, 0f, 1f), V3.Forward);
        mesh.AddTriangle(idx, idx, idx); // Degenerate but enough for vector test
        
        var transformed = mesh.Transformed(new V3(1f, 1f, 1f), 90f, 0f, V3.Zero);
        
        // Forward (0,0,1) rotated 90 deg around Y should become Right (1,0,0)
        V3 newVert = transformed.Vertices[0];
        Assert.True(Math.Abs(newVert.X - 1f) < 0.001f);
        Assert.True(Math.Abs(newVert.Y) < 0.001f);
        Assert.True(Math.Abs(newVert.Z) < 0.001f);
    }

    [Fact]
    public void Transformed_Offset()
    {
        var mesh = new ShapeMesh();
        mesh.AddVertex(V3.Zero, V3.Zero);
        
        V3 offset = new V3(10f, 20f, 30f);
        var transformed = mesh.Transformed(new V3(1f, 1f, 1f), 0f, 0f, offset);
        
        Assert.True(Math.Abs(transformed.Vertices[0].X - 10f) < 0.001f);
        Assert.True(Math.Abs(transformed.Vertices[0].Y - 20f) < 0.001f);
        Assert.True(Math.Abs(transformed.Vertices[0].Z - 30f) < 0.001f);
    }

    [Fact]
    public void Transformed_NormalsRotated()
    {
        var mesh = new ShapeMesh();
        // Vertex at (1,0,0) with normal (1,0,0)
        int idx = mesh.AddVertex(new V3(1f, 0f, 0f), V3.Right);
        mesh.AddTriangle(idx, idx, idx);
        
        // Yaw 90 turns Forward (0,0,1) into Right (1,0,0), so Right (1,0,0) becomes Back (0,0,-1)
        var transformed = mesh.Transformed(new V3(1f, 1f, 1f), 90f, 0f, V3.Zero);

        V3 newNorm = transformed.Normals[0];
        Assert.True(Math.Abs(newNorm.X) < 0.001f);
        Assert.True(Math.Abs(newNorm.Y) < 0.001f);
        Assert.True(Math.Abs(newNorm.Z - (-1f)) < 0.001f);
    }

    [Fact]
    public void Bounds_EmptyMesh()
    {
        var mesh = new ShapeMesh();
        Assert.Equal(V3.Zero, mesh.Min);
        Assert.Equal(V3.Zero, mesh.Max);
    }

    [Fact]
    public void V3_Normalized_ZeroStaysZero()
    {
        V3 zero = V3.Zero;
        Assert.Equal(V3.Zero, zero.Normalized);
    }

    [Fact]
    public void V3_DotProduct()
    {
        V3 a = new V3(1f, 0f, 0f);
        V3 b = new V3(0f, 1f, 0f);
        Assert.Equal(0f, V3.Dot(a, b));
        
        V3 c = new V3(1f, 2f, 3f);
        V3 d = new V3(1f, 2f, 3f);
        Assert.Equal(14f, V3.Dot(c, d));
    }

    [Fact]
    public void V3_CrossProduct()
    {
        V3 a = V3.Right; // (1,0,0)
        V3 b = V3.Up;    // (0,1,0)
        V3 c = V3.Cross(a, b);
        
        Assert.Equal(0f, c.X);
        Assert.Equal(0f, c.Y);
        Assert.Equal(1f, c.Z); // Forward
    }

    [Fact]
    public void V3_Lerp()
    {
        V3 a = new V3(0f, 0f, 0f);
        V3 b = new V3(10f, 10f, 10f);
        
        V3 mid = V3.Lerp(a, b, 0.5f);
        Assert.Equal(5f, mid.X);
        Assert.Equal(5f, mid.Y);
        Assert.Equal(5f, mid.Z);
    }

    [Fact]
    public void V3_Operators()
    {
        V3 a = new V3(1f, 2f, 3f);
        V3 b = new V3(4f, 5f, 6f);
        
        Assert.Equal(new V3(5f, 7f, 9f), a + b);
        Assert.Equal(new V3(-3f, -3f, -3f), a - b);
        Assert.Equal(new V3(2f, 4f, 6f), a * 2f);
        Assert.Equal(new V3(0.5f, 1f, 1.5f), a / 2f);
        Assert.Equal(new V3(-1f, -2f, -3f), -a);
    }
}
