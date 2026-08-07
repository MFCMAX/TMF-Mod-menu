using System;
using System.Collections.Generic;

namespace TMFModMenu.Features.Inventory;

internal readonly record struct InventorySourceSlot(string Name, int Count);

internal readonly record struct InventoryPackEntry(
    int SlotIndex,
    string Name,
    int Count);

internal static class InventorySnapshotBuilder
{
    public static InventoryPackEntry[] Build(
        int packSize,
        IReadOnlyList<InventorySourceSlot> rawSlots)
    {
        if (rawSlots == null || packSize <= 0)
            return Array.Empty<InventoryPackEntry>();

        int limit = Math.Min(packSize, rawSlots.Count);
        var entries = new List<InventoryPackEntry>(limit);
        for (int slotIndex = 0; slotIndex < limit; slotIndex++)
        {
            var slot = rawSlots[slotIndex];
            if (slot.Count <= 0 || string.IsNullOrWhiteSpace(slot.Name))
                continue;

            entries.Add(new InventoryPackEntry(
                slotIndex,
                slot.Name,
                slot.Count));
        }

        return entries.ToArray();
    }
}
