using System;
using System.Collections.Generic;

namespace TMFModMenu.Menu;

internal enum MenuItemKind
{
    Action,
    Toggle,
    Choice,
    Submenu
}

internal readonly record struct MenuInvocationContext(object Player);

internal abstract class MenuItem
{
    protected MenuItem(string label, bool isEnabled)
    {
        Label = string.IsNullOrWhiteSpace(label) ? "(Unnamed)" : label;
        IsEnabled = isEnabled;
    }

    public string Label { get; }

    public bool IsEnabled { get; private set; }

    public abstract MenuItemKind Kind { get; }

    public virtual string Value => string.Empty;

    internal int Version { get; private set; }

    public void SetEnabled(bool enabled)
    {
        if (IsEnabled == enabled)
            return;

        IsEnabled = enabled;
        MarkChanged();
    }

    internal virtual bool Activate(MenuInvocationContext context) => false;

    internal virtual bool Adjust(MenuInvocationContext context, int direction) => false;

    internal void MarkChanged()
    {
        Version++;
    }
}

internal sealed class MenuActionItem : MenuItem
{
    private readonly Action<MenuInvocationContext> action;
    private string value;

    public MenuActionItem(
        string label,
        Action<MenuInvocationContext> action = null,
        string value = "",
        bool isEnabled = true)
        : base(label, isEnabled)
    {
        this.action = action;
        this.value = value ?? string.Empty;
    }

    public override MenuItemKind Kind => MenuItemKind.Action;

    public override string Value => value;

    public void SetValue(string newValue)
    {
        newValue ??= string.Empty;
        if (value == newValue)
            return;

        value = newValue;
        MarkChanged();
    }

    internal override bool Activate(MenuInvocationContext context)
    {
        if (!IsEnabled)
            return false;

        action?.Invoke(context);
        MarkChanged();
        return true;
    }
}

internal sealed class MenuToggleItem : MenuItem
{
    private readonly Func<MenuInvocationContext, bool, bool> change;

    public MenuToggleItem(
        string label,
        bool initialValue = false,
        Func<MenuInvocationContext, bool, bool> change = null,
        bool isEnabled = true)
        : base(label, isEnabled)
    {
        IsOn = initialValue;
        this.change = change;
    }

    public override MenuItemKind Kind => MenuItemKind.Toggle;

    public bool IsOn { get; private set; }

    public override string Value => IsOn ? "ON" : "OFF";

    public void SetState(bool value)
    {
        if (IsOn == value)
            return;

        IsOn = value;
        MarkChanged();
    }

    internal override bool Activate(MenuInvocationContext context)
    {
        if (!IsEnabled)
            return false;

        bool requested = !IsOn;
        bool effective = change?.Invoke(context, requested) ?? requested;
        if (IsOn != effective)
        {
            IsOn = effective;
            MarkChanged();
        }
        return true;
    }
}

internal sealed class MenuChoiceItem : MenuItem
{
    private readonly string[] choices;
    private readonly Func<MenuInvocationContext, int, int> change;

    public MenuChoiceItem(
        string label,
        IEnumerable<string> choices,
        int selectedIndex = 0,
        Func<MenuInvocationContext, int, int> change = null,
        bool isEnabled = true)
        : base(label, isEnabled)
    {
        this.choices = choices == null
            ? Array.Empty<string>()
            : new List<string>(choices).ToArray();
        SelectedIndex = this.choices.Length == 0
            ? -1
            : Math.Clamp(selectedIndex, 0, this.choices.Length - 1);
        this.change = change;
    }

    public override MenuItemKind Kind => MenuItemKind.Choice;

    public int SelectedIndex { get; private set; }

    public override string Value => SelectedIndex < 0
        ? string.Empty
        : choices[SelectedIndex];

    public bool IsNumericValue => int.TryParse(Value, out _);

    internal override bool Activate(MenuInvocationContext context) =>
        Adjust(context, 1);

    internal override bool Adjust(MenuInvocationContext context, int direction)
    {
        if (!IsEnabled || choices.Length == 0 || direction == 0)
            return false;

        int requested = (SelectedIndex + Math.Sign(direction) + choices.Length) % choices.Length;
        int effective = change?.Invoke(context, requested) ?? requested;
        int next = Math.Clamp(effective, 0, choices.Length - 1);
        if (SelectedIndex != next)
        {
            SelectedIndex = next;
            MarkChanged();
        }
        return true;
    }
}

internal sealed class MenuSubmenuItem : MenuItem
{
    private readonly Func<MenuInvocationContext, MenuPage> pageFactory;

    public MenuSubmenuItem(
        string label,
        Func<MenuInvocationContext, MenuPage> pageFactory,
        bool isEnabled = true)
        : base(label, isEnabled)
    {
        this.pageFactory = pageFactory;
    }

    public override MenuItemKind Kind => MenuItemKind.Submenu;

    public override string Value => ">";

    internal MenuPage CreatePage(MenuInvocationContext context) =>
        IsEnabled ? pageFactory?.Invoke(context) : null;
}
