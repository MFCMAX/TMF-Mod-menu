using System.Collections.Generic;
using System.Linq;
using TMFModMenu.Menu;

namespace TMFModMenu.Tests;

public sealed class MenuManagerTests
{
    [Fact]
    public void ToggleOpensAndClosesExactlyOncePerCommand()
    {
        var menu = CreateMenu(3);

        Assert.True(menu.Handle(MenuCommand.Toggle));
        Assert.True(menu.IsOpen);
        Assert.False(menu.Handle(MenuCommand.None));
        Assert.True(menu.IsOpen);
        Assert.True(menu.Handle(MenuCommand.Toggle));
        Assert.False(menu.IsOpen);
    }

    [Fact]
    public void ThreeRowsWrapInBothDirections()
    {
        var menu = CreateMenu(3);
        menu.Handle(MenuCommand.Toggle);

        Assert.Equal(0, menu.SelectedIndex);
        Assert.True(menu.Handle(MenuCommand.Up));
        Assert.Equal(2, menu.SelectedIndex);
        Assert.True(menu.Handle(MenuCommand.Down));
        Assert.Equal(0, menu.SelectedIndex);
    }

    [Fact]
    public void ZeroAndOneRowMenusNeverProduceInvalidSelection()
    {
        var empty = CreateMenu(0);
        empty.Handle(MenuCommand.Toggle);
        Assert.Equal(-1, empty.SelectedIndex);
        Assert.False(empty.Handle(MenuCommand.Up));

        var one = CreateMenu(1);
        one.Handle(MenuCommand.Toggle);
        Assert.Equal(0, one.SelectedIndex);
        Assert.False(one.Handle(MenuCommand.Down));
        Assert.Equal(0, one.SelectedIndex);
    }

    [Fact]
    public void LongChildPagesAndReturnsWithParentSelectionPreserved()
    {
        var child = CreatePage("Child", 12);
        var root = new MenuPage(
            "Root",
            new MenuItem[]
            {
                new MenuActionItem("First"),
                new MenuSubmenuItem("Child", _ => child),
                new MenuActionItem("Last")
            });
        var menu = new MenuManager(root, visibleCapacity: 7);
        menu.Handle(MenuCommand.Toggle);
        menu.Handle(MenuCommand.Down);
        menu.Handle(MenuCommand.Right);

        Assert.Equal(2, menu.Depth);
        Assert.Equal("ROOT > CHILD", menu.Snapshot.Breadcrumb);
        for (int i = 0; i < 8; i++)
            menu.Handle(MenuCommand.Down);

        Assert.Equal(8, menu.SelectedIndex);
        Assert.Equal(7, menu.WindowOffset);
        Assert.Equal("2 / 2", menu.Snapshot.PageIndicator);

        menu.Handle(MenuCommand.Back);

        Assert.Equal(1, menu.Depth);
        Assert.Equal(1, menu.SelectedIndex);
        Assert.Equal("ROOT", menu.Snapshot.Breadcrumb);
    }

    [Fact]
    public void RemovingRowsClampsSelectionAndNeverDesynchronizesSnapshot()
    {
        var page = CreatePage("Root", 10);
        var menu = new MenuManager(page, visibleCapacity: 7);
        menu.Handle(MenuCommand.Toggle);
        menu.Handle(MenuCommand.Up);
        Assert.Equal(9, menu.SelectedIndex);

        for (int i = 0; i < 9; i++)
            page.RemoveAt(page.Items.Count - 1);

        Assert.Equal(0, menu.SelectedIndex);
        Assert.Equal(0, menu.WindowOffset);
        Assert.Single(menu.Snapshot.Rows);
        Assert.Equal(page.Items[0].Label, menu.Snapshot.Rows[0].Label);

        page.RemoveAt(0);
        Assert.Equal(-1, menu.SelectedIndex);
        Assert.Empty(menu.Snapshot.Rows);
    }

    [Fact]
    public void SnapshotIsCachedUntilStateChanges()
    {
        var menu = CreateMenu(3);
        var first = menu.Snapshot;

        Assert.Same(first, menu.Snapshot);

        menu.Handle(MenuCommand.Toggle);
        var afterOpen = menu.Snapshot;
        Assert.NotSame(first, afterOpen);
        Assert.Same(afterOpen, menu.Snapshot);
    }

    [Fact]
    public void DirectItemStateChangesInvalidateTheCachedSnapshot()
    {
        var toggle = new MenuToggleItem("Toggle");
        var page = new MenuPage("Root", new[] { toggle });
        var menu = new MenuManager(page);
        var before = menu.Snapshot;

        toggle.SetState(true);

        Assert.NotSame(before, menu.Snapshot);
        Assert.Equal("ON", menu.Snapshot.Rows[0].Value);

        var action = new MenuActionItem("Action");
        var actionMenu = new MenuManager(new MenuPage("Root", new[] { action }));
        var actionBefore = actionMenu.Snapshot;

        action.SetValue("READY");

        Assert.NotSame(actionBefore, actionMenu.Snapshot);
        Assert.Equal("READY", actionMenu.Snapshot.Rows[0].Value);
    }

    [Fact]
    public void CachedSnapshotRowsCannotBeMutatedThroughThePublicView()
    {
        var menu = CreateMenu(2);

        Assert.False(menu.Snapshot.Rows is MenuRenderRow[]);
        var collection = Assert.IsAssignableFrom<ICollection<MenuRenderRow>>(
            menu.Snapshot.Rows);
        Assert.True(collection.IsReadOnly);
    }

    [Fact]
    public void BackPopsChildBeforeClosingRoot()
    {
        var child = CreatePage("Child", 1);
        var root = new MenuPage(
            "Root",
            new[] { new MenuSubmenuItem("Child", _ => child) });
        var menu = new MenuManager(root);
        menu.Handle(MenuCommand.Toggle);
        menu.Handle(MenuCommand.Right);

        Assert.True(menu.Handle(MenuCommand.Back));
        Assert.True(menu.IsOpen);
        Assert.Equal(1, menu.Depth);
        Assert.True(menu.Handle(MenuCommand.Back));
        Assert.False(menu.IsOpen);
    }

    [Fact]
    public void LeftOnTextChoiceGoesBackInsteadOfChangingTheText()
    {
        var choice = new MenuChoiceItem("Mode", new[] { "LOW", "HIGH" });
        var child = new MenuPage("Child", new[] { choice });
        var root = new MenuPage(
            "Root",
            new[] { new MenuSubmenuItem("Child", _ => child) });
        var menu = new MenuManager(root);
        menu.Handle(MenuCommand.Toggle);
        menu.Handle(MenuCommand.Right);

        menu.Handle(MenuCommand.Left);

        Assert.Equal(1, menu.Depth);
        Assert.Equal("LOW", choice.Value);
    }

    private static MenuManager CreateMenu(int rowCount) =>
        new(CreatePage("Root", rowCount));

    private static MenuPage CreatePage(string title, int rowCount)
    {
        var items = Enumerable.Range(0, rowCount)
            .Select(i => (MenuItem)new MenuActionItem($"Row {i}"));
        return new MenuPage(title, items);
    }
}
