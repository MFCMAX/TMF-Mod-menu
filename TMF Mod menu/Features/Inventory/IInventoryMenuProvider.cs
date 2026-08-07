using TMFModMenu.Menu;

namespace TMFModMenu.Features.Inventory;

internal interface IInventoryMenuProvider
{
    MenuPage CreateInventoryPage();
}

internal sealed class UnavailableInventoryMenuProvider : IInventoryMenuProvider
{
    public MenuPage CreateInventoryPage() =>
        new(
            "Inventory",
            new[]
            {
                new MenuActionItem("(Inventory unavailable)", isEnabled: false)
            });
}
