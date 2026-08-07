using System;
using System.Collections.Generic;

namespace TMFModMenu.Menu;

internal sealed class MenuManager
{
    private sealed class MenuFrame
    {
        public MenuFrame(MenuPage page)
        {
            Page = page;
            SelectedIndex = page.Items.Count == 0 ? -1 : 0;
            SeenPageVersion = page.ContentVersion;
        }

        public MenuPage Page { get; }

        public int SelectedIndex { get; set; }

        public int WindowOffset { get; set; }

        public long SeenPageVersion { get; set; }
    }

    private readonly List<MenuFrame> frames = new();
    private MenuSnapshot snapshot;

    public MenuManager(MenuPage rootPage, int visibleCapacity = 7)
    {
        if (rootPage == null)
            throw new ArgumentNullException(nameof(rootPage));

        VisibleCapacity = Math.Max(1, visibleCapacity);
        frames.Add(new MenuFrame(rootPage));
        RebuildSnapshot();
    }

    public bool IsOpen { get; private set; }

    public int VisibleCapacity { get; }

    public int SelectedIndex
    {
        get
        {
            if (NormalizeCurrentFrame())
                RebuildSnapshot();
            return CurrentFrame.SelectedIndex;
        }
    }

    public int ItemCount => CurrentFrame.Page.Items.Count;

    public int WindowOffset
    {
        get
        {
            if (NormalizeCurrentFrame())
                RebuildSnapshot();
            return CurrentFrame.WindowOffset;
        }
    }

    public int Depth => frames.Count;

    public MenuPage CurrentPage => CurrentFrame.Page;

    public MenuSnapshot Snapshot
    {
        get
        {
            if (NormalizeCurrentFrame())
                RebuildSnapshot();
            return snapshot;
        }
    }

    private MenuFrame CurrentFrame => frames[^1];

    public bool Handle(
        MenuCommand command,
        MenuInvocationContext context = default)
    {
        if (command == MenuCommand.Toggle)
        {
            if (IsOpen)
                Close();
            else
            {
                IsOpen = true;
                NormalizeCurrentFrame();
                RebuildSnapshot();
            }
            return true;
        }

        if (!IsOpen)
            return false;

        NormalizeCurrentFrame();

        bool changed = command switch
        {
            MenuCommand.Up => Move(-1),
            MenuCommand.Down => Move(1),
            MenuCommand.Back => GoBack(),
            MenuCommand.Left => HandleLeft(context),
            MenuCommand.Right => ActivateCurrent(context, adjustChoice: true),
            MenuCommand.Select => ActivateCurrent(context, adjustChoice: true),
            _ => false
        };

        if (changed)
        {
            NormalizeCurrentFrame();
            RebuildSnapshot();
        }

        return changed;
    }

    public void Close()
    {
        IsOpen = false;
        while (frames.Count > 1)
            frames.RemoveAt(frames.Count - 1);
        NormalizeCurrentFrame();
        RebuildSnapshot();
    }

    public void InvalidateSnapshot()
    {
        NormalizeCurrentFrame();
        RebuildSnapshot();
    }

    private bool Move(int direction)
    {
        var frame = CurrentFrame;
        int count = frame.Page.Items.Count;
        if (count <= 1)
            return false;

        frame.SelectedIndex =
            (frame.SelectedIndex + Math.Sign(direction) + count) % count;
        AlignWindow(frame);
        return true;
    }

    private bool GoBack()
    {
        if (frames.Count > 1)
        {
            frames.RemoveAt(frames.Count - 1);
            NormalizeCurrentFrame();
            return true;
        }

        Close();
        return true;
    }

    private bool HandleLeft(MenuInvocationContext context)
    {
        var item = GetCurrentItem();
        if (item is MenuChoiceItem choice && choice.IsNumericValue)
            return choice.Adjust(context, -1);

        return GoBack();
    }

    private bool ActivateCurrent(
        MenuInvocationContext context,
        bool adjustChoice)
    {
        var item = GetCurrentItem();
        if (item == null || !item.IsEnabled)
            return false;

        if (item is MenuSubmenuItem submenu)
        {
            var page = submenu.CreatePage(context);
            if (page == null)
                return false;

            frames.Add(new MenuFrame(page));
            return true;
        }

        if (item is MenuChoiceItem choice && adjustChoice)
            return choice.Adjust(context, 1);

        return item.Activate(context);
    }

    private MenuItem GetCurrentItem()
    {
        var frame = CurrentFrame;
        return frame.SelectedIndex >= 0 &&
            frame.SelectedIndex < frame.Page.Items.Count
            ? frame.Page.Items[frame.SelectedIndex]
            : null;
    }

    private bool NormalizeCurrentFrame()
    {
        var frame = CurrentFrame;
        if (frame.SeenPageVersion == frame.Page.ContentVersion)
            return false;

        int count = frame.Page.Items.Count;
        frame.SelectedIndex = count == 0
            ? -1
            : Math.Clamp(frame.SelectedIndex, 0, count - 1);
        frame.SeenPageVersion = frame.Page.ContentVersion;
        AlignWindow(frame);
        return true;
    }

    private void AlignWindow(MenuFrame frame)
    {
        frame.WindowOffset = frame.SelectedIndex < 0
            ? 0
            : (frame.SelectedIndex / VisibleCapacity) * VisibleCapacity;
    }

    private void RebuildSnapshot()
    {
        var frame = CurrentFrame;
        int count = frame.Page.Items.Count;
        int start = Math.Min(frame.WindowOffset, Math.Max(0, count - 1));
        int visibleCount = Math.Min(VisibleCapacity, Math.Max(0, count - start));
        var rows = new MenuRenderRow[visibleCount];

        for (int i = 0; i < visibleCount; i++)
        {
            var item = frame.Page.Items[start + i];
            rows[i] = new MenuRenderRow(
                item.Label,
                item.Value,
                start + i == frame.SelectedIndex,
                item.IsEnabled,
                item.Kind);
        }

        int pageCount = Math.Max(1, (count + VisibleCapacity - 1) / VisibleCapacity);
        int pageIndex = frame.SelectedIndex < 0
            ? 0
            : frame.SelectedIndex / VisibleCapacity;
        snapshot = new MenuSnapshot(
            BuildBreadcrumb(),
            $"{pageIndex + 1} / {pageCount}",
            rows);
    }

    private string BuildBreadcrumb()
    {
        var titles = new string[frames.Count];
        for (int i = 0; i < frames.Count; i++)
            titles[i] = frames[i].Page.Title.ToUpperInvariant();
        return string.Join(" > ", titles);
    }
}
