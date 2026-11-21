using System;
using System.Collections.Generic;
using System.IO;
using WobblyMods.Core.Shapes;
using Xunit;

public class SkylarFurnitureTests
{
    public static IEnumerable<object[]> AllIds()
    {
        foreach (string id in SkylarFurniture.Ids) yield return new object[] { id };
    }

    private static void Bounds(List<ColoredMesh> parts, out V3 min, out V3 max)
    {
        min = parts[0].Mesh.Min; max = parts[0].Mesh.Max;
        foreach (ColoredMesh p in parts)
        {
            V3 a = p.Mesh.Min, b = p.Mesh.Max;
            min = new V3(Math.Min(min.X, a.X), Math.Min(min.Y, a.Y), Math.Min(min.Z, a.Z));
            max = new V3(Math.Max(max.X, b.X), Math.Max(max.Y, b.Y), Math.Max(max.Z, b.Z));
        }
    }

    [Theory, MemberData(nameof(AllIds))]
    public void StandsOnTheFloorCenteredAndFurnitureSized(string id)
    {
        List<ColoredMesh> parts = SkylarFurniture.Build(id);
        Assert.NotEmpty(parts);
        V3 min, max;
        Bounds(parts, out min, out max);
        Assert.Equal(0f, min.Y, 3);
        Assert.Equal(0f, min.X + max.X, 2);
        Assert.Equal(0f, min.Z + max.Z, 2);
        Assert.InRange(max.Y, 0.02f, 1.6f);
        Assert.InRange(max.X - min.X, 0.2f, 1.5f);
        Assert.InRange(max.Z - min.Z, 0.2f, 1.5f);
    }

    [Theory, MemberData(nameof(AllIds))]
    public void MeshesAreValid(string id)
    {
        foreach (ColoredMesh p in SkylarFurniture.Build(id))
        {
            ShapeMesh m = p.Mesh;
            Assert.Equal(m.Vertices.Count, m.Normals.Count);
            Assert.Equal(0, m.Triangles.Count % 3);
            foreach (int i in m.Triangles) Assert.InRange(i, 0, m.Vertices.Count - 1);
            foreach (V3 n in m.Normals) Assert.InRange(n.Length, 0.999f, 1.001f);
        }
    }

    [Theory, MemberData(nameof(AllIds))]
    public void HasANameAndIsDeterministic(string id)
    {
        Assert.NotEqual(id, SkylarFurniture.NiceName(id));
        List<ColoredMesh> a = SkylarFurniture.Build(id), b = SkylarFurniture.Build(id);
        Assert.Equal(a.Count, b.Count);
        for (int i = 0; i < a.Count; i++) Assert.Equal(a[i].Mesh.Vertices.Count, b[i].Mesh.Vertices.Count);
    }

    [Fact]
    public void UnknownIdThrows()
    {
        Assert.Throws<ArgumentException>(() => SkylarFurniture.Build("spaceship"));
    }

    [Fact]
    public void TheRugIsFlatAndTheTruckWheelsTouchTheFloor()
    {
        V3 min, max;
        Bounds(SkylarFurniture.Build("fishrug"), out min, out max);
        Assert.True(max.Y < 0.06f);
        List<ColoredMesh> truck = SkylarFurniture.Build("monstertruck");
        int wheelsOnFloor = 0;
        foreach (ColoredMesh p in truck) if (p.Mesh.Min.Y < 0.001f) wheelsOnFloor++;
        Assert.Equal(4, wheelsOnFloor);
    }

    [Fact]
    public void WritesPreviewPictures()
    {
        string dir = Path.Combine(AppContext.BaseDirectory, "..", "..", "preview", "furniture");
        Directory.CreateDirectory(dir);
        foreach (string id in SkylarFurniture.Ids)
        {
            string path = Path.Combine(dir, id + ".bmp");
            // Turned a little and tipped toward the camera, so tops (tables, rugs) can be seen.
            var tipped = new List<ColoredMesh>();
            V3 one = new V3(1f, 1f, 1f);
            foreach (ColoredMesh part in SkylarFurniture.Build(id))
                tipped.Add(new ColoredMesh(part.Mesh.Transformed(one, 35f, 0f, V3.Zero).Transformed(one, 0f, 25f, V3.Zero), part.Color));
            Preview.SaveBmp(path, tipped, 360, 360, 0f, new Rgb(120, 170, 220));
            Assert.True(new FileInfo(path).Length > 1000);
        }
    }
}
