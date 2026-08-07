using TMFModMenu.Features.Inventory;

namespace TMFModMenu.Tests;

public sealed class InventoryPackCounterTests
{
    [Fact]
    public void BuildAggregatesPackStacksAndExcludesEquipmentAndTemporarySlots()
    {
        var source = new[]
        {
            new InventoryPackSourceSlot<int>(1, 7),
            new InventoryPackSourceSlot<int>(2, 1),
            new InventoryPackSourceSlot<int>(1, 5),
            new InventoryPackSourceSlot<int>(3, 1), // equipment
            new InventoryPackSourceSlot<int>(1, 99) // temporary
        };

        var result = InventoryPackCounter.Build(packSize: 3, source);

        Assert.Equal(2, result.Length);
        Assert.Equal(new InventoryPackItemCount<int>(1, 12), result[0]);
        Assert.Equal(new InventoryPackItemCount<int>(2, 1), result[1]);
        Assert.Equal(12, InventoryPackCounter.Count(3, source, 1));
        Assert.Equal(0, InventoryPackCounter.Count(3, source, 3));
    }

    [Fact]
    public void BuildHandlesTrimmedListsEmptySlotsAndInvalidCounts()
    {
        var source = new[]
        {
            new InventoryPackSourceSlot<string>("empty", 50, IsEmpty: true),
            new InventoryPackSourceSlot<string>("zero", 0),
            new InventoryPackSourceSlot<string>("negative", -3),
            new InventoryPackSourceSlot<string>("wood", 2)
        };

        var result = InventoryPackCounter.Build(packSize: 20, source);

        Assert.Single(result);
        Assert.Equal(new InventoryPackItemCount<string>("wood", 2), result[0]);
        Assert.Empty(InventoryPackCounter.Build(0, source));
        Assert.Empty(InventoryPackCounter.Build<string>(10, null));
    }
}
