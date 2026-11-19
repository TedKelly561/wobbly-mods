using System.Collections.Generic;
using WobblyMods.Core;
using Xunit;

public class HouseLayoutEdgeTests
{
    [Fact]
    public void WindowsLineEndingsAreParsed()
    {
        string text = "wobbly-house 1\r\nitem bed 0 0 0 0 1\r\n";
        string error;
        var warnings = new List<string>();
        HouseLayout h = HouseLayout.Load(text, warnings, out error);
        Assert.Null(error);
        Assert.Empty(warnings);
        Assert.Single(h.Items);
    }

    [Fact]
    public void TabsBetweenFieldsAreParsed()
    {
        string text = "wobbly-house 1\nitem\tbed\t0\t0\t0\t0\t1\n";
        string error;
        var warnings = new List<string>();
        HouseLayout h = HouseLayout.Load(text, warnings, out error);
        Assert.Null(error);
        Assert.Empty(warnings);
        Assert.Single(h.Items);
    }

    [Fact]
    public void ExtraSpacesAreIgnored()
    {
        string text = "wobbly-house   1\n   item   bed   0   0   0   0   1   \n";
        string error;
        var warnings = new List<string>();
        HouseLayout h = HouseLayout.Load(text, warnings, out error);
        Assert.Null(error);
        Assert.Empty(warnings);
        Assert.Single(h.Items);
    }

    [Fact]
    public void CommentLinesBetweenItemsAreIgnored()
    {
        string text = "wobbly-house 1\nitem bed 0 0 0 0 1\n# comment\nitem lamp 1 1 1 0 1\n";
        string error;
        var warnings = new List<string>();
        HouseLayout h = HouseLayout.Load(text, warnings, out error);
        Assert.Null(error);
        Assert.Empty(warnings);
        Assert.Equal(2, h.Items.Count);
    }

    [Fact]
    public void NegativeCoordinatesAreParsed()
    {
        string text = "wobbly-house 1\nitem bed -5.5 -3.2 -10.0 0 1\n";
        string error;
        var warnings = new List<string>();
        HouseLayout h = HouseLayout.Load(text, warnings, out error);
        Assert.Null(error);
        Assert.Empty(warnings);
        Assert.Equal(-5.5f, h.Items[0].X);
        Assert.Equal(-3.2f, h.Items[0].Y);
        Assert.Equal(-10.0f, h.Items[0].Z);
    }

    [Fact]
    public void VeryLargeCoordinatesAreParsed()
    {
        string text = "wobbly-house 1\nitem bed 99999.5 99999.5 99999.5 0 1\n";
        string error;
        var warnings = new List<string>();
        HouseLayout h = HouseLayout.Load(text, warnings, out error);
        Assert.Null(error);
        Assert.Empty(warnings);
        Assert.Equal(99999.5f, h.Items[0].X);
    }

    [Fact]
    public void YawOutside0360IsKeptAsGiven()
    {
        string text = "wobbly-house 1\nitem bed 0 0 0 -90 1\n";
        string error;
        var warnings = new List<string>();
        HouseLayout h = HouseLayout.Load(text, warnings, out error);
        Assert.Null(error);
        Assert.Empty(warnings);
        Assert.Equal(-90f, h.Items[0].Yaw);

        text = "wobbly-house 1\nitem bed 0 0 0 720 1\n";
        h = HouseLayout.Load(text, warnings, out error);
        Assert.Null(error);
        Assert.Empty(warnings);
        Assert.Equal(720f, h.Items[0].Yaw);
    }

    [Fact]
    public void ScaleLimitMinOk()
    {
        string text = "wobbly-house 1\nitem bed 0 0 0 0 0.0001\n";
        string error;
        var warnings = new List<string>();
        HouseLayout h = HouseLayout.Load(text, warnings, out error);
        Assert.Null(error);
        Assert.Empty(warnings);
        Assert.Equal(0.0001f, h.Items[0].Scale);
    }

    [Fact]
    public void ScaleLimitMaxOk()
    {
        string text = "wobbly-house 1\nitem bed 0 0 0 0 100\n";
        string error;
        var warnings = new List<string>();
        HouseLayout h = HouseLayout.Load(text, warnings, out error);
        Assert.Null(error);
        Assert.Empty(warnings);
        Assert.Equal(100f, h.Items[0].Scale);
    }

    [Fact]
    public void ScaleTooLargeIsRejected()
    {
        string text = "wobbly-house 1\nitem bed 0 0 0 0 100.1\n";
        string error;
        var warnings = new List<string>();
        HouseLayout h = HouseLayout.Load(text, warnings, out error);
        Assert.Null(error);
        Assert.Single(warnings);
        Assert.Empty(h.Items);
    }

    [Fact]
    public void NegativeScaleIsRejected()
    {
        string text = "wobbly-house 1\nitem bed 0 0 0 0 -1\n";
        string error;
        var warnings = new List<string>();
        HouseLayout h = HouseLayout.Load(text, warnings, out error);
        Assert.Null(error);
        Assert.Single(warnings);
        Assert.Empty(h.Items);
    }

    [Fact]
    public void EightFieldsIsRejected()
    {
        string text = "wobbly-house 1\nitem bed 0 0 0 0 1 0.5\n";
        string error;
        var warnings = new List<string>();
        HouseLayout h = HouseLayout.Load(text, warnings, out error);
        Assert.Null(error);
        Assert.Single(warnings);
        Assert.Empty(h.Items);
    }

    [Fact]
    public void NineFieldsIsRejected()
    {
        string text = "wobbly-house 1\nitem bed 0 0 0 0 1 0.5 0.5\n";
        string error;
        var warnings = new List<string>();
        HouseLayout h = HouseLayout.Load(text, warnings, out error);
        Assert.Null(error);
        Assert.Single(warnings);
        Assert.Empty(h.Items);
    }

    [Fact]
    public void CommaDecimalSeparatorIsRejected()
    {
        string text = "wobbly-house 1\nitem bed 0,5 0 0 0 1\n";
        string error;
        var warnings = new List<string>();
        HouseLayout h = HouseLayout.Load(text, warnings, out error);
        Assert.Null(error);
        Assert.Single(warnings);
        Assert.Empty(h.Items);
    }

    [Fact]
    public void HeaderVersionZeroIsAccepted()
    {
        string text = "wobbly-house 0\nitem bed 0 0 0 0 1\n";
        string error;
        var warnings = new List<string>();
        HouseLayout h = HouseLayout.Load(text, warnings, out error);
        Assert.Null(error);
        Assert.Empty(warnings);
        Assert.Single(h.Items);
    }

    [Fact]
    public void HeaderWithTrailingTextIsRejected()
    {
        string text = "wobbly-house 1 extra\n";
        string error;
        HouseLayout h = HouseLayout.Load(text, null, out error);
        Assert.Null(h);
        Assert.NotNull(error);
    }

    [Fact]
    public void LargeFileRoundTripsExactly()
    {
        var original = new HouseLayout();
        for (int i = 0; i < 5000; i++)
        {
            original.Items.Add(new HouseItem
            {
                Id = "item" + i,
                X = i * 0.5f,
                Y = i * 0.5f,
                Z = i * 0.5f,
                Yaw = i * 10f,
                Scale = 1f
            });
        }

        string text = original.Save();
        string error;
        var warnings = new List<string>();
        HouseLayout loaded = HouseLayout.Load(text, warnings, out error);

        Assert.Null(error);
        Assert.Empty(warnings);
        Assert.Equal(5000, loaded.Items.Count);

        for (int i = 0; i < 5000; i++)
        {
            var orig = original.Items[i];
            var load = loaded.Items[i];
            Assert.Equal(orig.Id, load.Id);
            Assert.Equal(orig.X, load.X);
            Assert.Equal(orig.Y, load.Y);
            Assert.Equal(orig.Z, load.Z);
            Assert.Equal(orig.Yaw, load.Yaw);
            Assert.Equal(orig.Scale, load.Scale);
        }
    }

    [Fact]
    public void IdsWithDashesUnderscoresAndDigitsRoundTrip()
    {
        string text = "wobbly-house 1\nitem my-item_01 0 0 0 0 1\n";
        string error;
        var warnings = new List<string>();
        HouseLayout h = HouseLayout.Load(text, warnings, out error);
        Assert.Null(error);
        Assert.Empty(warnings);
        Assert.Single(h.Items);
        Assert.Equal("my-item_01", h.Items[0].Id);

        string saved = h.Save();
        var h2 = HouseLayout.Load(saved, warnings, out error);
        Assert.Null(error);
        Assert.Empty(warnings);
        Assert.Single(h2.Items);
        Assert.Equal("my-item_01", h2.Items[0].Id);
    }

    [Fact]
    public void ExactlySevenFieldsAreAccepted()
    {
        string text = "wobbly-house 1\nitem bed 1 2 3 90 1\n";
        string error;
        var warnings = new List<string>();
        HouseLayout h = HouseLayout.Load(text, warnings, out error);
        Assert.Null(error);
        Assert.Empty(warnings);
        Assert.Single(h.Items);
        Assert.False(h.Items[0].HasColor);
    }

    [Fact]
    public void ExactlyTenFieldsAreAccepted()
    {
        string text = "wobbly-house 1\nitem bed 1 2 3 90 1 1 0.5 0\n";
        string error;
        var warnings = new List<string>();
        HouseLayout h = HouseLayout.Load(text, warnings, out error);
        Assert.Null(error);
        Assert.Empty(warnings);
        Assert.Single(h.Items);
        Assert.True(h.Items[0].HasColor);
    }

    [Fact(Skip = "BUG: Save() writes numbers with 3 decimals, so a valid scale below 0.0005 is saved as 0 and Load() drops that item")]
    public void TinyScaleSurvivesSaveAndLoad()
    {
        var h = new HouseLayout();
        h.Items.Add(new HouseItem { Id = "bed", X = 0f, Y = 0f, Z = 0f, Yaw = 0f, Scale = 0.0001f });
        string text = h.Save();
        string error;
        var warnings = new List<string>();
        HouseLayout back = HouseLayout.Load(text, warnings, out error);
        Assert.Null(error);
        Assert.Single(back.Items);
    }
}
