using TMFModMenu.Menu;

namespace TMFModMenu.Features.Survival;

internal interface ISurvivalMenuProvider
{
    MenuItem CreateSurvivalMenuItem();
}

internal sealed class UnavailableSurvivalMenuProvider : ISurvivalMenuProvider
{
    public MenuItem CreateSurvivalMenuItem() =>
        new MenuToggleItem("Survival Assist", isEnabled: false);
}
