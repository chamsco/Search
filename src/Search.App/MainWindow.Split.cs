using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Web.WebView2.Wpf;
using Search.Core;

namespace Search.App;

/// <summary>
/// Split View (Split.swift): two to four pages side by side or one above the other, sharing one place in the row.
/// The pages are web views placed in the stage; lines between them are dragged to share the room.
/// </summary>
public partial class MainWindow
{
    const double SplitPad = 3, SplitGap = 10;

    readonly List<Border> frames = [];
    readonly List<Thumb> lines = [];
    Guid chromeFor;

    bool SplitOn => Shared.Prefs.Data.SplitView;
    List<TabSplit> Splits => StateOf(space).Splits;
    TabSplit? SplitOf(Tab t) => SplitOn ? Splits.FirstOrDefault(s => s.Holds(t.Id)) : null;
    Tab? TabById(Guid id) => Ordered().FirstOrDefault(t => t.Id == id);

    void RestoreSplits(SpaceState st, SessionShape shape)
    {
        var all = st.Pins.Concat(st.Tabs).ToList();
        foreach (var e in shape.Splits)
        {
            var ids = e.Tabs.Where(i => i >= 0 && i < all.Count && !all[i].IsBlank).Select(i => all[i].Id).Distinct().ToList();
            if (ids.Count < 2 || ids.Any(id => st.Splits.Any(x => x.Holds(id)))) continue;
            var sp = new TabSplit(ids, e.Axis, e.Sizes.Count == ids.Count ? e.Sizes : null);
            if (e.Focused >= 0 && e.Focused < e.Tabs.Count && e.Tabs[e.Focused] is var fi && fi >= 0 && fi < all.Count) sp.Focused = all[fi].Id;
            st.Splits.Add(sp);
        }
    }

    // MARK: - the stage

    /// <summary>Put the live page, or the live split's pages, where they belong in the stage.</summary>
    void LayoutStage()
    {
        var order = Ordered().ToList();
        for (var i = 0; i < order.Count; i++)
        {
            var t = order[i];
            var own = SplitOf(t);
            t.InSplit = own is not null;
            // Split pages sit in one box in the column: a first, any middle, a last.
            var before = i > 0 && !t.IsPin && !order[i - 1].IsPin && own is not null && SplitOf(order[i - 1]) == own;
            var after = i < order.Count - 1 && !t.IsPin && !order[i + 1].IsPin && own is not null && SplitOf(order[i + 1]) == own;
            t.SplitPos = own is null || (!before && !after) ? 0 : !before ? 1 : after ? 2 : 3;
        }
        if (live is null) return;
        var sp = SplitOf(live);
        if (sp is null || Stage.ActualWidth < 60 || Stage.ActualHeight < 60)
        {
            ClearChrome();
            if (live.View is { } v) Fill(v);
            return;
        }
        if (chromeFor != sp.Id || frames.Count != sp.Tabs.Count) BuildChrome(sp);
        foreach (var id in sp.Tabs) if (TabById(id) is { } t) { var v = Wake(t); v.Visibility = Visibility.Visible; }
        PlaceSplit(sp);
    }

    static void Fill(WebView2 v)
    {
        v.HorizontalAlignment = HorizontalAlignment.Stretch;
        v.VerticalAlignment = VerticalAlignment.Stretch;
        v.Width = v.Height = double.NaN;
        v.Margin = new Thickness(0);
    }

    void ClearChrome()
    {
        foreach (var f in frames) Stage.Children.Remove(f);
        foreach (var l in lines) Stage.Children.Remove(l);
        frames.Clear();
        lines.Clear();
        chromeFor = Guid.Empty;
    }

    void BuildChrome(TabSplit sp)
    {
        ClearChrome();
        chromeFor = sp.Id;
        for (var i = 0; i < sp.Tabs.Count; i++)
        {
            var f = new Border { CornerRadius = new CornerRadius(Card.CornerRadius.TopLeft + 3), BorderThickness = new Thickness(2), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, IsHitTestVisible = false };
            frames.Add(f);
            Stage.Children.Insert(0, f);
        }
        for (var i = 0; i < sp.Tabs.Count - 1; i++)
        {
            var index = i;
            var l = new Thumb { Background = Brushes.Transparent, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Template = LineTemplate() };
            System.Windows.Automation.AutomationProperties.SetAutomationId(l, "SplitLine" + i);
            l.Cursor = sp.Axis == SplitAxis.Horizontal ? Cursors.SizeWE : Cursors.SizeNS;
            l.DragDelta += (_, e) =>
            {
                var room = (sp.Axis == SplitAxis.Horizontal ? Stage.ActualWidth : Stage.ActualHeight) - 2 * SplitPad - SplitGap * (sp.Tabs.Count - 1);
                if (room <= 0) return;
                sp.Resize(index, (sp.Axis == SplitAxis.Horizontal ? e.HorizontalChange : e.VerticalChange) / room);
                PlaceSplit(sp);
            };
            l.DragCompleted += (_, _) => SaveSoon();
            l.MouseDoubleClick += (_, _) => { sp.Sizes.Clear(); sp.Sizes.AddRange(TabSplit.Even(sp.Tabs.Count)); PlaceSplit(sp); SaveSoon(); };
            lines.Add(l);
            Stage.Children.Add(l);
        }
    }

    static ControlTemplate LineTemplate()
    {
        var t = new ControlTemplate(typeof(Thumb));
        var b = new FrameworkElementFactory(typeof(Border));
        b.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        t.VisualTree = b;
        return t;
    }

    void PlaceSplit(TabSplit sp)
    {
        var scale = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 ?? 1;
        var w = Stage.ActualWidth - 2 * SplitPad;
        var h = Stage.ActualHeight - 2 * SplitPad;
        var rects = sp.Rects(w, h, SplitGap);
        var focused = sp.Focused ?? live?.Id;
        var accent = (Brush)FindResource("Ink");
        var quiet = (Brush)FindResource("Row.Wash");
        for (var i = 0; i < rects.Count; i++)
        {
            var (x, y, rw, rh) = rects[i];
            x += SplitPad; y += SplitPad;
            if (TabById(sp.Tabs[i])?.View is { } v)
            {
                v.HorizontalAlignment = HorizontalAlignment.Left;
                v.VerticalAlignment = VerticalAlignment.Top;
                v.Margin = new Thickness(x, y, 0, 0);
                v.Width = Math.Max(1, rw);
                v.Height = Math.Max(1, rh);
                if (v.Handle != IntPtr.Zero) Native.Round(v.Handle, (int)(rw * scale), (int)(rh * scale), (int)(Card.CornerRadius.TopLeft * scale));
            }
            if (i < frames.Count)
            {
                var f = frames[i];
                f.Margin = new Thickness(x - 2, y - 2, 0, 0);
                f.Width = rw + 4;
                f.Height = rh + 4;
                f.BorderBrush = Brushes.Transparent; // no ring round the focused page: its place shows in the column
            }
            if (i < lines.Count)
            {
                var l = lines[i];
                if (sp.Axis == SplitAxis.Horizontal) { l.Margin = new Thickness(x + rw, y, 0, 0); l.Width = SplitGap; l.Height = rh; }
                else { l.Margin = new Thickness(x, y + rh, 0, 0); l.Width = rw; l.Height = SplitGap; }
            }
        }
    }

    // MARK: - making and unmaking splits

    TabSplit? Pair(Tab left, Tab right)
    {
        if (!SplitOn || left == right || left.IsBlank || right.IsBlank) return null;
        if (SplitOf(right) is not null) return null;
        var sp = SplitOf(left);
        if (sp is null)
        {
            sp = new TabSplit([left.Id, right.Id]);
            Splits.Add(sp);
        }
        else if (!sp.Add(right.Id)) return null;
        // Side by side in the column too, so the row reads as one place.
        if (!left.IsPin && !right.IsPin)
        {
            var last = sp.Tabs.Select(id => tabs.IndexOf(TabById(id)!)).Where(i => i >= 0).Max();
            var from = tabs.IndexOf(right);
            if (from >= 0 && from != last + 1 && from != last) tabs.Move(from, from < last ? last : last + 1);
        }
        return sp;
    }

    /// <summary>A tab's menu › Open in Split View: that tab beside the page on screen; asked of the page itself, an empty page beside it.</summary>
    void OpenInSplit(Tab t)
    {
        if (!SplitOn || live is null || t.IsBlank && t != live) return;
        if (t == live || live.IsBlank)
        {
            if (live.IsBlank) return;
            if (SplitOf(live) is { CanAdd: false }) return;
            var beside = new Tab { Url = "about:blank", Title = "New Tab" };
            var at = tabs.IndexOf(live);
            tabs.Insert(at < 0 ? tabs.Count : at + 1, beside);
            if (Pair(live, beside) is null) { tabs.Remove(beside); return; }
            Select(beside);
            BeginEditing();
            return;
        }
        if (SplitOf(live)?.Holds(t.Id) == true) { Select(t); return; }
        if (Pair(live, t) is null) return;
        Select(t);
    }

    void SplitKey()
    {
        if (live is null || live.IsBlank) return;
        if (SplitOf(live) is not null) Separate(live); else OpenInSplit(live);
    }

    void FocusPane(int by)
    {
        if (live is null || SplitOf(live) is not { } sp) return;
        var at = sp.Tabs.IndexOf(live.Id) + by;
        if (at < 0 || at >= sp.Tabs.Count) return;
        if (TabById(sp.Tabs[at]) is { } t) Select(t);
    }

    void SwapPages(Tab t)
    {
        if (SplitOf(t) is not { } sp) return;
        sp.Tabs.Reverse();
        var sizes = sp.Sizes.AsEnumerable().Reverse().ToList();
        sp.Sizes.Clear(); sp.Sizes.AddRange(sizes);
        BuildChrome(sp);
        if (live is not null) ShowLive();
        SaveSoon();
    }

    void Turn(Tab t)
    {
        if (SplitOf(t) is not { } sp) return;
        sp.Axis = sp.Axis == SplitAxis.Horizontal ? SplitAxis.Vertical : SplitAxis.Horizontal;
        BuildChrome(sp);
        if (live is not null) ShowLive();
        SaveSoon();
    }

    /// <summary>This page alone out of the split; the others stay together.</summary>
    void LeaveSplit(Tab t, bool relayout = true)
    {
        var all = StateOf(StateOfTab(t)).Splits;
        if (all.FirstOrDefault(s => s.Holds(t.Id)) is not { } sp) return;
        if (!sp.Remove(t.Id)) { foreach (var id in sp.Tabs) if (TabById(id) is { } o) o.InSplit = false; all.Remove(sp); }
        t.InSplit = false;
        if (relayout && live is not null) { ShowLive(); }
        SaveSoon();
    }

    Space StateOfTab(Tab t) => Shared.Book.Spaces.FirstOrDefault(x => x.Id == t.SpaceId) ?? space;

    /// <summary>The whole split comes apart; every page stays as a tab of its own.</summary>
    void Separate(Tab t)
    {
        if (SplitOf(t) is not { } sp) return;
        Splits.Remove(sp);
        ClearChrome();
        if (live is not null) ShowLive();
        SaveSoon();
    }

    void CloseSplit(Tab t)
    {
        if (SplitOf(t) is not { } sp) return;
        foreach (var id in sp.Tabs.ToList()) if (TabById(id) is { } o) Close(o);
    }

    void AddSplitItems(ContextMenu menu, Tab t, Func<string, Action, bool, MenuItem> item)
    {
        if (!SplitOn) return;
        menu.Items.Add(new Separator());
        if (SplitOf(t) is { } sp)
        {
            if (sp.Tabs.Count == 2) menu.Items.Add(item("Swap Pages", () => SwapPages(t), true));
            menu.Items.Add(item(sp.Axis == SplitAxis.Horizontal ? "Stack Pages Vertically" : "Put Pages Side by Side", () => Turn(t), true));
            if (sp.Tabs.Count > 2) menu.Items.Add(item("Remove from Split View", () => LeaveSplit(t), true));
            menu.Items.Add(item("Separate Split Tabs", () => Separate(t), true));
            menu.Items.Add(item(sp.Tabs.Count > 2 ? "Close All Pages" : "Close Both Pages", () => CloseSplit(t), true));
        }
        else
        {
            var adding = live is not null && SplitOf(live) is not null && live != t;
            var can = !t.IsBlank && (t == live ? !(live is null) : live is not null && !live.IsBlank && (SplitOf(live) is null || SplitOf(live)!.CanAdd));
            menu.Items.Add(item(adding ? "Add to Split View" : "Open in Split View", () => OpenInSplit(t), can));
        }
    }
}
