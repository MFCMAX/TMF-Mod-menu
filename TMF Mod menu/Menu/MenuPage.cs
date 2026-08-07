using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TMFModMenu.Menu;

internal sealed class MenuPage
{
    private readonly List<MenuItem> items;
    private readonly ReadOnlyCollection<MenuItem> itemView;

    public MenuPage(string title, IEnumerable<MenuItem> items = null)
    {
        Title = string.IsNullOrWhiteSpace(title) ? "MENU" : title;
        this.items = Materialize(items);
        itemView = this.items.AsReadOnly();
    }

    public string Title { get; }

    public IReadOnlyList<MenuItem> Items => itemView;

    public int Version { get; private set; }

    internal long ContentVersion
    {
        get
        {
            long result = (long)Version << 32;
            foreach (var item in items)
                result += (uint)item.Version;
            return result;
        }
    }

    public void Add(MenuItem item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        items.Add(item);
        Version++;
    }

    public bool Remove(MenuItem item)
    {
        if (!items.Remove(item))
            return false;

        Version++;
        return true;
    }

    public void RemoveAt(int index)
    {
        items.RemoveAt(index);
        Version++;
    }

    public void ReplaceItems(IEnumerable<MenuItem> replacement)
    {
        var materialized = Materialize(replacement);
        items.Clear();
        items.AddRange(materialized);
        Version++;
    }

    private static List<MenuItem> Materialize(IEnumerable<MenuItem> source)
    {
        var result = source == null
            ? new List<MenuItem>()
            : new List<MenuItem>(source);

        if (result.Exists(item => item == null))
            throw new ArgumentException("Menu pages cannot contain null rows.", nameof(source));

        return result;
    }
}
