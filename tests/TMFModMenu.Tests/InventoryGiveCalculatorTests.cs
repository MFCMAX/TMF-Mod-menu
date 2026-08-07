using TMFModMenu.Features.Inventory;

namespace TMFModMenu.Tests;

public sealed class InventoryGiveCalculatorTests
{
    [Theory]
    [InlineData(10, 10, 10, 0, true)]
    [InlineData(10, 4, 4, 6, false)]
    [InlineData(10, 0, 0, 10, false)]
    [InlineData(10, 99, 10, 0, true)]
    public void ResultUsesActualAddedQuantityAndRemainder(
        int requested,
        int hostResult,
        int expectedAdded,
        int expectedRemainder,
        bool complete)
    {
        var result = InventoryGiveCalculator.Execute(
            requested,
            _ => hostResult);

        Assert.Equal(requested, result.Requested);
        Assert.Equal(expectedAdded, result.Added);
        Assert.Equal(expectedRemainder, result.Remainder);
        Assert.Equal(complete, result.IsComplete);
    }

    [Theory]
    [InlineData(1, new[] { "1", "10" })]
    [InlineData(10, new[] { "1", "10" })]
    [InlineData(100, new[] { "1", "10", "100" })]
    public void QuantityOptionsIncludeBulkTenAndDeduplicateOneStack(
        int stackSize,
        string[] expected)
    {
        Assert.Equal(expected, InventoryGiveCalculator.QuantityOptions(stackSize));
    }

    [Theory]
    [InlineData(10, 10, "TMF: Added 10/10 Dirt; 0 not added.")]
    [InlineData(10, 4, "TMF: Added 4/10 Dirt; 6 not added.")]
    [InlineData(10, 0, "TMF: Could not add Dirt; 10 not added (pack full).")]
    public void NotificationDistinguishesCompletePartialAndFailedAdds(
        int requested,
        int hostResult,
        string expected)
    {
        var result = InventoryGiveCalculator.Execute(requested, _ => hostResult);

        Assert.Equal(
            expected,
            InventoryGiveCalculator.FormatNotification(" Dirt ", result));
    }

    [Fact]
    public void NotificationDistinguishesRejectedRequestFromFullPack()
    {
        var result = new InventoryGiveResult(
            Requested: 10,
            Added: 0,
            Remainder: 10,
            WasAttempted: false);

        Assert.False(result.IsComplete);
        Assert.Equal(
            "TMF: Could not add Dirt; request rejected (player or inventory unavailable).",
            InventoryGiveCalculator.FormatNotification("Dirt", result));
    }
}
