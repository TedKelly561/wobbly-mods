using WobblyMods.Core;
using Xunit;

public class TapWatcherTests
{
    [Fact]
    public void QuickPressAndReleaseIsATap()
    {
        var t = new TapWatcher();
        Assert.False(t.Update(true, false, 0.0f));
        Assert.False(t.Update(true, false, 0.1f));
        Assert.True(t.Update(false, false, 0.2f));
    }

    [Fact]
    public void LongHoldIsNotATap()
    {
        var t = new TapWatcher();
        t.Update(true, false, 0.0f);
        Assert.False(t.Update(false, false, 1.0f));
    }

    [Fact]
    public void OtherButtonDuringPressCancelsTap()
    {
        // Holding LB+RB to open the menu must never place or undo.
        var t = new TapWatcher();
        t.Update(true, false, 0.0f);
        t.Update(true, true, 0.1f);
        Assert.False(t.Update(false, false, 0.2f));
    }

    [Fact]
    public void OtherButtonAlreadyDownCancelsTap()
    {
        var t = new TapWatcher();
        t.Update(true, true, 0.0f);
        Assert.False(t.Update(false, true, 0.1f));
    }

    [Fact]
    public void FiresOncePerTapAndResets()
    {
        var t = new TapWatcher();
        t.Update(true, false, 0.0f);
        Assert.True(t.Update(false, false, 0.1f));
        Assert.False(t.Update(false, false, 0.2f));
        t.Update(true, false, 0.3f);
        Assert.True(t.Update(false, false, 0.4f));
    }

    [Fact]
    public void ReleaseWithoutPressIsNotATap()
    {
        Assert.False(new TapWatcher().Update(false, false, 0.0f));
    }
}
