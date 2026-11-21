using System;
using System.Collections.Generic;
using System.IO;
using WobblyMods.Core.Shapes;
using Xunit;

public class PreviewTests
{
    private static readonly Rgb Black = new Rgb(0, 0, 0);
    private static readonly Rgb Red = new Rgb(255, 0, 0);
    private static readonly Rgb Blue = new Rgb(0, 0, 255);
    private static readonly Rgb Gray = new Rgb(128, 128, 128);

    private static ColoredMesh Sphere(float radius, V3 at, Rgb color)
    {
        ShapeMesh mesh = Shapes.Ellipsoid(new V3(radius, radius, radius), 24, 16)
            .Transformed(new V3(1f, 1f, 1f), 0f, 0f, at);
        return new ColoredMesh(mesh, color);
    }

    private static bool SameColor(Rgb a, Rgb b)
    {
        return a.R == b.R && a.G == b.G && a.B == b.B;
    }

    private static int CountNot(Rgb[] pixels, Rgb background)
    {
        int count = 0;
        foreach (Rgb p in pixels)
            if (!SameColor(p, background)) count++;
        return count;
    }

    private static bool LooksRed(Rgb p)
    {
        return p.R > 100 && p.G < 40 && p.B < 40;
    }

    // ---- Render -----------------------------------------------------------------------------

    [Fact]
    public void Render_LengthIsWidthTimesHeight()
    {
        var pixels = Preview.Render(new List<ColoredMesh> { Sphere(1f, V3.Zero, Red) }, 64, 40, 0f, Black);
        Assert.Equal(64 * 40, pixels.Length);
    }

    [Fact]
    public void Render_EmptyListOrNull_IsAllBackground()
    {
        var background = new Rgb(10, 20, 30);
        Assert.Equal(0, CountNot(Preview.Render(new List<ColoredMesh>(), 20, 10, 0f, background), background));
        Assert.Equal(0, CountNot(Preview.Render(null, 20, 10, 0f, background), background));
    }

    [Fact]
    public void Render_Sphere_CenterIsCoveredAndCornersAreBackground()
    {
        var pixels = Preview.Render(new List<ColoredMesh> { Sphere(1f, V3.Zero, Red) }, 100, 100, 0f, Black);
        Assert.False(SameColor(pixels[50 * 100 + 50], Black));
        Assert.True(SameColor(pixels[0], Black));
        Assert.True(SameColor(pixels[99], Black));
        Assert.True(SameColor(pixels[99 * 100], Black));
        Assert.True(SameColor(pixels[99 * 100 + 99], Black));
    }

    [Fact]
    public void Render_Sphere_CoveredPixelCountMatchesCircleArea()
    {
        var pixels = Preview.Render(new List<ColoredMesh> { Sphere(1f, V3.Zero, Red) }, 100, 100, 0f, Black);
        double expected = Math.PI * 47.5 * 47.5; // the model fills 95 % of the picture
        double covered = CountNot(pixels, Black);
        Assert.True(Math.Abs(covered - expected) / expected < 0.10, "covered " + covered + ", expected about " + expected);
    }

    [Fact]
    public void Render_RedSphereInFront_ShowsRedAtYaw0AndBlueAtYaw180()
    {
        var parts = new List<ColoredMesh>
        {
            Sphere(1f, V3.Zero, Blue),
            Sphere(0.5f, new V3(0f, 0f, 2f), Red), // in front, on the +Z side
        };

        Rgb front = Preview.Render(parts, 80, 80, 0f, Black)[40 * 80 + 40];
        Rgb behind = Preview.Render(parts, 80, 80, 180f, Black)[40 * 80 + 40];

        Assert.True(front.R > front.B, "yaw 0 centre should be red");
        Assert.True(behind.B > behind.R, "yaw 180 centre should be blue");
    }

    [Fact]
    public void Render_PartAtPlusX_AppearsInTheRightHalf()
    {
        var parts = new List<ColoredMesh> { Sphere(0.5f, V3.Zero, Gray), Sphere(0.5f, new V3(3f, 0f, 0f), Red) };
        var pixels = Preview.Render(parts, 100, 100, 0f, Black);

        int red = 0;
        for (int i = 0; i < pixels.Length; i++)
        {
            if (!LooksRed(pixels[i])) continue;
            red++;
            Assert.True(i % 100 >= 50, "a red pixel is in the left half");
        }
        Assert.True(red > 50);
    }

    [Fact]
    public void Render_PartAtPlusY_AppearsInTheTopHalf()
    {
        var parts = new List<ColoredMesh> { Sphere(0.5f, V3.Zero, Gray), Sphere(0.5f, new V3(0f, 3f, 0f), Red) };
        var pixels = Preview.Render(parts, 100, 100, 0f, Black);

        int red = 0;
        for (int i = 0; i < pixels.Length; i++)
        {
            if (!LooksRed(pixels[i])) continue;
            red++;
            Assert.True(i / 100 < 50, "a red pixel is in the bottom half (row " + (i / 100) + ")");
        }
        Assert.True(red > 50);
    }

    [Fact]
    public void Render_Yaw90_TurnsPlusZIntoTheRight()
    {
        var parts = new List<ColoredMesh> { Sphere(0.5f, V3.Zero, Gray), Sphere(0.5f, new V3(0f, 0f, 3f), Red) };
        var pixels = Preview.Render(parts, 100, 100, 90f, Black);

        int red = 0;
        for (int i = 0; i < pixels.Length; i++)
        {
            if (!LooksRed(pixels[i])) continue;
            red++;
            Assert.True(i % 100 >= 50, "yaw 90 should put +Z on the right");
        }
        Assert.True(red > 50);
    }

    [Fact]
    public void Render_WideShape_LeavesTopRowBackground()
    {
        var mesh = Shapes.Ellipsoid(new V3(2f, 1f, 1f), 24, 16);
        var pixels = Preview.Render(new List<ColoredMesh> { new ColoredMesh(mesh, Red) }, 100, 100, 0f, Black);
        for (int x = 0; x < 100; x++)
            Assert.True(SameColor(pixels[x], Black), "top row pixel " + x + " is covered");
    }

    [Fact]
    public void Render_DrawsBothSidesOfATriangle()
    {
        var front = new ShapeMesh();
        front.AddVertex(new V3(-1f, -1f, 0f), V3.Forward);
        front.AddVertex(new V3(1f, -1f, 0f), V3.Forward);
        front.AddVertex(new V3(0f, 1f, 0f), V3.Forward);
        front.AddTriangle(0, 1, 2);

        var back = new ShapeMesh();
        back.AddVertex(new V3(-1f, -1f, 0f), V3.Forward);
        back.AddVertex(new V3(1f, -1f, 0f), V3.Forward);
        back.AddVertex(new V3(0f, 1f, 0f), V3.Forward);
        back.AddTriangle(0, 2, 1); // the other winding

        int a = CountNot(Preview.Render(new List<ColoredMesh> { new ColoredMesh(front, Red) }, 50, 50, 0f, Black), Black);
        int b = CountNot(Preview.Render(new List<ColoredMesh> { new ColoredMesh(back, Red) }, 50, 50, 0f, Black), Black);
        Assert.True(a > 100);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Render_FlatShadingFollowsTheLight()
    {
        // A triangle in the XY plane has normal +/-Z: brightness = 0.35 + 0.65 * (0.8 / |(0.3, 0.5, 0.8)|).
        var mesh = new ShapeMesh();
        mesh.AddVertex(new V3(-1f, -1f, 0f), V3.Forward);
        mesh.AddVertex(new V3(1f, -1f, 0f), V3.Forward);
        mesh.AddVertex(new V3(0f, 1f, 0f), V3.Forward);
        mesh.AddTriangle(0, 1, 2);

        Rgb centre = Preview.Render(new List<ColoredMesh> { new ColoredMesh(mesh, new Rgb(200, 100, 50)) }, 100, 100, 0f, Black)[50 * 100 + 50];

        float brightness = 0.35f + 0.65f * (0.8f / (float)Math.Sqrt(0.3 * 0.3 + 0.5 * 0.5 + 0.8 * 0.8));
        Assert.True(Math.Abs(centre.R - 200 * brightness) <= 1.5f, "R " + centre.R);
        Assert.True(Math.Abs(centre.G - 100 * brightness) <= 1.5f, "G " + centre.G);
        Assert.True(Math.Abs(centre.B - 50 * brightness) <= 1.5f, "B " + centre.B);
    }

    [Fact]
    public void Render_PointWithNoSize_IsAllBackground()
    {
        var mesh = new ShapeMesh();
        for (int i = 0; i < 3; i++) mesh.AddVertex(new V3(1f, 2f, 3f), V3.Up);
        mesh.AddTriangle(0, 1, 2);
        var pixels = Preview.Render(new List<ColoredMesh> { new ColoredMesh(mesh, Red) }, 30, 30, 0f, Black);
        Assert.Equal(0, CountNot(pixels, Black));
    }

    // ---- BMP --------------------------------------------------------------------------------

    private static int ReadInt(byte[] bytes, int offset)
    {
        return bytes[offset] | (bytes[offset + 1] << 8) | (bytes[offset + 2] << 16) | (bytes[offset + 3] << 24);
    }

    private static Rgb[] Pixels(int width, int height)
    {
        var pixels = new Rgb[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Rgb((byte)(i * 3), (byte)(i * 5), (byte)(i * 7));
        return pixels;
    }

    [Fact]
    public void ToBmp_HasTheRightHeaderAndLength()
    {
        int width = 7, height = 5;
        byte[] bmp = Preview.ToBmp(Pixels(width, height), width, height);

        int rowSize = ((width * 3 + 3) / 4) * 4;
        Assert.Equal((byte)'B', bmp[0]);
        Assert.Equal((byte)'M', bmp[1]);
        Assert.Equal(54 + rowSize * height, bmp.Length);
        Assert.Equal(bmp.Length, ReadInt(bmp, 2));   // file size
        Assert.Equal(54, ReadInt(bmp, 10));          // data offset
        Assert.Equal(40, ReadInt(bmp, 14));          // header size
        Assert.Equal(width, ReadInt(bmp, 18));
        Assert.Equal(height, ReadInt(bmp, 22));
        Assert.Equal(24, bmp[28]);                   // bits per pixel
    }

    [Fact]
    public void ToBmp_BottomLeftPixelIsTheFirstDataBytesInBgrOrder()
    {
        int width = 4, height = 3;
        var pixels = Pixels(width, height);
        pixels[(height - 1) * width] = new Rgb(11, 22, 33); // bottom-left of the picture

        byte[] bmp = Preview.ToBmp(pixels, width, height);

        Assert.Equal(33, bmp[54]); // B
        Assert.Equal(22, bmp[55]); // G
        Assert.Equal(11, bmp[56]); // R
    }

    [Fact]
    public void ToBmp_TopRowIsStoredLastAndRowsArePadded()
    {
        int width = 5, height = 2; // 15 bytes per row, padded to 16
        var pixels = Pixels(width, height);
        pixels[0] = new Rgb(1, 2, 3); // top-left

        byte[] bmp = Preview.ToBmp(pixels, width, height);

        Assert.Equal(54 + 16 * 2, bmp.Length);
        Assert.Equal(3, bmp[54 + 16]); // top row is the second stored row: B
        Assert.Equal(2, bmp[54 + 16 + 1]);
        Assert.Equal(1, bmp[54 + 16 + 2]);
        Assert.Equal(0, bmp[54 + 15]); // padding byte of the first row
    }

    [Fact]
    public void SaveBmp_WritesAFileOfTheRightLength()
    {
        string path = Path.GetTempFileName();
        try
        {
            Preview.SaveBmp(path, new List<ColoredMesh> { Sphere(1f, V3.Zero, Red) }, 33, 21, 0f, Black);
            int rowSize = ((33 * 3 + 3) / 4) * 4;
            Assert.Equal(54 + rowSize * 21, new FileInfo(path).Length);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void BadSizesThrow()
    {
        var parts = new List<ColoredMesh> { Sphere(1f, V3.Zero, Red) };
        Assert.Throws<ArgumentOutOfRangeException>(() => Preview.Render(parts, 0, 10, 0f, Black));
        Assert.Throws<ArgumentOutOfRangeException>(() => Preview.Render(parts, 10, 0, 0f, Black));
        Assert.Throws<ArgumentOutOfRangeException>(() => Preview.ToBmp(new Rgb[0], 0, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => Preview.SaveBmp("x.bmp", parts, -1, 5, 0f, Black));
        Assert.Throws<ArgumentException>(() => Preview.ToBmp(new Rgb[3], 2, 2));
    }
}
