using TMFModMenu.Features.Inventory;

namespace TMFModMenu.Tests;

public sealed class InventoryRemoveCalculatorTests
{
    [Fact]
    public void RemoveOneUsesBeforeAndAfterCounts()
    {
        int[] counts = { 8, 7 };
        int readIndex = 0;
        int requestedByHost = 0;

        var result = InventoryRemoveCalculator.Execute(
            InventoryRemoveDecision.One,
            () => counts[readIndex++],
            requested =>
            {
                requestedByHost = requested;
                return 0;
            });

        Assert.Equal(1, requestedByHost);
        Assert.Equal(8, result.Before);
        Assert.Equal(1, result.Requested);
        Assert.Equal(0, result.UnfulfilledRemainder);
        Assert.Equal(1, result.HostReportedRemoved);
        Assert.Equal(7, result.After);
        Assert.Equal(1, result.ActualRemoved);
        Assert.True(result.RemovedAny);
        Assert.True(result.IsComplete);
    }

    [Fact]
    public void RemoveAllRequestsCurrentCountInsteadOfSentinelQuantity()
    {
        int[] counts = { 37, 0 };
        int readIndex = 0;
        int requestedByHost = 0;

        var result = InventoryRemoveCalculator.Execute(
            InventoryRemoveDecision.All,
            () => counts[readIndex++],
            requested =>
            {
                requestedByHost = requested;
                return 0;
            });

        Assert.Equal(37, requestedByHost);
        Assert.NotEqual(int.MaxValue, requestedByHost);
        Assert.Equal(37, result.Requested);
        Assert.Equal(37, result.ActualRemoved);
        Assert.True(result.IsComplete);
    }

    [Fact]
    public void UnfulfilledRemainderIsNotMistakenForActualRemoved()
    {
        int[] counts = { 10, 4 };
        int readIndex = 0;

        var result = InventoryRemoveCalculator.Execute(
            InventoryRemoveDecision.All,
            () => counts[readIndex++],
            _ => 4);

        Assert.Equal(4, result.UnfulfilledRemainder);
        Assert.Equal(6, result.HostReportedRemoved);
        Assert.Equal(6, result.ActualRemoved);
        Assert.False(result.IsComplete);
    }

    [Fact]
    public void ObservedCountsWinWhenHostRemainderIsInconsistent()
    {
        int[] counts = { 10, 7 };
        int readIndex = 0;

        var result = InventoryRemoveCalculator.Execute(
            InventoryRemoveDecision.All,
            () => counts[readIndex++],
            _ => 0);

        Assert.Equal(10, result.HostReportedRemoved);
        Assert.Equal(3, result.ActualRemoved);
        Assert.Equal(7, result.ActualNotRemoved);
        Assert.False(result.IsComplete);
    }

    [Theory]
    [InlineData(5, 8, 0)]
    [InlineData(5, -20, 5)]
    public void ObservedDeltaIsDefensivelyClamped(
        int before,
        int after,
        int expectedRemoved)
    {
        int[] counts = { before, after };
        int readIndex = 0;

        var result = InventoryRemoveCalculator.Execute(
            InventoryRemoveDecision.All,
            () => counts[readIndex++],
            _ => 0);

        Assert.Equal(expectedRemoved, result.ActualRemoved);
    }

    [Fact]
    public void EmptyInventoryDoesNotCallDecrement()
    {
        int decrementCalls = 0;

        var result = InventoryRemoveCalculator.Execute(
            InventoryRemoveDecision.All,
            () => 0,
            _ =>
            {
                decrementCalls++;
                return 0;
            });

        Assert.Equal(0, decrementCalls);
        Assert.Equal(0, result.Requested);
        Assert.Equal(0, result.ActualRemoved);
        Assert.False(result.RemovedAny);
        Assert.False(result.IsComplete);
    }

    [Fact]
    public void CancelMakesNoInventoryCalls()
    {
        int readCalls = 0;
        int decrementCalls = 0;

        var result = InventoryRemoveCalculator.Execute(
            InventoryRemoveDecision.Cancel,
            () =>
            {
                readCalls++;
                return 12;
            },
            _ =>
            {
                decrementCalls++;
                return 0;
            });

        Assert.True(result.WasCancelled);
        Assert.Equal(0, readCalls);
        Assert.Equal(0, decrementCalls);
        Assert.Equal(0, result.Requested);
        Assert.Equal(0, result.ActualRemoved);
        Assert.False(result.WasAttempted);
    }

    [Theory]
    [InlineData(
        "Dirt",
        10,
        0,
        0,
        "TMF: Removed 10/10 Dirt; 0 remain.")]
    [InlineData(
        "Dirt",
        10,
        6,
        6,
        "TMF: Removed 4/10 Dirt; 6 not removed, 6 remain.")]
    public void NotificationReportsActualRemovalAndRemainder(
        string name,
        int before,
        int after,
        int hostRemainder,
        string expected)
    {
        int readCount = 0;
        var result = InventoryRemoveCalculator.Execute(
            InventoryRemoveDecision.All,
            () => readCount++ == 0 ? before : after,
            _ => hostRemainder);

        Assert.Equal(
            expected,
            InventoryRemoveCalculator.FormatNotification(name, result));
    }

    [Fact]
    public void NotificationDistinguishesCancelAndRejectedRequest()
    {
        var cancelled = InventoryRemoveCalculator.Execute(
            InventoryRemoveDecision.Cancel,
            null,
            null);
        var rejected = new InventoryRemoveResult(
            WasCancelled: false,
            Before: 0,
            Requested: 0,
            UnfulfilledRemainder: 0,
            After: 0,
            ActualRemoved: 0,
            WasAttempted: false);

        Assert.Equal(
            "TMF: Remove all Dirt cancelled.",
            InventoryRemoveCalculator.FormatNotification("Dirt", cancelled));
        Assert.Equal(
            "TMF: Could not remove Dirt; request rejected (player or inventory unavailable).",
            InventoryRemoveCalculator.FormatNotification("Dirt", rejected));
    }

    [Fact]
    public void NotificationUsesObservedCountsWhenHostRemainderDisagrees()
    {
        int[] counts = { 10, 7 };
        int readIndex = 0;
        var result = InventoryRemoveCalculator.Execute(
            InventoryRemoveDecision.All,
            () => counts[readIndex++],
            _ => 0);

        Assert.Equal(
            "TMF: Removed 3/10 Dirt; 7 not removed, 7 remain.",
            InventoryRemoveCalculator.FormatNotification("Dirt", result));
    }
}
