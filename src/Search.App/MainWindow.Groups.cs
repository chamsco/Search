using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Search.Core;

namespace Search.App;

/// <summary>
/// Tab groups (TabGroups.swift) and folders among the pinned rows (PinFolders.swift). The tabs and pins stay plain lists;
/// what the column draws is a list of lines made from them: a heading where a group or folder begins, then its members,
/// or nothing of them while it is folded.
/// </summary>
public partial class MainWindow
{
    readonly Dictionary<Guid, RowHeading> headings = [];
    bool rebuilding;
    bool rowsMode;

    List<TabGroup> Groups => StateOf(space).Groups;
    List<PinFolder> Folders => StateOf(space).Folders;
    bool FoldersShown => Shared.Prefs.Data.PinsAsRows;

    // MARK: - the lines of the column

    void RowsChanged(SpaceState st, NotifyCollectionChangedEventArgs e, bool pins)
    {
        if (restoring || rebuilding) return;
        // A tab set down inside a group joins it.
        if (e.Action == NotifyCollectionChangedAction.Add && !pins && e.NewStartingIndex >= 0 && e.NewItems is { Count: 1 } && e.NewItems[0] is Tab added && added.GroupId is null)
        {
            var i = e.NewStartingIndex;
            var before = i > 0 ? st.Tabs[i - 1].GroupId : null;
            var after = i + 1 < st.Tabs.Count ? st.Tabs[i + 1].GroupId : null;
            added.GroupId = Search.Core.Groups.Adopt(before, after, null);
        }
        RebuildRows(st);
    }

    RowHeading HeadingFor(Guid id, bool folder)
    {
        if (!headings.TryGetValue(id, out var h)) headings[id] = h = new RowHeading(id, folder);
        return h;
    }

    void RebuildRows(SpaceState st)
    {
        if (rebuilding) return;
        rebuilding = true;
        try
        {
            var liveHere = st.Space.Id == space.Id ? live : st.Live;

            // Groups: every one a single run, none empty.
            var norm = Search.Core.Groups.Normalize(st.Tabs.Select(t => t.GroupId).ToList(), st.Groups.Select(g => g.Id).ToHashSet());
            for (var i = 0; i < st.Tabs.Count; i++) if (st.Tabs[i].GroupId != norm[i]) st.Tabs[i].GroupId = norm[i];
            var kept = Search.Core.Groups.Kept(st.Groups, norm);
            if (kept.Count != st.Groups.Count) { st.Groups.Clear(); st.Groups.AddRange(kept); }

            var rows = new List<object>();
            Guid? last = null;
            foreach (var t in st.Tabs)
            {
                var g = t.GroupId is Guid gid ? st.Groups.First(x => x.Id == gid) : null;
                if (g?.Id != last)
                {
                    if (g is not null)
                    {
                        var h = HeadingFor(g.Id, false);
                        h.Name = g.Name; h.Collapsed = g.Collapsed; h.Colour = g.Colour; h.Depth = 0;
                        h.Count = st.Tabs.Count(x => x.GroupId == g.Id);
                        rows.Add(h);
                    }
                    last = g?.Id;
                }
                t.Indent = new Thickness(g is null ? 0 : 10, 0, 0, 2);
                if (g is { Collapsed: true } && t != liveHere) continue;
                rows.Add(t);
            }
            Sync(st.Rows, rows);

            // Folders among the pinned rows.
            var clean = PinTree.Sanitized(st.Folders);
            if (clean.Count != st.Folders.Count || !clean.SequenceEqual(st.Folders)) { st.Folders.Clear(); st.Folders.AddRange(clean); }
            var ids = st.Folders.Select(f => f.Id).ToHashSet();
            foreach (var p in st.Pins) if (p.FolderId is Guid f && !ids.Contains(f)) p.FolderId = null;
            var pinRows = new List<object>();
            if (FoldersShown)
            {
                foreach (var line in PinTree.Lines(st.Folders, st.Pins.Select(p => (p.Id, p.FolderId)).ToList()))
                {
                    if (line.Folder is { } f)
                    {
                        var h = HeadingFor(f.Id, true);
                        h.Name = f.Name; h.Collapsed = f.Collapsed; h.Depth = line.Depth; h.Colour = null;
                        h.Count = st.Pins.Count(p => p.FolderId == f.Id);
                        pinRows.Add(h);
                    }
                    else if (st.Pins.FirstOrDefault(p => p.Id == line.Pin) is { } pin)
                    {
                        pin.Indent = new Thickness(line.Depth * 12, 0, 0, 2);
                        pinRows.Add(pin);
                    }
                }
            }
            else
            {
                foreach (var p in st.Pins) { p.Indent = new Thickness(0, 0, 0, 2); pinRows.Add(p); }
            }
            Sync(st.PinRows, pinRows);
        }
        finally { rebuilding = false; }
    }

    /// <summary>Make ROWS read as DESIRED with as few moves as it takes, so a row being dragged keeps its place in the tree.</summary>
    static void Sync(ObservableCollection<object> rows, List<object> desired)
    {
        for (var i = 0; i < desired.Count; i++)
        {
            if (i < rows.Count && ReferenceEquals(rows[i], desired[i])) continue;
            var j = -1;
            for (var k = i + 1; k < rows.Count; k++) if (ReferenceEquals(rows[k], desired[i])) { j = k; break; }
            if (j >= 0) rows.Move(j, i); else rows.Insert(i, desired[i]);
        }
        while (rows.Count > desired.Count) rows.RemoveAt(rows.Count - 1);
    }

    /// <summary>The column's lists read the lines of this space.</summary>
    void SetRowSources(SpaceState st)
    {
        var tabRow = (DataTemplate)FindResource("TabRow");
        var chip = (DataTemplate)FindResource("TabChip");
        var tile = (DataTemplate)FindResource("PinTile");
        var headRow = (DataTemplate)FindResource("HeadingRow");
        var headChip = (DataTemplate)FindResource("HeadingChip");
        TabList.ItemTemplate = null; TabList.ItemTemplateSelector = new RowSelector(tabRow, headRow);
        TopTabs.ItemTemplate = null; TopTabs.ItemTemplateSelector = new RowSelector(chip, headChip);
        TabList.ItemsSource = st.Rows;
        TopTabs.ItemsSource = st.Rows;
        TopPins.ItemsSource = pins;
        var rows = FoldersShown;
        rowsMode = rows;
        PinGrid.ItemTemplate = null;
        PinGrid.ItemTemplateSelector = new RowSelector(rows ? tabRow : tile, headRow);
        PinGrid.ItemsPanel = rows ? new ItemsPanelTemplate(new FrameworkElementFactory(typeof(StackPanel))) : (ItemsPanelTemplate)PinGridPanel;
        PinGrid.ItemsSource = rows ? st.PinRows : pins;
    }

    // MARK: - headings

    void Heading_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || e.ClickCount != 1 || (sender as FrameworkElement)?.DataContext is not RowHeading h) return;
        Fold(h);
        e.Handled = true;
    }

    void Fold(RowHeading h)
    {
        var st = StateOf(space);
        if (h.IsFolder) { if (st.Folders.FirstOrDefault(f => f.Id == h.Id) is { } f) f.Collapsed = !f.Collapsed; }
        else if (st.Groups.FirstOrDefault(g => g.Id == h.Id) is { } g) g.Collapsed = !g.Collapsed;
        RebuildRows(st);
        SaveSoon();
    }

    FrameworkElement? HeadingContainer(RowHeading h) =>
        (h.IsFolder ? PinGrid : TopLayout ? TopTabs : TabList).ItemContainerGenerator.ContainerFromItem(h) as FrameworkElement;

    void BeginRenamingHeading(RowHeading h)
    {
        EndEditing(false);
        h.Editing = true;
        Dispatcher.BeginInvoke(() =>
        {
            var field = HeadingContainer(h) is { } c ? FindChild<TextBox>(c, "HeadField") : null;
            if (field is null) { h.Editing = false; return; }
            field.Text = h.Name;
            field.Focus();
            Keyboard.Focus(field);
            field.SelectAll();
        }, DispatcherPriority.Loaded);
    }

    void HeadField_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox { DataContext: RowHeading h } box) return;
        if (e.Key == Key.Enter) { CommitHeading(h, box.Text); e.Handled = true; Dispatcher.BeginInvoke(() => (live?.View as UIElement)?.Focus()); }
        else if (e.Key == Key.Escape) { h.Editing = false; e.Handled = true; }
    }

    void HeadField_LostFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is TextBox { DataContext: RowHeading { Editing: true } h } box) CommitHeading(h, box.Text);
    }

    void CommitHeading(RowHeading h, string text)
    {
        h.Editing = false;
        text = text.Trim();
        if (text.Length == 0) return;
        var st = StateOf(space);
        if (h.IsFolder) { if (st.Folders.FirstOrDefault(f => f.Id == h.Id) is { } f) f.Name = text; }
        else if (st.Groups.FirstOrDefault(g => g.Id == h.Id) is { } g) g.Name = text;
        h.Name = text;
        SaveSoon();
    }

    void Heading_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: RowHeading h, ContextMenu: { } menu }) return;
        menu.Items.Clear();
        MenuItem Item(string header, Action act, bool enabled = true) { var m = new MenuItem { Header = header, IsEnabled = enabled }; m.Click += (_, _) => act(); return m; }
        var st = StateOf(space);
        menu.Items.Add(Item(h.Collapsed ? "Expand" : "Collapse", () => Fold(h)));
        menu.Items.Add(Item(h.IsFolder ? "Rename Folder" : "Rename Group", () => BeginRenamingHeading(h)));
        if (!h.IsFolder)
        {
            var colour = new MenuItem { Header = "Colour" };
            for (var i = 0; i < Spaces.ColourNames.Length; i++)
            {
                var index = i;
                var item = Item(Spaces.ColourNames[i], () => { if (st.Groups.FirstOrDefault(g => g.Id == h.Id) is { } g) { g.Colour = index == 0 ? null : index; RebuildRows(st); SaveSoon(); } });
                item.IsChecked = (st.Groups.FirstOrDefault(g => g.Id == h.Id)?.Colour ?? 0) == i;
                colour.Items.Add(item);
            }
            menu.Items.Add(colour);
            menu.Items.Add(new Separator());
            menu.Items.Add(Item("Ungroup", () => Ungroup(h.Id)));
            menu.Items.Add(Item("Close Group", () => { foreach (var t in st.Tabs.Where(t => t.GroupId == h.Id).ToList()) Close(t); }));
        }
        else
        {
            menu.Items.Add(Item("New Folder Inside", () => NewFolder(h.Id), PinTree.Depth(h.Id, st.Folders) < PinTree.MaxDepth));
            menu.Items.Add(new Separator());
            menu.Items.Add(Item("Delete Folder", () => DeleteFolder(h.Id)));
        }
    }

    // MARK: - tab groups

    TabGroup NewGroup(Tab t)
    {
        var st = StateOf(space);
        var g = new TabGroup { Name = "New Group" };
        st.Groups.Add(g);
        t.GroupId = g.Id;
        RebuildRows(st);
        SaveSoon();
        if (headings.TryGetValue(g.Id, out var h)) BeginRenamingHeading(h);
        return g;
    }

    /// <summary>The tab goes to the end of the group's run, and is in it.</summary>
    void AddToGroup(Tab t, TabGroup g)
    {
        if (t.IsPin || t.GroupId == g.Id) return;
        var st = StateOf(space);
        var from = tabs.IndexOf(t);
        if (from < 0) return;
        t.GroupId = null;
        var end = tabs.Select((x, i) => (x, i)).Where(p => p.x.GroupId == g.Id).Select(p => p.i).DefaultIfEmpty(-1).Max();
        var to = end < 0 ? from : (from <= end ? end : end + 1);
        if (to != from) { rebuilding = true; tabs.Move(from, Math.Min(to, tabs.Count - 1)); rebuilding = false; }
        t.GroupId = g.Id;
        RebuildRows(st);
        SaveSoon();
    }

    /// <summary>Out of its group, to just after it.</summary>
    void RemoveFromGroup(Tab t)
    {
        if (t.GroupId is not Guid id) return;
        var st = StateOf(space);
        var from = tabs.IndexOf(t);
        t.GroupId = null;
        var end = tabs.Select((x, i) => (x, i)).Where(p => p.x.GroupId == id).Select(p => p.i).DefaultIfEmpty(from).Max();
        if (end > from) { rebuilding = true; tabs.Move(from, end); rebuilding = false; }
        RebuildRows(st);
        SaveSoon();
    }

    void Ungroup(Guid id)
    {
        foreach (var t in tabs.Where(t => t.GroupId == id)) t.GroupId = null;
        RebuildRows(StateOf(space));
        SaveSoon();
    }

    // MARK: - folders among the pins

    PinFolder NewFolder(Guid? parent, Tab? holding = null)
    {
        var st = StateOf(space);
        var f = new PinFolder { Name = "New Folder", Parent = parent };
        st.Folders.Add(f);
        if (holding is not null) holding.FolderId = f.Id;
        RebuildRows(st);
        SaveSoon();
        if (headings.TryGetValue(f.Id, out var h)) BeginRenamingHeading(h);
        return f;
    }

    /// <summary>The folder goes; what it held goes up into the folder above it.</summary>
    void DeleteFolder(Guid id)
    {
        var st = StateOf(space);
        var parent = st.Folders.FirstOrDefault(f => f.Id == id)?.Parent;
        foreach (var f in st.Folders.Where(f => f.Parent == id)) f.Parent = parent;
        foreach (var p in st.Pins.Where(p => p.FolderId == id)) p.FolderId = parent;
        st.Folders.RemoveAll(f => f.Id == id);
        RebuildRows(st);
        SaveSoon();
    }

    // MARK: - menus

    void AddGroupItems(ContextMenu menu, Tab t, Func<string, Action, bool, MenuItem> item)
    {
        var st = StateOf(space);
        if (t.IsPin)
        {
            if (!FoldersShown) return;
            var move = new MenuItem { Header = "Move to Folder" };
            move.Items.Add(item("New Folder", () => NewFolder(null, t), true));
            foreach (var f in st.Folders)
            {
                var target = f;
                var sub = item(new string(' ', (PinTree.Depth(f.Id, st.Folders) - 1) * 2) + f.Name, () => { t.FolderId = target.Id; RebuildRows(st); SaveSoon(); }, true);
                sub.IsChecked = t.FolderId == f.Id;
                move.Items.Add(sub);
            }
            if (t.FolderId is not null) { move.Items.Add(new Separator()); move.Items.Add(item("Out of Folder", () => { t.FolderId = null; RebuildRows(st); SaveSoon(); }, true)); }
            menu.Items.Add(move);
            return;
        }
        if (t.IsBlank) return;
        var add = new MenuItem { Header = "Add to Group" };
        add.Items.Add(item("New Group", () => NewGroup(t), true));
        foreach (var g in st.Groups.Where(g => g.Id != t.GroupId))
        {
            var target = g;
            add.Items.Add(item(string.IsNullOrWhiteSpace(g.Name) ? "Untitled Group" : g.Name, () => AddToGroup(t, target), true));
        }
        menu.Items.Add(add);
        if (t.GroupId is not null) menu.Items.Add(item("Remove from Group", () => RemoveFromGroup(t), true));
    }

    // MARK: - dropping

    void Heading_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = dragging is { IsBlank: false } ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    void Heading_Drop(object sender, DragEventArgs e)
    {
        e.Handled = true;
        if (e.Data.GetData(typeof(Tab)) is not Tab moving || (sender as FrameworkElement)?.DataContext is not RowHeading h || moving.IsBlank) return;
        var st = StateOf(space);
        if (h.IsFolder)
        {
            if (!moving.IsPin) Pin(moving);
            moving.FolderId = h.Id;
            RebuildRows(st);
            SaveSoon();
        }
        else if (st.Groups.FirstOrDefault(g => g.Id == h.Id) is { } g && !moving.IsPin) AddToGroup(moving, g);
    }

    /// <summary>A row let go after being dragged: it is in the group (or the folder) it came to rest in.</summary>
    void Landed(Tab t)
    {
        var st = StateOf(space);
        if (t.IsPin || !tabs.Contains(t)) return;
        var i = tabs.IndexOf(t);
        var before = i > 0 ? tabs[i - 1].GroupId : null;
        var after = i + 1 < tabs.Count ? tabs[i + 1].GroupId : null;
        t.GroupId = Search.Core.Groups.Adopt(before, after, t.GroupId);
        RebuildRows(st);
    }
}
