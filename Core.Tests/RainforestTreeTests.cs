using System;
using System.Collections.Generic;
using System.IO;
using WobblyMods.Core.Shapes;
using Xunit;

public class RainforestTreeTests
{
    public static IEnumerable<object[]> AllVariants()
    {
        for (int i = 0; i < RainforestTree.Variants; i++) yield return new object[] { i };
    }

    private static void Check(IEnumerable<ColoredMesh> parts)
    {
        foreach (ColoredMesh p in parts)
        {
            ShapeMesh m = p.Mesh;
            Assert.Equal(m.Vertices.Count, m.Normals.Count);
            Assert.Equal(0, m.Triangles.Count % 3);
            foreach (int i in m.Triangles) Assert.InRange(i, 0, m.Vertices.Count - 1);
            foreach (V3 n in m.Normals) Assert.InRange(n.Length, 0.99f, 1.01f);
        }
    }

    [Theory, MemberData(nameof(AllVariants))]
    public void TreeIsFewMeshesAndTreeSized(int variant)
    {
        List<ColoredMesh> parts = RainforestTree.Build(variant);
        Assert.InRange(parts.Count, 3, 4);                 // merged by color: this is what keeps sixty of them cheap
        Check(parts);
        float minY = 99f, maxY = -99f, maxR = 0f;
        int triangles = 0;
        foreach (ColoredMesh p in parts)
        {
            triangles += p.Mesh.Triangles.Count / 3;
            minY = Math.Min(minY, p.Mesh.Min.Y); maxY = Math.Max(maxY, p.Mesh.Max.Y);
            maxR = Math.Max(maxR, Math.Max(Math.Max(Math.Abs(p.Mesh.Min.X), p.Mesh.Max.X), Math.Max(Math.Abs(p.Mesh.Min.Z), p.Mesh.Max.Z)));
        }
        Assert.InRange(minY, -0.2f, 0.05f);
        Assert.InRange(maxY, 7f, 10.5f);
        Assert.InRange(maxR, 2.5f, 6.5f);
        Assert.True(triangles < 6000, "too many triangles for a forest of sixty: " + triangles);
        Assert.Equal(maxY, RainforestTree.Height(variant), 2);
    }

    [Theory, MemberData(nameof(AllVariants))]
    public void TreeIsTheSameEveryTimeAndHasPerches(int variant)
    {
        List<ColoredMesh> a = RainforestTree.Build(variant), b = RainforestTree.Build(variant);
        Assert.Equal(a.Count, b.Count);
        for (int i = 0; i < a.Count; i++) Assert.Equal(a[i].Mesh.Vertices.Count, b[i].Mesh.Vertices.Count);
        // Birds sit where they can be seen: every perch is clear of the leaves (head included) and on wood.
        List<V3> low = RainforestTree.LowPerches(variant), high = RainforestTree.Perches(variant);
        Assert.InRange(low.Count, 3, 4);
        foreach (V3 p in low) Assert.InRange(p.Y, 0.5f, 4.6f);
        foreach (V3 p in high) Assert.InRange(p.Y, 4f, 9f);
        ShapeMesh wood = a[0].Mesh;
        var all = new List<V3>(low);
        all.AddRange(high);
        foreach (V3 p in all)
        {
            Assert.False(RainforestTree.InLeaves(variant, p), "perch inside the leaves");
            Assert.False(RainforestTree.InLeaves(variant, p + new V3(0f, 0.6f, 0f)), "bird's head inside the leaves");
            float drop;
            Assert.True(wood.TryDropOnto(p + new V3(0f, 0.3f, 0f), 1f, out drop), "no wood under the bird");
            Assert.InRange(drop - 0.3f, -0.02f, 0.02f);
        }
        Assert.Equal(RainforestTree.Build(variant + RainforestTree.Variants)[0].Mesh.Vertices.Count, a[0].Mesh.Vertices.Count); // wraps round
    }

    [Fact]
    public void VariantsDiffer()
    {
        Assert.NotEqual(RainforestTree.Height(0), RainforestTree.Height(1));
    }

    [Fact]
    public void VineGateSpansItsWidthAndLeavesHeadroomForAWobbly()
    {
        List<ColoredMesh> gate = RainforestTree.VineGate(0, 3.2f, 4.5f);
        Assert.Equal(3, gate.Count);
        Check(gate);
        float minX = 9f, maxX = -9f, maxY = 0f;
        foreach (ColoredMesh p in gate) { minX = Math.Min(minX, p.Mesh.Min.X); maxX = Math.Max(maxX, p.Mesh.Max.X); maxY = Math.Max(maxY, p.Mesh.Max.Y); }
        Assert.InRange(maxX - minX, 3.2f, 4.2f);
        Assert.InRange(maxY, 4.5f, 5f);
        // The vines (dark green) stop above the floor: they brush a Wobbly, they don't reach the ground.
        Assert.True(gate[2].Mesh.Min.Y > 0.6f);
    }

    [Fact]
    public void ParrotIsBirdSizedAndWingsStartAtTheShoulder()
    {
        var red = new Rgb(230, 40, 40);
        List<ColoredMesh> body = BirdModel.Body(red, new Rgb(255, 210, 60));
        Check(body);
        float minY = 9f, maxZ = -9f, minZ = 9f;
        foreach (ColoredMesh p in body) { minY = Math.Min(minY, p.Mesh.Min.Y); maxZ = Math.Max(maxZ, p.Mesh.Max.Z); minZ = Math.Min(minZ, p.Mesh.Min.Z); }
        Assert.InRange(minY, -0.25f, 0.01f);
        Assert.InRange(maxZ - minZ, 0.4f, 0.75f);
        foreach (int side in new[] { -1, 1 })
        {
            List<ColoredMesh> wing = BirdModel.Wing(red, new Rgb(40, 90, 230), side);
            Check(wing);
            float near = side > 0 ? wing[0].Mesh.Min.X : wing[0].Mesh.Max.X;
            float far = side > 0 ? wing[1].Mesh.Max.X : wing[1].Mesh.Min.X;
            Assert.Equal(0f, near, 2);
            Assert.InRange(Math.Abs(far), 0.3f, 0.4f);
            Assert.True(wing[0].Mesh.Max.Y - wing[0].Mesh.Min.Y < 0.05f, "a wing lies flat");
        }
    }

    [Fact]
    public void WritesPreviewPictures()
    {
        string dir = Path.Combine(AppContext.BaseDirectory, "..", "..", "preview", "forest");
        Directory.CreateDirectory(dir);
        V3 one = new V3(1f, 1f, 1f);
        var bg = new Rgb(150, 200, 235);
        for (int v = 0; v < RainforestTree.Variants; v++)
        {
            string path = Path.Combine(dir, "tree" + v + ".bmp");
            int count = 0;
            foreach (ColoredMesh p in RainforestTree.Build(v)) count += p.Mesh.Triangles.Count / 3;
            File.AppendAllText(Path.Combine(dir, "triangles.txt"), "tree " + v + ": " + count + " triangles\n");
            var tipped = new List<ColoredMesh>();
            foreach (ColoredMesh p in RainforestTree.Build(v)) tipped.Add(new ColoredMesh(p.Mesh.Transformed(one, 30f, 0f, V3.Zero).Transformed(one, 0f, 12f, V3.Zero), p.Color));
            Preview.SaveBmp(path, tipped, 300, 360, 0f, bg);
            Assert.True(new FileInfo(path).Length > 1000);
        }
        var bird = new List<ColoredMesh>(BirdModel.Body(new Rgb(230, 40, 40), new Rgb(255, 210, 60)));
        foreach (int side in new[] { -1, 1 })
            foreach (ColoredMesh w in BirdModel.Wing(new Rgb(230, 40, 40), new Rgb(40, 90, 230), side))
                bird.Add(new ColoredMesh(w.Mesh.Transformed(one, 0f, 0f, new V3(side * 0.07f, 0.17f, 0f)), w.Color));
        var turned = new List<ColoredMesh>();
        foreach (ColoredMesh p in bird) turned.Add(new ColoredMesh(p.Mesh.Transformed(one, -50f, 0f, V3.Zero).Transformed(one, 0f, 15f, V3.Zero), p.Color));
        Preview.SaveBmp(Path.Combine(dir, "parrot.bmp"), turned, 360, 360, 0f, bg);
        var gate = new List<ColoredMesh>();
        foreach (ColoredMesh p in RainforestTree.VineGate(0, 3.2f, 4.5f)) gate.Add(new ColoredMesh(p.Mesh.Transformed(one, 20f, 0f, V3.Zero), p.Color));
        Preview.SaveBmp(Path.Combine(dir, "vinegate.bmp"), gate, 300, 360, 0f, bg);
    }
}
