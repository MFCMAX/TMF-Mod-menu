using System;
using System.Collections.Generic;

namespace TMFModMenu.Features.Inventory;

internal readonly record struct InventoryGiveResult(
    int Requested,
    int Added,
    int Remainder,
    bool WasAttempted = true)
{
    public bool IsComplete => WasAttempted && Added == Requested;
}

internal static class InventoryGiveCalculator
{
    public static InventoryGiveResult Execute(
        int requested,
        Func<int, int> add)
    {
        requested = Math.Max(1, requested);
        int added = add == null
            ? 0
            : Math.Clamp(add(requested), 0, requested);
        return new InventoryGiveResult(
            requested,
            added,
            requested - added);
    }

    public static string[] QuantityOptions(int stackSize)
    {
        // Ten is a deliberate bulk request, independent of per-slot stack size.
        // Total Miner still splits durable/unstackable items into one-item slots.
        var values = new List<string> { "1", "10" };
        string stack = Math.Max(1, stackSize).ToString();
        if (!values.Contains(stack))
            values.Add(stack);
        return values.ToArray();
    }

    public static string FormatNotification(
        string displayName,
        InventoryGiveResult result)
    {
        displayName = string.IsNullOrWhiteSpace(displayName)
            ? "item"
            : displayName.Trim();

        if (result.Added == result.Requested)
            return $"TMF: Added {result.Added}/{result.Requested} {displayName}; 0 not added.";

        if (!result.WasAttempted)
            return $"TMF: Could not add {displayName}; request rejected (player or inventory unavailable).";

        if (result.Added == 0)
            return $"TMF: Could not add {displayName}; {result.Remainder} not added (pack full).";

        return $"TMF: Added {result.Added}/{result.Requested} {displayName}; {result.Remainder} not added.";
    }
}
