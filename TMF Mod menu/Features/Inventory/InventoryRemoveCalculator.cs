using System;

namespace TMFModMenu.Features.Inventory;

internal enum InventoryRemoveDecision
{
    Cancel,
    One,
    All
}

internal readonly record struct InventoryRemoveResult(
    bool WasCancelled,
    int Before,
    int Requested,
    int UnfulfilledRemainder,
    int After,
    int ActualRemoved,
    bool WasAttempted = true)
{
    public int HostReportedRemoved => Requested - UnfulfilledRemainder;

    public int ActualNotRemoved => Math.Max(0, Requested - ActualRemoved);

    public bool RemovedAny => ActualRemoved > 0;

    public bool IsComplete =>
        WasAttempted &&
        !WasCancelled &&
        Requested > 0 &&
        ActualRemoved == Requested;
}

internal static class InventoryRemoveCalculator
{
    public static InventoryRemoveResult Execute(
        InventoryRemoveDecision decision,
        Func<int> readCurrentCount,
        Func<int, int> decrement)
    {
        if (decision == InventoryRemoveDecision.Cancel)
            return new InventoryRemoveResult(
                true,
                0,
                0,
                0,
                0,
                0,
                WasAttempted: false);

        if (decision is not InventoryRemoveDecision.One and
            not InventoryRemoveDecision.All)
        {
            throw new ArgumentOutOfRangeException(
                nameof(decision),
                decision,
                "Unknown inventory removal decision.");
        }

        if (readCurrentCount == null)
            throw new ArgumentNullException(nameof(readCurrentCount));
        if (decrement == null)
            throw new ArgumentNullException(nameof(decrement));

        int before = NormalizeCount(readCurrentCount());
        int requested = decision == InventoryRemoveDecision.All
            ? before
            : Math.Min(1, before);

        if (requested == 0)
        {
            return new InventoryRemoveResult(
                false,
                before,
                0,
                0,
                before,
                0);
        }

        // Total Miner returns the portion of the request it could not remove,
        // not the number removed or the new inventory count.
        int unfulfilled = Math.Clamp(decrement(requested), 0, requested);
        int after = NormalizeCount(readCurrentCount());

        // Counts are authoritative. Clamp the observed delta so concurrent or
        // inconsistent host state cannot produce a negative or oversized result.
        int actualRemoved = Math.Clamp(before - after, 0, requested);

        return new InventoryRemoveResult(
            false,
            before,
            requested,
            unfulfilled,
            after,
            actualRemoved);
    }

    private static int NormalizeCount(int count) => Math.Max(0, count);

    public static string FormatNotification(
        string displayName,
        InventoryRemoveResult result)
    {
        displayName = string.IsNullOrWhiteSpace(displayName)
            ? "item"
            : displayName.Trim();

        if (result.WasCancelled)
            return $"TMF: Remove all {displayName} cancelled.";
        if (!result.WasAttempted)
            return $"TMF: Could not remove {displayName}; request rejected (player or inventory unavailable).";
        if (result.Requested == 0)
            return $"TMF: No {displayName} found in the pack.";
        if (result.IsComplete)
            return $"TMF: Removed {result.ActualRemoved}/{result.Requested} {displayName}; {result.After} remain.";

        return $"TMF: Removed {result.ActualRemoved}/{result.Requested} {displayName}; {result.ActualNotRemoved} not removed, {result.After} remain.";
    }
}
