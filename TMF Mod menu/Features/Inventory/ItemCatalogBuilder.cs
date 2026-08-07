using System;
using System.Collections.Generic;

namespace TMFModMenu.Features.Inventory;

internal readonly record struct ItemCatalogCandidate<T>(
    T Id,
    string EnumName,
    bool IsValid,
    bool IsEnabled,
    string TypeName,
    string DisplayName,
    int StackSize);

internal readonly record struct ItemCatalogEntry<T>(
    T Id,
    string TypeName,
    string DisplayName,
    int StackSize);

internal static class ItemCatalogBuilder
{
    public static ItemCatalogEntry<T>[] Build<T>(
        IEnumerable<ItemCatalogCandidate<T>> candidates)
    {
        if (candidates == null)
            return Array.Empty<ItemCatalogEntry<T>>();

        var result = new List<ItemCatalogEntry<T>>();
        foreach (var candidate in candidates)
        {
            string enumName = candidate.EnumName ?? string.Empty;
            string typeName = candidate.TypeName ?? string.Empty;
            if (!candidate.IsValid ||
                !candidate.IsEnabled ||
                enumName.Equals("None", StringComparison.OrdinalIgnoreCase) ||
                enumName.StartsWith("Unused", StringComparison.OrdinalIgnoreCase) ||
                enumName.StartsWith("z", StringComparison.Ordinal) ||
                typeName.Equals("None", StringComparison.OrdinalIgnoreCase) ||
                typeName.Equals("zCount", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(candidate.DisplayName))
                continue;

            result.Add(new ItemCatalogEntry<T>(
                candidate.Id,
                typeName,
                candidate.DisplayName,
                Math.Max(1, candidate.StackSize)));
        }

        result.Sort((left, right) =>
        {
            int typeComparison = StringComparer.OrdinalIgnoreCase.Compare(
                left.TypeName,
                right.TypeName);
            return typeComparison != 0
                ? typeComparison
                : StringComparer.OrdinalIgnoreCase.Compare(
                    left.DisplayName,
                    right.DisplayName);
        });
        return result.ToArray();
    }
}
