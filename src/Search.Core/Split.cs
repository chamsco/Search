namespace Search.Core;

public enum SplitAxis { Horizontal, Vertical }

/// <summary>
/// Tabs shown side by side, sharing one place in the row. Two to four pages, in a line, side by side or one above the other.
/// Port of Sources/Search/Split.swift. The pages are tab ids; the shape is a list with an axis and each page's share.
/// </summary>
public sealed class TabSplit
{
    public const int Most = 4;
    public const double LeastShare = 0.12;

    public Guid Id { get; } = Guid.NewGuid();
    public List<Guid> Tabs { get; }
    public SplitAxis Axis { get; set; }
    public List<double> Sizes { get; private set; }
    public Guid? Focused { get; set; }

    public TabSplit(IEnumerable<Guid> tabs, SplitAxis axis = SplitAxis.Horizontal, IEnumerable<double>? sizes = null)
    {
        Tabs = tabs.Distinct().Take(Most).ToList();
        Axis = axis;
        Sizes = Even(Tabs.Count);
        if (sizes?.ToList() is { } s && s.Count == Tabs.Count) Sizes = Clamp(s);
    }

    public Guid First => Tabs[0];
    public bool Holds(Guid tab) => Tabs.Contains(tab);
    public bool IsValid => Tabs.Count >= 2;

    public static List<double> Even(int n) => Enumerable.Repeat(n == 0 ? 0 : 1.0 / n, n).ToList();

    /// <summary>Shares that add up to one, none thinner than <see cref="LeastShare"/>.</summary>
    public static List<double> Clamp(IReadOnlyList<double> sizes)
    {
        if (sizes.Count == 0) return [];
        var least = Math.Min(LeastShare, 1.0 / sizes.Count);
        var s = sizes.Select(x => Math.Max(double.IsFinite(x) ? x : 0, least)).ToList();
        var sum = s.Sum();
        return s.Select(x => x / sum).ToList();
    }

    public bool CanAdd => Tabs.Count < Most;

    /// <summary>Another page at the end, sharing the room evenly.</summary>
    public bool Add(Guid tab)
    {
        if (!CanAdd || Holds(tab)) return false;
        Tabs.Add(tab);
        Sizes = Even(Tabs.Count);
        return true;
    }

    /// <summary>Another page at INDEX (0 first), sharing the room evenly: a tab let go on the left half of the first page goes first.</summary>
    public bool Insert(Guid tab, int index)
    {
        if (!CanAdd || Holds(tab)) return false;
        Tabs.Insert(Math.Clamp(index, 0, Tabs.Count), tab);
        Sizes = Even(Tabs.Count);
        return true;
    }

    /// <summary>OLD's place, and its share of the room, goes to WITH: the empty page beside a page becomes the tab picked for it.</summary>
    public bool Replace(Guid old, Guid with)
    {
        var at = Tabs.IndexOf(old);
        if (at < 0 || Holds(with)) return false;
        Tabs[at] = with;
        if (Focused == old) Focused = with;
        return true;
    }

    /// <summary>
    /// Where a page let go at (X, Y) over a stage of W by H goes: the index it takes among the pages.
    /// Over the first half of a page (left, or top when stacked) it goes before that page; over the second half, after it.
    /// </summary>
    public int DropIndex(double x, double y, double w, double h, double gap)
    {
        var rects = Rects(w, h, gap);
        for (var i = 0; i < rects.Count; i++)
        {
            var (rx, ry, rw, rh) = rects[i];
            var along = Axis == SplitAxis.Horizontal ? x : y;
            var start = Axis == SplitAxis.Horizontal ? rx : ry;
            var size = Axis == SplitAxis.Horizontal ? rw : rh;
            if (along < start + size + gap / 2 || i == rects.Count - 1) return along < start + size / 2 ? i : i + 1;
        }
        return Tabs.Count;
    }

    /// <summary>One page leaves; the rest share its room evenly. False once fewer than two are left: the split is over.</summary>
    public bool Remove(Guid tab)
    {
        if (Tabs.Remove(tab)) { Sizes = Even(Tabs.Count); if (Focused == tab) Focused = null; }
        return IsValid;
    }

    /// <summary>Drag the line after page INDEX by DELTA (a share of the whole): that page grows, the next shrinks.</summary>
    public void Resize(int index, double delta)
    {
        if (index < 0 || index >= Tabs.Count - 1) return;
        var a = Sizes[index] + delta;
        var b = Sizes[index + 1] - delta;
        var least = Math.Min(LeastShare, 1.0 / Tabs.Count);
        if (a < least) { b -= least - a; a = least; }
        if (b < least) { a -= least - b; b = least; }
        Sizes[index] = a;
        Sizes[index + 1] = b;
    }

    /// <summary>Where each page goes in a stage of W by H, with a GAP between pages.</summary>
    public List<(double X, double Y, double W, double H)> Rects(double w, double h, double gap)
    {
        var n = Tabs.Count;
        var room = (Axis == SplitAxis.Horizontal ? w : h) - gap * (n - 1);
        var list = new List<(double, double, double, double)>();
        var at = 0.0;
        for (var i = 0; i < n; i++)
        {
            var size = room * Sizes[i];
            list.Add(Axis == SplitAxis.Horizontal ? (at, 0, size, h) : (0, at, w, size));
            at += size + gap;
        }
        return list;
    }

    public SplitEntry ToEntry(Func<Guid, int> indexOf) => new()
    {
        Tabs = Tabs.Select(indexOf).ToList(), Axis = Axis, Sizes = [.. Sizes],
        Focused = Focused is Guid f ? indexOf(f) : 0,
    };
}
