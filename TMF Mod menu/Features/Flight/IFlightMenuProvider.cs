using TMFModMenu.Menu;

namespace TMFModMenu.Features.Flight;

internal interface IFlightMenuProvider
{
    MenuItem CreateFlightMenuItem();
}

internal sealed class UnavailableFlightMenuProvider : IFlightMenuProvider
{
    public MenuItem CreateFlightMenuItem() =>
        new MenuToggleItem("Flight", isEnabled: false);
}
