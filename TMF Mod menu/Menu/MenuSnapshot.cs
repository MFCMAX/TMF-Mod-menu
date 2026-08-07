using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TMFModMenu.Menu;

internal readonly record struct MenuRenderRow(
    string Label,
    string Value,
    bool IsSelected,
    bool IsEnabled,
    MenuItemKind Kind);

internal sealed class MenuSnapshot
{
    public MenuSnapshot(
        string breadcrumb,
        string pageIndicator,
        MenuRenderRow[] rows)
    {
        Breadcrumb = breadcrumb ?? string.Empty;
        PageIndicator = pageIndicator ?? string.Empty;
        Rows = Array.AsReadOnly(rows ?? Array.Empty<MenuRenderRow>());
    }

    public string Breadcrumb { get; }

    public string PageIndicator { get; }

    public IReadOnlyList<MenuRenderRow> Rows { get; }
}
