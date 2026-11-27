namespace Search.Core;

/// <summary>
/// Several tabs chosen at once in the column or the top strip, as in a file list:
/// a click picks one, Ctrl+click adds or takes away one, Shift+click picks every tab from the last one clicked to this one.
/// </summary>
public static class Picks
{
    /// <summary>Every item from ANCHOR to TO in ORDER, both included, whichever comes first; only TO when the anchor is not there.</summary>
    public static List<T> Range<T>(IReadOnlyList<T> order, T? anchor, T to) where T : class
    {
        var a = anchor is null ? -1 : IndexOf(order, anchor);
        var b = IndexOf(order, to);
        if (b < 0) return [];
        if (a < 0) return [to];
        var (lo, hi) = a <= b ? (a, b) : (b, a);
        return order.Skip(lo).Take(hi - lo + 1).ToList();
    }

    /// <summary>The picks after Ctrl+click on ITEM: taken away if it was picked, added if not. LIVE, the page on screen, counts as picked when nothing else is yet.</summary>
    public static List<T> Toggle<T>(IReadOnlyList<T> order, IReadOnlyCollection<T> picked, T? live, T item) where T : class
    {
        var now = picked.ToList();
        if (now.Count == 0 && live is not null && live != item && IndexOf(order, live) >= 0) now.Add(live);
        if (now.Contains(item)) now.Remove(item); else now.Add(item);
        return order.Where(now.Contains).ToList();
    }

    static int IndexOf<T>(IReadOnlyList<T> order, T item) where T : class
    {
        for (var i = 0; i < order.Count; i++) if (ReferenceEquals(order[i], item)) return i;
        return -1;
    }
}
