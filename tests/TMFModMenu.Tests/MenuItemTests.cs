using System;
using System.Collections.Generic;
using TMFModMenu.Menu;

namespace TMFModMenu.Tests;

public sealed class MenuItemTests
{
    [Fact]
    public void ActionInvokesExactlyOncePerActivation()
    {
        int calls = 0;
        var item = new MenuActionItem("Run", _ => calls++);

        Assert.True(item.Activate(default));

        Assert.Equal(1, calls);
        Assert.Equal(MenuItemKind.Action, item.Kind);
        Assert.Equal(string.Empty, item.Value);
    }

    [Fact]
    public void DisabledActionCannotInvoke()
    {
        int calls = 0;
        var item = new MenuActionItem("Run", _ => calls++, isEnabled: false);

        Assert.False(item.Activate(default));

        Assert.Equal(0, calls);
    }

    [Fact]
    public void ToggleOwnsItsOnOffValueAndHonorsEffectiveState()
    {
        var item = new MenuToggleItem(
            "Toggle",
            change: (_, requested) => requested);

        Assert.Equal("OFF", item.Value);
        Assert.True(item.Activate(default));
        Assert.True(item.IsOn);
        Assert.Equal("ON", item.Value);
    }

    [Fact]
    public void ChoiceWrapsBothDirections()
    {
        var item = new MenuChoiceItem("Choice", new[] { "1", "2", "3" });

        Assert.True(item.Adjust(default, -1));
        Assert.Equal("3", item.Value);
        Assert.True(item.IsNumericValue);
        Assert.True(item.Adjust(default, 1));
        Assert.Equal("1", item.Value);
    }

    [Fact]
    public void SubmenuOwnsItsNavigationValueAndPageFactory()
    {
        var child = new MenuPage("Child");
        var item = new MenuSubmenuItem("Open", _ => child);

        Assert.Equal(">", item.Value);
        Assert.Same(child, item.CreatePage(default));
        Assert.Equal(MenuItemKind.Submenu, item.Kind);
    }

    [Fact]
    public void PageCopiesSuppliedItemsAndExposesAReadOnlyView()
    {
        var supplied = new List<MenuItem> { new MenuActionItem("One") };
        var page = new MenuPage("Page", supplied);

        supplied.Clear();

        Assert.Single(page.Items);
        Assert.False(page.Items is List<MenuItem>);
    }

    [Fact]
    public void ReplaceItemsCanSafelyReuseTheCurrentReadOnlyView()
    {
        var page = new MenuPage(
            "Page",
            new MenuItem[]
            {
                new MenuActionItem("One"),
                new MenuActionItem("Two")
            });

        page.ReplaceItems(page.Items);

        Assert.Equal(2, page.Items.Count);
        Assert.Equal("One", page.Items[0].Label);
        Assert.Equal("Two", page.Items[1].Label);
    }

    [Fact]
    public void PageRejectsNullRowsInConstructorAndReplacement()
    {
        Assert.Throws<ArgumentException>(() =>
            new MenuPage("Page", new MenuItem[] { null }));

        var page = new MenuPage("Page");
        Assert.Throws<ArgumentException>(() =>
            page.ReplaceItems(new MenuItem[] { null }));
    }
}
