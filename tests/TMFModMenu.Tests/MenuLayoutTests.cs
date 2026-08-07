using TMFModMenu.Menu;

namespace TMFModMenu.Tests;

public sealed class MenuLayoutTests
{
    [Theory]
    [InlineData(0, 0, 1280, 720)]
    [InlineData(0, 0, 1920, 1080)]
    [InlineData(100, 50, 960, 540)]
    [InlineData(0, 0, 640, 360)]
    [InlineData(640, 360, 640, 360)]
    public void FiveRowPanelStaysInsideViewport(
        int x,
        int y,
        int width,
        int height)
    {
        var layout = MenuLayout.Calculate(x, y, width, height, visibleRowCount: 5);

        Assert.True(layout.X >= x);
        Assert.True(layout.Y >= y);
        Assert.True(layout.Width > 0);
        Assert.True(layout.Height > 0);
        Assert.True(layout.X + layout.Width <= x + width);
        Assert.True(layout.Y + layout.Height <= y + height);
        Assert.True(layout.RowsY >= layout.Y);
        Assert.True(layout.FooterY < layout.Y + layout.Height);
    }

    [Fact]
    public void InvalidDimensionsStillProduceSafePositiveBounds()
    {
        var layout = MenuLayout.Calculate(10, 20, 0, -1, visibleRowCount: -5);

        Assert.True(layout.Width > 0);
        Assert.True(layout.Height > 0);
        Assert.True(layout.RowHeight > 0);
    }
}
