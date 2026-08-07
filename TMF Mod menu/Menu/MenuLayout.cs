using System;

namespace TMFModMenu.Menu;

internal readonly record struct MenuLayoutBounds(
    int X,
    int Y,
    int Width,
    int Height,
    int RowsY,
    int FooterY,
    int RowHeight);

internal static class MenuLayout
{
    private const int HeaderHeight = 78;
    private const int FooterHeight = 52;
    private const int RowHeight = 26;

    public static MenuLayoutBounds Calculate(
        int viewportX,
        int viewportY,
        int viewportWidth,
        int viewportHeight,
        int visibleRowCount)
    {
        viewportWidth = Math.Max(1, viewportWidth);
        viewportHeight = Math.Max(1, viewportHeight);
        visibleRowCount = Math.Max(0, visibleRowCount);

        int margin = Math.Clamp(viewportWidth / 32, 8, 32);
        int maxWidth = Math.Max(1, viewportWidth - (margin * 2));
        int maxHeight = Math.Max(1, viewportHeight - (margin * 2));
        int desiredWidth = Math.Clamp(viewportWidth * 2 / 5, 280, 430);
        int desiredHeight = HeaderHeight +
            (visibleRowCount * RowHeight) + FooterHeight;
        int width = Math.Min(desiredWidth, maxWidth);
        int height = Math.Min(desiredHeight, maxHeight);
        int desiredTop = Math.Clamp(viewportHeight / 12, margin, 64);
        int latestTop = Math.Max(margin, viewportHeight - margin - height);
        int top = Math.Min(desiredTop, latestTop);

        return new MenuLayoutBounds(
            viewportX + margin,
            viewportY + top,
            width,
            height,
            viewportY + top + HeaderHeight,
            viewportY + top + HeaderHeight + (visibleRowCount * RowHeight) + 6,
            RowHeight);
    }
}
