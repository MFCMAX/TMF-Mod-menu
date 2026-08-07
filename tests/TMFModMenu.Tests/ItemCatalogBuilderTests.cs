using System.Linq;
using TMFModMenu.Features.Inventory;

namespace TMFModMenu.Tests;

public sealed class ItemCatalogBuilderTests
{
    [Fact]
    public void CatalogFiltersSentinelsInvalidDisabledAndBlankEntries()
    {
        var candidates = new[]
        {
            Candidate(0, "None", true, true, "None", "None", 1),
            Candidate(1, "Unused1", true, true, "Item", "Unused", 10),
            Candidate(2, "zLastItemID", true, true, "Item", "Last", 1),
            Candidate(3, "Disabled", true, false, "Item", "Disabled", 1),
            Candidate(4, "Invalid", false, true, "Item", "Invalid", 1),
            Candidate(5, "Blank", true, true, "Item", " ", 1),
            Candidate(6, "Dirt", true, true, "Block", "Dirt", 100),
            Candidate(7, "SteelPickaxe", true, true, "Tool", "Steel Pickaxe", 1)
        };

        var result = ItemCatalogBuilder.Build(candidates);

        Assert.Equal(2, result.Length);
        Assert.Equal("Dirt", result[0].DisplayName);
        Assert.Equal("Block", result[0].TypeName);
        Assert.Equal(100, result[0].StackSize);
        Assert.Equal("Steel Pickaxe", result[1].DisplayName);
    }

    [Fact]
    public void CatalogSortsByTypeThenDisplayNameAndClampsStackSize()
    {
        var candidates = new[]
        {
            Candidate(1, "Zeta", true, true, "Item", "Zeta", 0),
            Candidate(2, "Alpha", true, true, "Item", "Alpha", 5),
            Candidate(3, "Stone", true, true, "Block", "Stone", 100)
        };

        var result = ItemCatalogBuilder.Build(candidates);

        Assert.Equal(new[] { "Stone", "Alpha", "Zeta" },
            result.Select(item => item.DisplayName));
        Assert.Equal(1, result[2].StackSize);
    }

    private static ItemCatalogCandidate<int> Candidate(
        int id,
        string enumName,
        bool valid,
        bool enabled,
        string type,
        string display,
        int stack) =>
        new(id, enumName, valid, enabled, type, display, stack);
}
