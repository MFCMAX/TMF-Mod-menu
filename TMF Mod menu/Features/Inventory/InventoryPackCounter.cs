using System;
using System.Collections.Generic;

namespace TMFModMenu.Features.Inventory;

internal readonly record struct InventoryPackSourceSlot<T>(
    T ItemId,
    int Count,
    bool IsEmpty = false);

internal readonly record struct InventoryPackItemCount<T>(
    T ItemId,
    int Count);

internal static class InventoryPackCounter
{
    public static InventoryPackItemCount<T>[] Build<T>(
        int packSize,
        IReadOnlyList<InventoryPackSourceSlot<T>> rawSlots,
        IEqualityComparer<T> comparer = null)
    {
        if (packSize <= 0 || rawSlots == null)
            return Array.Empty<InventoryPackItemCount<T>>();

        comparer ??= EqualityComparer<T>.Default;
        var counts = new Dictionary<T, int>(comparer);
        var order = new List<T>();
        int limit = Math.Min(packSize, rawSlots.Count);
        for (int slot = 0; slot < limit; slot++)
        {
            var entry = rawSlots[slot];
            if (entry.IsEmpty || entry.Count <= 0)
                continue;

            if (counts.TryGetValue(entry.ItemId, out int current))
                counts[entry.ItemId] = current + entry.Count;
            else
            {
                counts.Add(entry.ItemId, entry.Count);
                order.Add(entry.ItemId);
            }
        }

        var result = new InventoryPackItemCount<T>[order.Count];
        for (int i = 0; i < order.Count; i++)
            result[i] = new InventoryPackItemCount<T>(order[i], counts[order[i]]);
        return result;
    }

    public static int Count<T>(
        int packSize,
        IReadOnlyList<InventoryPackSourceSlot<T>> rawSlots,
        T itemId,
        IEqualityComparer<T> comparer = null)
    {
        comparer ??= EqualityComparer<T>.Default;
        foreach (var entry in Build(packSize, rawSlots, comparer))
        {
            if (comparer.Equals(entry.ItemId, itemId))
                return entry.Count;
        }
        return 0;
    }
}
