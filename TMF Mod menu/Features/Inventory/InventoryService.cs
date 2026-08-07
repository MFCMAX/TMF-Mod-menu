using System;
using System.Collections.Generic;
using System.Linq;
using StudioForge.TotalMiner;
using StudioForge.TotalMiner.API;
using TMFModMenu.Menu;

namespace TMFModMenu.Features.Inventory;

internal sealed class InventoryService : IInventoryMenuProvider
{
    private ITMGame game;
    private ItemCatalogEntry<Item>[] catalog = Array.Empty<ItemCatalogEntry<Item>>();

    public void Initialize(ITMGame game)
    {
        this.game = game;
        catalog = BuildCatalog();
    }

    public void Clear()
    {
        game = null;
        catalog = Array.Empty<ItemCatalogEntry<Item>>();
    }

    public MenuPage CreateInventoryPage()
    {
        return new MenuPage(
            "Inventory",
            new MenuItem[]
            {
                new MenuSubmenuItem(
                    "View Pack",
                    context => InventoryViewer.CreatePackPage(context.Player)),
                new MenuSubmenuItem(
                    "Give Item",
                    _ => CreateGiveCatalogPage(),
                    isEnabled: catalog.Length > 0),
                new MenuSubmenuItem(
                    "Remove Item",
                    context => CreateRemoveCatalogPage(context.Player),
                    isEnabled: game != null)
            });
    }

    private MenuPage CreateGiveCatalogPage()
    {
        if (catalog.Length == 0)
            return CreateMessagePage("Give Item", "(Catalog unavailable)");

        var rows = catalog
            .GroupBy(item => item.TypeName, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var groupItems = group.ToArray();
                return (MenuItem)new MenuSubmenuItem(
                    group.Key,
                    _ => CreateItemGroupPage(group.Key, groupItems));
            })
            .ToArray();
        return new MenuPage("Give Item", rows);
    }

    private MenuPage CreateItemGroupPage(
        string typeName,
        ItemCatalogEntry<Item>[] groupItems)
    {
        var rows = new MenuItem[groupItems.Length];
        for (int i = 0; i < groupItems.Length; i++)
        {
            var item = groupItems[i];
            rows[i] = new MenuSubmenuItem(
                item.DisplayName,
                _ => CreateQuantityPage(item));
        }
        return new MenuPage(typeName, rows);
    }

    private MenuPage CreateQuantityPage(ItemCatalogEntry<Item> item)
    {
        var quantity = new MenuChoiceItem(
            "Quantity",
            InventoryGiveCalculator.QuantityOptions(item.StackSize));
        var addedRow = new MenuActionItem(
            "Added",
            value: "--",
            isEnabled: false);
        var remainderRow = new MenuActionItem(
            "Not Added",
            value: "--",
            isEnabled: false);
        var give = new MenuActionItem(
            "Give",
            context =>
            {
                int requested = int.Parse(quantity.Value);
                var result = Give(context.Player, item.Id, requested);
                addedRow.SetValue($"{result.Added}/{result.Requested}");
                remainderRow.SetValue(result.Remainder.ToString());
                game?.AddNotification(InventoryGiveCalculator.FormatNotification(
                    item.DisplayName,
                    result));
            });

        return new MenuPage(
            item.DisplayName,
            new MenuItem[] { quantity, give, addedRow, remainderRow });
    }

    private InventoryGiveResult Give(
        object playerObject,
        Item item,
        int requested)
    {
        if (playerObject is not ITMPlayer player ||
            game == null ||
            !ReferenceEquals(game.GetLocalPlayer(player.PlayerIndex), player) ||
            player.Inventory == null)
            return new InventoryGiveResult(
                requested,
                0,
                requested,
                WasAttempted: false);

        return InventoryGiveCalculator.Execute(
            requested,
            count => player.Inventory.AddToInventory(item, count));
    }

    private MenuPage CreateRemoveCatalogPage(object playerObject)
    {
        if (playerObject is not ITMPlayer player ||
            game == null ||
            !ReferenceEquals(game.GetLocalPlayer(player.PlayerIndex), player) ||
            player.Inventory == null)
            return CreateMessagePage("Remove Item", "(Player unavailable)");

        var counts = CapturePackCounts(player.Inventory);

        if (counts.Length == 0)
            return CreateMessagePage("Remove Item", "(Pack is empty)");

        var rows = counts
            .Select(entry => new
            {
                Item = entry.ItemId,
                Name = DisplayName(entry.ItemId)
            })
            .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .Select(entry => (MenuItem)new MenuSubmenuItem(
                entry.Name,
                _ => CreateRemoveItemPage(
                    player,
                    entry.Item,
                    entry.Name)))
            .ToArray();

        return new MenuPage("Remove Item", rows);
    }

    private MenuPage CreateRemoveItemPage(
        ITMPlayer player,
        Item item,
        string displayName)
    {
        var currentRow = new MenuActionItem(
            "Pack Count",
            value: ReadPackCount(player, item).ToString(),
            isEnabled: false);
        var removedRow = new MenuActionItem(
            "Last Removed",
            value: "--",
            isEnabled: false);
        var notRemovedRow = new MenuActionItem(
            "Not Removed",
            value: "--",
            isEnabled: false);

        void ShowResult(InventoryRemoveResult result)
        {
            if (result.WasCancelled)
            {
                game?.AddNotification(
                    InventoryRemoveCalculator.FormatNotification(displayName, result));
                return;
            }

            if (!result.WasAttempted)
            {
                removedRow.SetValue("REJECTED");
                notRemovedRow.SetValue("--");
                game?.AddNotification(
                    InventoryRemoveCalculator.FormatNotification(displayName, result));
                return;
            }

            currentRow.SetValue(result.After.ToString());
            removedRow.SetValue($"{result.ActualRemoved}/{result.Requested}");
            notRemovedRow.SetValue(result.ActualNotRemoved.ToString());
            game?.AddNotification(
                InventoryRemoveCalculator.FormatNotification(displayName, result));
        }

        var removeOne = new MenuActionItem(
            "Remove 1",
            context => ShowResult(Remove(
                context.Player,
                item,
                InventoryRemoveDecision.One)));
        var removeAll = new MenuSubmenuItem(
            "Remove All",
            _ => CreateRemoveAllConfirmationPage(
                item,
                displayName,
                ShowResult));

        return new MenuPage(
            displayName,
            new MenuItem[]
            {
                currentRow,
                removeOne,
                removeAll,
                removedRow,
                notRemovedRow
            });
    }

    private MenuPage CreateRemoveAllConfirmationPage(
        Item item,
        string displayName,
        Action<InventoryRemoveResult> showResult)
    {
        var confirmation = new MenuChoiceItem(
            "Confirm",
            new[] { "CANCEL", "REMOVE" });
        var status = new MenuActionItem(
            "Status",
            value: "WAITING",
            isEnabled: false);
        var apply = new MenuActionItem(
            "Apply",
            context =>
            {
                var result = confirmation.Value == "REMOVE"
                    ? Remove(context.Player, item, InventoryRemoveDecision.All)
                    : InventoryRemoveCalculator.Execute(
                        InventoryRemoveDecision.Cancel,
                        null,
                        null);
                status.SetValue(result.WasCancelled
                    ? "CANCELLED"
                    : result.WasAttempted ? "DONE" : "REJECTED");
                showResult(result);
            });

        return new MenuPage(
            $"Remove All {displayName}",
            new MenuItem[] { confirmation, apply, status });
    }

    private InventoryRemoveResult Remove(
        object playerObject,
        Item item,
        InventoryRemoveDecision decision)
    {
        if (playerObject is not ITMPlayer player ||
            game == null ||
            !ReferenceEquals(game.GetLocalPlayer(player.PlayerIndex), player) ||
            player.Inventory == null)
        {
            return new InventoryRemoveResult(
                false,
                0,
                0,
                0,
                0,
                0,
                WasAttempted: false);
        }

        var inventory = player.Inventory;
        return InventoryRemoveCalculator.Execute(
            decision,
            () => ReadPackCount(inventory, item),
            quantity => inventory.DecrementItem(item, quantity));
    }

    private static int ReadPackCount(ITMPlayer player, Item item) =>
        player?.Inventory == null
            ? 0
            : ReadPackCount(player.Inventory, item);

    private static int ReadPackCount(ITMInventory inventory, Item item)
    {
        var raw = CaptureRawSlots(inventory);
        return InventoryPackCounter.Count(inventory.PackSize, raw, item);
    }

    private static InventoryPackItemCount<Item>[] CapturePackCounts(
        ITMInventory inventory)
    {
        var raw = CaptureRawSlots(inventory);
        return InventoryPackCounter.Build(inventory.PackSize, raw);
    }

    private static InventoryPackSourceSlot<Item>[] CaptureRawSlots(
        ITMInventory inventory)
    {
        var items = inventory.Items;
        var source = new InventoryPackSourceSlot<Item>[items.Count];
        for (int slot = 0; slot < items.Count; slot++)
        {
            var item = items[slot];
            source[slot] = new InventoryPackSourceSlot<Item>(
                item.ItemID,
                item.Count,
                item.ItemID == Item.None);
        }
        return source;
    }

    private static string DisplayName(Item item)
    {
        string name = ItemData.ToString(item);
        return string.IsNullOrWhiteSpace(name)
            ? item.ToString()
            : name;
    }

    private static ItemCatalogEntry<Item>[] BuildCatalog()
    {
        var candidates = new List<ItemCatalogCandidate<Item>>();
        foreach (Item item in Enum.GetValues<Item>())
        {
            string enumName = Enum.GetName(typeof(Item), item);
            if (enumName == null ||
                enumName.Equals("None", StringComparison.OrdinalIgnoreCase) ||
                enumName.StartsWith("Unused", StringComparison.OrdinalIgnoreCase) ||
                enumName.StartsWith("z", StringComparison.Ordinal))
                continue;

            bool isValid = ItemData.IsValid(item);
            bool isEnabled = isValid && ItemData.IsEnabled(item);
            if (!isValid || !isEnabled)
                continue;

            var type = ItemData.GetItemType(item);
            candidates.Add(new ItemCatalogCandidate<Item>(
                item,
                enumName,
                isValid,
                isEnabled,
                type.ToString(),
                ItemData.ToString(item),
                ItemData.GetStackSize(item)));
        }

        return ItemCatalogBuilder.Build(candidates);
    }

    private static MenuPage CreateMessagePage(string title, string message) =>
        new(
            title,
            new[] { new MenuActionItem(message, isEnabled: false) });
}
