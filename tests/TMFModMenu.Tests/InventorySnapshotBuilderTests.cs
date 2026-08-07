using TMFModMenu.Features.Inventory;

namespace TMFModMenu.Tests;

public sealed class InventorySnapshotBuilderTests
{
    [Fact]
    public void BuildReadsOnlyPackSlotsAndPreservesSlotNumbers()
    {
        var source = new[]
        {
            new InventorySourceSlot("Dirt", 11),
            new InventorySourceSlot("", 0),
            new InventorySourceSlot("Steel Pickaxe", 1),
            new InventorySourceSlot("Equipped Amulet", 1),
            new InventorySourceSlot("Temporary", 8)
        };

        var snapshot = InventorySnapshotBuilder.Build(packSize: 3, source);

        Assert.Equal(2, snapshot.Length);
        Assert.Equal(new InventoryPackEntry(0, "Dirt", 11), snapshot[0]);
        Assert.Equal(new InventoryPackEntry(2, "Steel Pickaxe", 1), snapshot[1]);
        Assert.Equal("Equipped Amulet", source[3].Name);
        Assert.Equal(1, source[3].Count);
    }

    [Fact]
    public void BuildHandlesTrimmedRawListsAndEmptyPacks()
    {
        var shortSource = new[]
        {
            new InventorySourceSlot("Wood", 2)
        };

        var shortSnapshot = InventorySnapshotBuilder.Build(10, shortSource);
        var emptySnapshot = InventorySnapshotBuilder.Build(
            10,
            new[] { new InventorySourceSlot("", 0) });

        Assert.Single(shortSnapshot);
        Assert.Equal(0, shortSnapshot[0].SlotIndex);
        Assert.Empty(emptySnapshot);
        Assert.Empty(InventorySnapshotBuilder.Build(0, shortSource));
    }
}
