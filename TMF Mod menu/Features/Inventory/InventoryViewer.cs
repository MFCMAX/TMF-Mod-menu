using System;
using System.Collections.Generic;
using StudioForge.TotalMiner;
using StudioForge.TotalMiner.API;
using TMFModMenu.Menu;

namespace TMFModMenu.Features.Inventory;

internal static class InventoryViewer
{
    public static MenuPage CreatePackPage(object playerObject)
    {
        if (playerObject is not ITMPlayer player || player.Inventory == null)
            return CreateMessagePage("View Pack", "(Player unavailable)");

        var inventory = player.Inventory;
        var items = inventory.Items;
        int limit = Math.Min((int)inventory.PackSize, items.Count);
        var source = new InventorySourceSlot[limit];

        for (int slotIndex = 0; slotIndex < limit; slotIndex++)
        {
            var item = items[slotIndex];
            if (item.ItemID == Item.None || item.Count <= 0)
                continue;

            string name = ItemData.ToString(item.ItemID);
            if (string.IsNullOrWhiteSpace(name))
                name = item.ItemID.ToString();
            source[slotIndex] = new InventorySourceSlot(name, item.Count);
        }

        var snapshot = InventorySnapshotBuilder.Build(inventory.PackSize, source);
        if (snapshot.Length == 0)
            return CreateMessagePage("View Pack", "(Pack is empty)");

        var rows = new List<MenuItem>(snapshot.Length);
        foreach (var entry in snapshot)
        {
            rows.Add(new MenuActionItem(
                $"{entry.SlotIndex + 1:00} {entry.Name}",
                value: $"x{entry.Count}",
                isEnabled: false));
        }

        return new MenuPage("View Pack", rows);
    }

    private static MenuPage CreateMessagePage(string title, string message) =>
        new(
            title,
            new[]
            {
                new MenuActionItem(message, isEnabled: false)
            });
}
