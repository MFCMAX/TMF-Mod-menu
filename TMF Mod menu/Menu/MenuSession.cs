using System.Collections.Generic;
using StudioForge.TotalMiner.API;
using TMFModMenu.Features.Flight;
using TMFModMenu.Features.Inventory;
using TMFModMenu.Features.Survival;

namespace TMFModMenu.Menu;

internal sealed class MenuSession
{
    private ITMPlayer leasedPlayer;
    private bool originalOverrideControlInput;
    private bool ownsControlLease;
    private int demoActionCount;
    private readonly IInventoryMenuProvider inventoryService;
    private readonly IFlightMenuProvider flightService;
    private readonly ISurvivalMenuProvider survivalService;

    public MenuSession(
        IInventoryMenuProvider inventoryService = null,
        IFlightMenuProvider flightService = null,
        ISurvivalMenuProvider survivalService = null)
    {
        this.inventoryService = inventoryService ?? new UnavailableInventoryMenuProvider();
        this.flightService = flightService ?? new UnavailableFlightMenuProvider();
        this.survivalService = survivalService ?? new UnavailableSurvivalMenuProvider();
        Menu = new MenuManager(BuildRootPage(), visibleCapacity: 7);
    }

    public MenuManager Menu { get; }

    public MenuInput Input { get; } = new();

    public MenuSnapshot Snapshot => Menu.Snapshot;

    public void Handle(ITMPlayer player, MenuCommand command)
    {
        var wasOpen = Menu.IsOpen;
        Menu.Handle(command, new MenuInvocationContext(player));

        if (!wasOpen && Menu.IsOpen)
            AcquireControlLease(player);
        else if (wasOpen && !Menu.IsOpen)
            ReleaseControlLease();
    }

    internal void ApplyState(MenuCommand command, object player = null)
    {
        Menu.Handle(command, new MenuInvocationContext(player));
    }

    public void Close()
    {
        Menu.Close();
        Input.Reset();
        ReleaseControlLease();
    }

    private MenuPage BuildRootPage()
    {
        MenuActionItem demoAction = null;
        demoAction = new MenuActionItem(
            "Action Example",
            _ =>
            {
                demoActionCount++;
                demoAction.SetValue(demoActionCount.ToString());
            },
            value: "0");
        var demoItems = new List<MenuItem>
        {
            demoAction,
            new MenuToggleItem("Toggle Example"),
            new MenuChoiceItem(
                "Choice Example",
                new[] { "1", "2", "3" }),
            new MenuSubmenuItem(
                "Nested Page",
                _ => new MenuPage(
                    "Nested",
                    new[] { new MenuActionItem("Nested Action") }))
        };

        for (int i = 1; i <= 12; i++)
            demoItems.Add(new MenuActionItem($"Long Row {i:00}"));

        var demoPage = new MenuPage("Menu Demo", demoItems);

        return new MenuPage(
            "Root",
            new MenuItem[]
            {
                new MenuSubmenuItem(
                    "Inventory",
                    _ => inventoryService.CreateInventoryPage()),
                new MenuActionItem(
                    "Player Model",
                    value: "API BLOCKED",
                    isEnabled: false,
                    action: null),
                flightService.CreateFlightMenuItem(),
                survivalService.CreateSurvivalMenuItem(),
                new MenuSubmenuItem("Menu Demo", _ => demoPage)
            });
    }

    private void AcquireControlLease(ITMPlayer player)
    {
        if (ownsControlLease || player == null)
            return;

        leasedPlayer = player;
        originalOverrideControlInput = player.OverrideControlInput;
        player.OverrideControlInput = true;
        ownsControlLease = true;
    }

    private void ReleaseControlLease()
    {
        if (!ownsControlLease)
            return;

        try
        {
            if (leasedPlayer != null)
                leasedPlayer.OverrideControlInput = originalOverrideControlInput;
        }
        finally
        {
            leasedPlayer = null;
            ownsControlLease = false;
        }
    }
}
