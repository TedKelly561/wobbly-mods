using System.Collections.Generic;
using WobblyMods.Core;
using Xunit;

public class HouseLayoutTests
{
    private static HouseLayout Sample()
    {
        var h = new HouseLayout();
        h.Items.Add(new HouseItem { Id = "bed", X = 1.5f, Y = 0f, Z = -3.25f, Yaw = 90f, Scale = 1f });
        h.Items.Add(new HouseItem { Id = "fishtank", X = 10f, Y = 0.5f, Z = 2f, Yaw = 180f, Scale = 0.3f,
                                    HasColor = true, R = 1f, G = 0.5f, B = 0f });
        return h;
    }

    [Fact]
    public void RoundTripIsExact()
    {
        string text = Sample().Save();
        string error;
        var warnings = new List<string>();
        HouseLayout back = HouseLayout.Load(text, warnings, out error);
        Assert.Null(error);
        Assert.Empty(warnings);
        Assert.Equal(text, back.Save());
        Assert.Equal(2, back.Items.Count);
        Assert.Equal("fishtank", back.Items[1].Id);
        Assert.True(back.Items[1].HasColor);
        Assert.Equal(0.5f, back.Items[1].G);
        Assert.False(back.Items[0].HasColor);
    }

    [Fact]
    public void MissingHeaderFails()
    {
        string error;
        Assert.Null(HouseLayout.Load("item bed 0 0 0 0 1\n", null, out error));
        Assert.NotNull(error);
    }

    [Fact]
    public void NewerVersionFails()
    {
        string error;
        Assert.Null(HouseLayout.Load("wobbly-house 99\n", null, out error));
        Assert.Contains("newer", error);
    }

    [Fact]
    public void EmptyFileFails()
    {
        string error;
        Assert.Null(HouseLayout.Load("  \n# just a comment\n", null, out error));
        Assert.NotNull(error);
    }

    [Fact]
    public void BadLinesAreSkippedWithWarnings()
    {
        string text = "wobbly-house 1\n" +
                      "item bed 0 0 0 0 1\n" +
                      "item lamp 1 2\n" +              // too few fields
                      "item lamp 1 2 3 x 1\n" +        // bad number
                      "item lamp 1 2 3 0 0\n" +        // scale 0
                      "item lamp NaN 2 3 0 1\n" +      // not finite
                      "chair 1 2 3 0 1\n" +            // unknown keyword
                      "# comment\n" +
                      "item toybox 4 0 4 45 2\n";
        var warnings = new List<string>();
        string error;
        HouseLayout h = HouseLayout.Load(text, warnings, out error);
        Assert.Null(error);
        Assert.Equal(2, h.Items.Count);
        Assert.Equal(5, warnings.Count);
    }

    [Fact]
    public void ColorsAreClamped()
    {
        string error;
        HouseLayout h = HouseLayout.Load("wobbly-house 1\nitem lamp 0 0 0 0 1 2 -1 0.5\n", null, out error);
        Assert.Equal(1f, h.Items[0].R);
        Assert.Equal(0f, h.Items[0].G);
        Assert.Equal(0.5f, h.Items[0].B);
    }

    [Fact]
    public void IdsWithSpacesAreNotSaved()
    {
        var h = new HouseLayout();
        h.Items.Add(new HouseItem { Id = "two words", Scale = 1f });
        h.Items.Add(new HouseItem { Id = "", Scale = 1f });
        Assert.Equal("wobbly-house 1\n", h.Save());
    }
}
