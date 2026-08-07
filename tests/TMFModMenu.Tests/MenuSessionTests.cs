using TMFModMenu.Menu;
using TMFModMenu.Features.Flight;
using TMFModMenu.Features.Inventory;
using TMFModMenu.Features.Survival;

namespace TMFModMenu.Tests;

public sealed class MenuSessionTests
{
    [Fact]
    public void TwoSessionsDoNotShareOpenSelectionStackToggleOrChoiceState()
    {
        var first = new MenuSession();
        var second = new MenuSession();

        first.ApplyState(MenuCommand.Toggle);
        first.ApplyState(MenuCommand.Up);
        first.ApplyState(MenuCommand.Right);
        Assert.Equal(2, first.Menu.Depth);

        first.ApplyState(MenuCommand.Down);
        first.ApplyState(MenuCommand.Right);
        first.ApplyState(MenuCommand.Down);
        first.ApplyState(MenuCommand.Right);

        Assert.True(first.Menu.IsOpen);
        Assert.False(second.Menu.IsOpen);
        Assert.Equal(2, first.Menu.Depth);
        Assert.Equal(1, second.Menu.Depth);
        Assert.Equal("ON", first.Snapshot.Rows[1].Value);

        second.ApplyState(MenuCommand.Toggle);
        second.ApplyState(MenuCommand.Up);
        second.ApplyState(MenuCommand.Right);

        Assert.Equal("OFF", second.Snapshot.Rows[1].Value);
        Assert.Equal("1", second.Snapshot.Rows[2].Value);
        Assert.Equal("2", first.Snapshot.Rows[2].Value);
    }

    [Fact]
    public void ChoiceUsesLeftRightWithoutLeavingChild()
    {
        var session = OpenDemoPage();
        session.ApplyState(MenuCommand.Down);
        session.ApplyState(MenuCommand.Down);

        Assert.Equal("1", session.Snapshot.Rows[2].Value);
        session.ApplyState(MenuCommand.Left);

        Assert.Equal(2, session.Menu.Depth);
        Assert.Equal("3", session.Snapshot.Rows[2].Value);
    }

    [Fact]
    public void ActionValueAndLabelStayInOneSnapshotRow()
    {
        var session = OpenDemoPage();

        session.ApplyState(MenuCommand.Right);

        Assert.Equal("Action Example", session.Snapshot.Rows[0].Label);
        Assert.Equal("1", session.Snapshot.Rows[0].Value);
    }

    [Fact]
    public void InventoryRootUsesInjectedMenuProvider()
    {
        var provider = new StubInventoryMenuProvider();
        var session = new MenuSession(provider);

        session.ApplyState(MenuCommand.Toggle);
        session.ApplyState(MenuCommand.Right);

        Assert.Equal(1, provider.CreateCalls);
        Assert.Equal(2, session.Menu.Depth);
        Assert.Contains("TEST INVENTORY", session.Snapshot.Breadcrumb);
    }

    [Fact]
    public void PlayerModelReportsPublicApiGateFailure()
    {
        var session = new MenuSession();

        Assert.Equal("Player Model", session.Snapshot.Rows[1].Label);
        Assert.Equal("API BLOCKED", session.Snapshot.Rows[1].Value);
        Assert.False(session.Snapshot.Rows[1].IsEnabled);
    }

    [Fact]
    public void FlightRootUsesInjectedMenuProvider()
    {
        var provider = new StubFlightMenuProvider();
        var session = new MenuSession(flightService: provider);

        Assert.Equal(1, provider.CreateCalls);
        Assert.Equal("Test Flight", session.Snapshot.Rows[2].Label);
        Assert.True(session.Snapshot.Rows[2].IsEnabled);
    }

    [Fact]
    public void SurvivalRootUsesInjectedMenuProvider()
    {
        var provider = new StubSurvivalMenuProvider();
        var session = new MenuSession(survivalService: provider);

        Assert.Equal(1, provider.CreateCalls);
        Assert.Equal("Test Survival", session.Snapshot.Rows[3].Label);
        Assert.True(session.Snapshot.Rows[3].IsEnabled);
    }

    [Fact]
    public void SurvivalRootIsDisabledWithoutAProvider()
    {
        var session = new MenuSession();

        Assert.Equal("Survival Assist", session.Snapshot.Rows[3].Label);
        Assert.Equal("OFF", session.Snapshot.Rows[3].Value);
        Assert.False(session.Snapshot.Rows[3].IsEnabled);
    }

    private static MenuSession OpenDemoPage()
    {
        var session = new MenuSession();
        session.ApplyState(MenuCommand.Toggle);
        session.ApplyState(MenuCommand.Up);
        session.ApplyState(MenuCommand.Right);
        return session;
    }

    private sealed class StubInventoryMenuProvider : IInventoryMenuProvider
    {
        public int CreateCalls { get; private set; }

        public MenuPage CreateInventoryPage()
        {
            CreateCalls++;
            return new MenuPage(
                "Test Inventory",
                new[] { new MenuActionItem("Ready", isEnabled: false) });
        }
    }

    private sealed class StubFlightMenuProvider : IFlightMenuProvider
    {
        public int CreateCalls { get; private set; }

        public MenuItem CreateFlightMenuItem()
        {
            CreateCalls++;
            return new MenuToggleItem("Test Flight");
        }
    }

    private sealed class StubSurvivalMenuProvider : ISurvivalMenuProvider
    {
        public int CreateCalls { get; private set; }

        public MenuItem CreateSurvivalMenuItem()
        {
            CreateCalls++;
            return new MenuToggleItem("Test Survival");
        }
    }
}
