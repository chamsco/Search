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
            // An empty page left beside another last time still offers the field.
            foreach (var id in ids) if (all.FirstOrDefault(x => x.Id == id) is { Url: "about:blank" } fresh) fresh.Fresh = true;
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
            FillBlank();
            if (live.View is { } v) Fill(v);
            return;
        }
        if (chromeFor != sp.Id || frames.Count != sp.Tabs.Count) BuildChrome(sp);
        foreach (var id in sp.Tabs) if (TabById(id) is { } t) { var v = Wake(t); v.Visibility = t.ShowsField ? Visibility.Hidden : Visibility.Visible; }
        PlaceSplit(sp);
    }

    /// <summary>The empty page of a split waiting for an address: the one whose place the new-tab field takes.</summary>
    Tab? FieldPane(TabSplit? sp) => sp?.Tabs.Select(TabById).FirstOrDefault(t => t is { ShowsField: true });

    /// <summary>The new-tab field fills the stage again (an empty tab on its own).</summary>
    void FillBlank()
    {
        Blank.HorizontalAlignment = HorizontalAlignment.Stretch;
        Blank.VerticalAlignment = VerticalAlignment.Stretch;
        Blank.Width = Blank.Height = double.NaN;
        Blank.Margin = new Thickness(0);
        if (live is { IsBlank: false }) Blank.Visibility = Visibility.Collapsed;
    }

    /// <summary>Split with New Tab: an empty page beside T, the new-tab field in its place, ready for an address or an open tab.</summary>
    void SplitWithNew(Tab t)
    {
        if (!SplitOn || t.IsBlank || t.ShowsField) return;
        if (SplitOf(t) is { } own && (!own.CanAdd || FieldPane(own) is not null)) return;
        if (t != live) Select(t);
        var beside = new Tab { Url = "about:blank", Title = "New Tab", Fresh = true, SpaceId = t.SpaceId };
        var at = tabs.IndexOf(t);
        tabs.Insert(at < 0 ? tabs.Count : at + 1, beside);
        if (Pair(t, beside) is null) { tabs.Remove(beside); return; }
        ClearChrome();
        App.Trace("split with new tab");
        Select(beside);
        // After the menu that asked for it has closed and given the focus back: the field is where typing goes.
        Dispatcher.BeginInvoke(() => { if (live == beside && beside.ShowsField) { BlankField.Focus(); Keyboard.Focus(BlankField); } }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
    }

    /// <summary>An open tab picked in the empty page's field: it takes that page's place in the split, and the empty page goes.</summary>
    bool BringIntoPane(Tab fresh, Tab open)
    {
        if (SplitOf(fresh) is not { } sp || open == fresh || sp.Holds(open.Id) || open.IsBlank) return false;
        if (SplitOf(open) is not null) LeaveSplit(open, relayout: false);
        if (!sp.Replace(fresh.Id, open.Id)) return false;
        if (!open.IsPin && !fresh.IsPin && tabs.Contains(fresh))
        {
            tabs.Remove(open);
            tabs.Insert(Math.Min(tabs.IndexOf(fresh) + 1, tabs.Count), open);
        }
        Drop(fresh);
        ClearChrome();
        App.Trace($"brought {open.Title} into the split");
        Select(open);
        SaveSoon();
        return true;
    }

    /// <summary>A tab gone without a trace (nothing to reopen): the empty page of a split.</summary>
    void Drop(Tab t)
    {
        tabs.Remove(t);
        if (t.View is not null) { Stage.Children.Remove(t.View); t.View.Dispose(); t.View = null; }
        if (t == live) { live.Live = false; live = null; }
    }

    /// <summary>Escape in an empty split page's field: the page goes, the one it was beside stays on its own.</summary>
    void CancelFieldPane(Tab fresh)
    {
        var sp = SplitOf(fresh);
        var back = sp?.Tabs.Where(id => id != fresh.Id).Select(TabById).FirstOrDefault(x => x is not null);
        LeaveSplit(fresh, relayout: false);
        Drop(fresh);
        ClearChrome();
        if (back is not null) Select(back); else if ((tabs.LastOrDefault() ?? pins.LastOrDefault()) is { } any) Select(any); else NewTab();
        SaveSoon();
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
        var hasField = false;
        for (var i = 0; i < rects.Count; i++)
        {
            var (x, y, rw, rh) = rects[i];
            x += SplitPad; y += SplitPad;
            if (TabById(sp.Tabs[i]) is { ShowsField: true })
            {
                Blank.HorizontalAlignment = HorizontalAlignment.Left;
                Blank.VerticalAlignment = VerticalAlignment.Top;
                Blank.Margin = new Thickness(x, y, 0, 0);
                Blank.Width = Math.Max(1, rw);
                Blank.Height = Math.Max(1, rh);
                Blank.Visibility = Visibility.Visible;
                hasField = true;
            }
            else if (TabById(sp.Tabs[i])?.View is { } v)
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
        if (!hasField) FillBlank();
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
            SplitWithNew(live);
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
        menu.Items.Add(item("Split with New Tab", () => SplitWithNew(t), !t.IsBlank && !t.ShowsField && (SplitOf(t) is not { } mine || (mine.CanAdd && FieldPane(mine) is null))));
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

    // MARK: - a tab let go on the page

    /// <summary>Where a tab dragged over the page will go among the split's pages; -1 while it is not over the page.</summary>
    int pageDropAt = -1;

    /// <summary>A tab dragged from the column or the strip can be let go on the page on screen to be shown beside it.</summary>
    bool CanDropOnPage(Tab moving)
    {
        var all = (carrying.Contains(moving) ? carrying : [moving]).Where(m => m != live).ToList();
        return SplitOn && live is { IsBlank: false } && all.Contains(moving) && all.All(m => !m.IsBlank && SplitOf(m) is null)
            && (SplitOf(live)?.Tabs.Count ?? 1) + all.Count <= TabSplit.Most;
    }

    /// <summary>The drag begins: a see-through sheet over the page, which shows the half the tab will take.</summary>
    void BeginPageDrop(Tab moving)
    {
        pageDropAt = -1;
        if (!CanDropOnPage(moving) || Card.ActualWidth < 60) return;
        PageDropPopup.Width = Card.ActualWidth;
        PageDropPopup.Height = Card.ActualHeight;
        PageDropZone.Visibility = Visibility.Collapsed;
        PageDropPopup.IsOpen = true;
        App.Trace("page drop ready");
    }

    /// <summary>Called as the drag goes (from the source's feedback, so it works whatever window is under the pointer).</summary>
    void TrackPageDrop()
    {
        if (!PageDropPopup.IsOpen || live is null) return;
        if (!Native.GetCursorPos(out var c)) return;
        var p = Stage.PointFromScreen(new Point(c.X, c.Y));
        var w = Stage.ActualWidth - 2 * SplitPad;
        var h = Stage.ActualHeight - 2 * SplitPad;
        if (p.X < 0 || p.Y < 0 || p.X > Stage.ActualWidth || p.Y > Stage.ActualHeight || w < 20 || h < 20)
        {
            if (pageDropAt >= 0) App.Trace("page drop off");
            pageDropAt = -1;
            PageDropZone.Visibility = Visibility.Collapsed;
            return;
        }
        var x = p.X - SplitPad; var y = p.Y - SplitPad;
        var sp = SplitOf(live) ?? new TabSplit([live.Id, Guid.NewGuid()]);
        var lone = SplitOf(live) is null;
        int at;
        (double X, double Y, double W, double H) zone;
        if (lone)
        {
            // One page on screen: its left or right half.
            at = x < w / 2 ? 0 : 1;
            zone = at == 0 ? (0, 0, w / 2, h) : (w / 2, 0, w / 2, h);
        }
        else
        {
            at = sp.DropIndex(x, y, w, h, SplitGap);
            var rects = sp.Rects(w, h, SplitGap);
            var horizontal = sp.Axis == SplitAxis.Horizontal;
            var (rx, ry, rw, rh) = at < rects.Count && (horizontal ? x : y) <= (horizontal ? rects[at].X + rects[at].W : rects[at].Y + rects[at].H) ? rects[at] : rects[at - 1];
            var first = at < rects.Count && (rx, ry, rw, rh) == rects[at];
            zone = horizontal ? (first ? rx : rx + rw / 2, ry, rw / 2, rh) : (rx, first ? ry : ry + rh / 2, rw, rh / 2);
        }
        if (at != pageDropAt) App.Trace($"page drop over {at}");
        pageDropAt = at;
        var inCard = Stage.TranslatePoint(new Point(0, 0), Card);
        PageDropZone.Margin = new Thickness(inCard.X + SplitPad + zone.X + 4, inCard.Y + SplitPad + zone.Y + 4, 0, 0);
        PageDropZone.Width = Math.Max(1, zone.W - 8);
        PageDropZone.Height = Math.Max(1, zone.H - 8);
        PageDropZone.Visibility = Visibility.Visible;
    }

    /// <summary>The drag is over: let go over the page (and not called off), the tab is shown beside it, on that side.</summary>
    bool EndPageDrop(Tab moving, bool cancelled)
    {
        var at = pageDropAt;
        pageDropAt = -1;
        PageDropZone.Visibility = Visibility.Collapsed;
        if (!PageDropPopup.IsOpen) return false;
        PageDropPopup.IsOpen = false;
        if (cancelled || at < 0 || live is null || !CanDropOnPage(moving)) return false;
        var anchor = live;
        var all = (carrying.Contains(moving) ? carrying : [moving]).Where(m => m != anchor).ToList();
        if (Pair(anchor, all[0]) is not { } sp) return false;
        foreach (var m in all.Skip(1)) Pair(anchor, m);
        // The carried pages go together, in their order, at the side they were let go.
        foreach (var m in all) sp.Tabs.Remove(m.Id);
        for (var k = 0; k < all.Count; k++) sp.Insert(all[k].Id, at + k);
        ClearChrome();
        App.Trace($"split by page drop {moving.Title} at {at} of {sp.Tabs.Count}");
        Select(moving);
        SaveSoon();
        return true;
    }

    void PageDrop_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(typeof(Tab)) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    void PageDrop_Drop(object sender, DragEventArgs e) { e.Effects = DragDropEffects.Move; e.Handled = true; }
}
