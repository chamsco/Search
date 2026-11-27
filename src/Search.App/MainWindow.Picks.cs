using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Search.Core;

namespace Search.App;

/// <summary>
/// Several tabs at once, in the column or along the top: Ctrl+click adds or takes away one, Shift+click picks every
/// tab from the last one clicked to this one. Right-click one of them and the menu acts on them all.
/// </summary>
public partial class MainWindow
{
    Tab? pickAnchor;

    List<Tab> Picked() => Ordered().Where(t => t.Picked).ToList();

    void ClearPicks()
    {
        foreach (var t in Ordered()) t.Picked = false;
        pickAnchor = null;
    }

    void SetPicks(IEnumerable<Tab> chosen)
    {
        var set = chosen.ToHashSet();
        foreach (var t in Ordered()) t.Picked = set.Contains(t);
        App.Trace($"picked {set.Count}");
    }

    /// <summary>A click on a row with Ctrl or Shift held: true when it changed the picks (and nothing else should happen).</summary>
    bool PickClick(Tab t)
    {
        var ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
        var shift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt) || (!ctrl && !shift)) return false;
        var order = Ordered().ToList();
        if (shift)
        {
            var range = Picks.Range(order, pickAnchor ?? live, t);
            SetPicks(ctrl ? Picked().Concat(range) : range);
        }
        else
        {
            SetPicks(Picks.Toggle(order, Picked(), live, t));
            pickAnchor = t;
        }
        return true;
    }

    /// <summary>The menu for several tabs: the same words as for one, saying how many.</summary>
    void AddPickedItems(ContextMenu menu, List<Tab> many, Func<string, Action, bool, MenuItem> item)
    {
        var n = many.Count;
        var pages = many.Where(t => !t.IsBlank).ToList();
        menu.Items.Add(item($"{n} Tabs Selected", () => { }, false));
        menu.Items.Add(new Separator());
        if (SplitOn)
            menu.Items.Add(item($"Open {n} Tabs in Split View", () => SplitPicked(pages), pages.Count is >= 2 and <= TabSplit.Most && pages.Count == n));
        if (many.Any(t => !t.IsPin))
            menu.Items.Add(item("Pin Tabs", () => { foreach (var t in pages.Where(t => !t.IsPin)) Pin(t); ClearPicks(); SaveSoon(); }, pages.Any(t => !t.IsPin)));
        if (many.Any(t => t.IsPin))
            menu.Items.Add(item("Unpin Tabs", () => { foreach (var t in many.Where(t => t.IsPin).Reverse()) Unpin(t); ClearPicks(); SaveSoon(); }, true));
        var loose = pages.Where(t => !t.IsPin).ToList();
        menu.Items.Add(item("Add Tabs to New Group", () => GroupPicked(loose), loose.Count > 0));
        menu.Items.Add(item("Duplicate Tabs", () => { foreach (var t in pages) Duplicate(t); ClearPicks(); }, pages.Count > 0));
        menu.Items.Add(item("Copy Addresses", () => Clipboard.SetText(string.Join(Environment.NewLine, pages.Select(t => t.Url))), pages.Count > 0));
        var others = Shared.Book.In(profileId).Where(s => s.Id != space.Id).ToList();
        if (SpacesOn && others.Count > 0)
        {
            var move = new MenuItem { Header = "Move Tabs to Space" };
            foreach (var s in others)
            {
                var target = s;
                move.Items.Add(item(s.Name, () => { foreach (var t in many) { LeaveSplit(t, relayout: false); MoveToSpace(t, target); } ClearPicks(); ShowLive(); }, true));
            }
            menu.Items.Add(move);
        }
        menu.Items.Add(new Separator());
        menu.Items.Add(item($"Close {n} Tabs", () => { ClearPicks(); foreach (var t in many) Close(t); }, true));
    }

    /// <summary>The picked tabs, in the order they are in, side by side in one split: any split they were in comes apart first.</summary>
    void SplitPicked(List<Tab> pages)
    {
        if (!SplitOn || pages.Count is < 2 or > TabSplit.Most) return;
        foreach (var t in pages) if (SplitOf(t) is { } old) Splits.Remove(old);
        var sp = new TabSplit(pages.Select(t => t.Id));
        Splits.Add(sp);
        // Together in the column too, after the first of them.
        var loose = pages.Where(t => !t.IsPin).ToList();
        if (loose.Count > 1)
        {
            foreach (var t in loose.Skip(1)) tabs.Remove(t);
            var at = tabs.IndexOf(loose[0]);
            foreach (var t in loose.Skip(1)) tabs.Insert(++at, t);
        }
        ClearPicks();
        ClearChrome();
        App.Trace($"split picked {pages.Count}");
        Select(pages[0]);
    }

    void GroupPicked(List<Tab> loose)
    {
        if (loose.Count == 0) return;
        ClearPicks();
        var g = NewGroup(loose[0]);
        foreach (var t in loose.Skip(1)) AddToGroup(t, g);
        if (loose.Count > 1 && headings.TryGetValue(g.Id, out var h)) BeginRenamingHeading(h);
    }

    // MARK: - dragging several

    /// <summary>The tabs a drag carries: every picked tab when the one dragged is among several picked, else that tab alone.</summary>
    List<Tab> carrying = [];

    List<Tab> Carried(Tab t) => t.Picked && Picked() is { Count: > 1 } many ? many : [t];

    /// <summary>The tabs carried with T (not T itself), in the order they are in.</summary>
    IEnumerable<Tab> Others(Tab t) => carrying.Contains(t) ? carrying.Where(o => o != t) : [];

    /// <summary>After a drag that moved T within the list: the others carried with it close up round it, in the order they were in.</summary>
    void Gather(Tab t)
    {
        if (carrying.Count < 2 || !carrying.Contains(t)) return;
        var list = t.IsPin ? pins : tabs;
        var same = carrying.Where(o => o.IsPin == t.IsPin && list.Contains(o)).ToList();
        if (same.Count < 2) return;
        var at = same.IndexOf(t);
        foreach (var o in same.Where(o => o != t)) list.Remove(o);
        var here = list.IndexOf(t);
        // Those before it go just above it, in order; those after, just below.
        foreach (var o in same.Take(at)) list.Insert(here++, o);
        var below = here + 1;
        foreach (var o in same.Skip(at + 1)) list.Insert(Math.Min(below++, list.Count), o);
        App.Trace($"gathered {same.Count}");
    }
}
