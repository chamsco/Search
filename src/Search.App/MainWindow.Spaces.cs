using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Search.Core;

namespace Search.App;

/// <summary>
/// Spaces and profiles (Spaces.swift, Profiles.swift): separate rows of tabs in the one window, each with
/// sign-ins of its own if you like; a profile is a window with its own history, bookmarks and spaces.
/// Switching parks the row you leave, still playing, and brings the other's back.
/// </summary>
public partial class MainWindow
{
    sealed class SpaceState
    {
        public required Space Space { get; init; }
        public ObservableCollection<Tab> Pins { get; } = [];
        public ObservableCollection<Tab> Tabs { get; } = [];
        public Tab? Live { get; set; }
        public bool Restored { get; set; }
    }

    readonly Dictionary<Guid, SpaceState> states = [];
    Space space = null!;

    SpaceState StateOf(Space s)
    {
        if (states.TryGetValue(s.Id, out var st)) return st;
        st = new SpaceState { Space = s };
        void Mark(object? _, NotifyCollectionChangedEventArgs e) { foreach (Tab t in e.NewItems ?? Array.Empty<Tab>()) t.SpaceId = s.Id; }
        st.Pins.CollectionChanged += Mark;
        st.Tabs.CollectionChanged += Mark;
        return states[s.Id] = st;
    }

    bool SpacesOn => Shared.Prefs.Data.SpacesOn;

    void InitSpaces()
    {
        space = Shared.Book.HomeOf(profileId);
        var st = StateOf(space);
        st.Restored = true;
        pins = st.Pins;
        tabs = st.Tabs;
        BindLists();
        Shared.Book.Changed += OnSpacesChanged;
        Closed += (_, _) => Shared.Book.Changed -= OnSpacesChanged;
    }

    string? StoreNameOf(Tab t)
    {
        var s = Shared.Book.Spaces.FirstOrDefault(x => x.Id == t.SpaceId) ?? space;
        return SpaceBook.StoreName(s);
    }

    void BindLists()
    {
        PinGrid.ItemsSource = pins;
        TabList.ItemsSource = tabs;
        TopPins.ItemsSource = pins;
        TopTabs.ItemsSource = tabs;
    }

    // MARK: - saving

    void SaveSpace(Space s, IEnumerable<Tab> spacePins, IEnumerable<Tab> spaceTabs, Tab? liveTab)
    {
        if (!persistent) return;
        var all = spacePins.Concat(spaceTabs).ToList();
        Session.Save(new SessionShape
        {
            Tabs = all.Select(t => t.ToEntry()).ToList(),
            Active = liveTab is null ? 0 : Math.Max(0, all.IndexOf(liveTab)),
            SideWidth = SideColumn.Width.Value,
        }, s.Id);
    }

    void SaveAllSpaces()
    {
        foreach (var st in states.Values)
            if (st.Space.Id == space.Id) SaveSpace(space, pins, tabs, live);
            else if (st.Restored) SaveSpace(st.Space, st.Pins, st.Tabs, st.Live);
    }

    // MARK: - switching

    void SwitchSpace(Space target)
    {
        if (target.Id == space.Id || !Shared.Book.Spaces.Contains(target)) return;
        EndEditing(commit: false);
        CloseFind();
        MarkPopup.IsOpen = false;
        var from = StateOf(space);
        from.Live = live;
        SaveSpace(space, pins, tabs, live);

        space = target;
        var st = StateOf(target);
        pins = st.Pins;
        tabs = st.Tabs;
        BindLists();

        if (!st.Restored)
        {
            st.Restored = true;
            restoring = true;
            var shape = persistent ? Session.Load(target.Id) : null;
            foreach (var e in shape?.Tabs ?? [])
            {
                var t = Tab.From(e);
                t.Icon = CachedIcon(t.Url);
                (t.IsPin ? pins : tabs).Add(t);
            }
            restoring = false;
            if (shape is not null && Ordered().ElementAtOrDefault(shape.Active) is { } was) st.Live = was;
        }

        var pick = st.Live is { } l && Ordered().Contains(l) ? l : Ordered().FirstOrDefault();
        if (pick is null) NewTab(); else Select(pick);
        ApplyWash();
        RebuildStrip();
        SaveSoon();
    }

    void SwitchSpaceBy(int index)
    {
        if (!SpacesOn) return;
        var mine = Shared.Book.In(profileId).ToList();
        if (index >= 0 && index < mine.Count) SwitchSpace(mine[index]);
    }

    /// <summary>A space or a profile was changed (Settings, another window): the strip follows, and a space that is gone is left.</summary>
    void OnSpacesChanged()
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (!Shared.Book.Profiles.Any(p => p.Id == profileId) && profileId != Spaces.PersonalId) { Close(); return; }
            foreach (var gone in states.Keys.Where(id => Shared.Book.Spaces.All(s => s.Id != id)).ToList())
            {
                var st = states[gone];
                if (gone == space.Id) SwitchSpace(Shared.Book.HomeOf(profileId));
                foreach (var t in st.Pins.Concat(st.Tabs)) { if (t.View is { } v) { Stage.Children.Remove(v); v.Dispose(); t.View = null; } }
                states.Remove(gone);
                try { File.Delete(Store.File(Session.FileFor(gone))); } catch { }
            }
            if (Shared.Book.Spaces.FirstOrDefault(s => s.Id == space.Id) is { } fresh && !ReferenceEquals(fresh, space)) space = fresh;
            ApplyWash();
            RebuildStrip();
            PostToPages(SettingsMessage(), "settings");
        });
    }

    // MARK: - the strip

    void RebuildStrip()
    {
        SpaceStrip.Children.Clear();
        TopSpaceStrip.Children.Clear();
        ProfileDoor.Visibility = TopProfileDoor.Visibility = Shared.Book.Profiles.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
        SpaceStrip.Visibility = TopSpaceStrip.Visibility = SpacesOn ? Visibility.Visible : Visibility.Collapsed;
        if (!SpacesOn) return;
        foreach (var s in Shared.Book.In(profileId).ToList()) { SpaceStrip.Children.Add(SpaceButton(s)); TopSpaceStrip.Children.Add(SpaceButton(s)); }
        SpaceStrip.Children.Add(PlusButton());
        TopSpaceStrip.Children.Add(PlusButton());
    }

    Button PlusButton()
    {
        var plus = new Button { Style = (Style)FindResource("Door"), Content = "\uE710", FontSize = 11, ToolTip = "New Space", Margin = new Thickness(0, 0, 2, 0) };
        System.Windows.Automation.AutomationProperties.SetAutomationId(plus, "NewSpace");
        plus.Click += (_, _) => NewSpacePopup(plus);
        return plus;
    }

    static void Hover(Border b, bool selected)
    {
        if (selected) { b.SetResourceReference(Border.BackgroundProperty, "Row.Wash"); return; }
        b.MouseEnter += (_, _) => b.SetResourceReference(Border.BackgroundProperty, "Row.Hover");
        b.MouseLeave += (_, _) => b.Background = Brushes.Transparent;
    }

    Border SpaceButton(Space s)
    {
        var current = s.Id == space.Id;
        var grid = new Grid();
        var glyph = new TextBlock { Text = Spaces.Icons[s.IconIndex], FontFamily = (FontFamily)FindResource("Glyphs"), FontSize = 13, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        glyph.SetResourceReference(TextBlock.ForegroundProperty, current ? "Ink" : "Row.Muted");
        grid.Children.Add(glyph);
        if (Spaces.Rgb(s.Colour) is { } c)
            grid.Children.Add(new Ellipse { Width = 6, Height = 6, Fill = new SolidColorBrush(Color.FromRgb(c.R, c.G, c.B)), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 4, 4) });
        var b = new Border { Width = 28, Height = 28, CornerRadius = new CornerRadius(9), Background = Brushes.Transparent, Child = grid, ToolTip = s.Name, Margin = new Thickness(0, 0, 2, 0), Tag = s };
        Hover(b, current);
        b.MouseLeftButtonUp += (_, _) => SwitchSpace(s);
        b.ContextMenu = SpaceMenu(s);
        return b;
    }

    ContextMenu SpaceMenu(Space s)
    {
        var menu = new ContextMenu();
        MenuItem Add(string header, Action run, bool enabled = true) { var i = new MenuItem { Header = header, IsEnabled = enabled }; i.Click += (_, _) => run(); menu.Items.Add(i); return i; }
        var mine = Shared.Book.In(profileId).ToList();
        Add("Edit in Settings…", () => OpenPage("search://settings/tabs"));
        menu.Items.Add(new Separator());
        Add("Move Left", () => Shared.Book.Move(s.Id, -1), mine.IndexOf(s) > 0);
        Add("Move Right", () => Shared.Book.Move(s.Id, 1), mine.IndexOf(s) < mine.Count - 1);
        menu.Items.Add(new Separator());
        Add("Delete Space", () =>
        {
            if (MessageBox.Show(this, $"Delete “{s.Name}” and close its tabs?", "Search", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
                Shared.Book.DeleteSpace(s.Id);
        }, !s.IsFirst && mine.First() != s);
        return menu;
    }

    // MARK: - a new space

    void NewSpacePopup(FrameworkElement anchor)
    {
        var popup = new Popup { AllowsTransparency = true, PlacementTarget = anchor, Placement = PlacementMode.Top, StaysOpen = false, PopupAnimation = PopupAnimation.Fade };
        var name = new TextBox { Text = "", Height = 30, FontSize = 13, Padding = new Thickness(8, 0, 8, 0), VerticalContentAlignment = VerticalAlignment.Center, Background = Brushes.Transparent, CaretBrush = (Brush)FindResource("Ink") };
        name.SetResourceReference(Control.ForegroundProperty, "Ink");
        name.SetResourceReference(Control.BorderBrushProperty, "Hairline");
        var colour = 1;
        var dots = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0) };
        void Paint()
        {
            dots.Children.Clear();
            for (var i = 0; i < Spaces.Colours.Length; i++)
            {
                var index = i;
                var c = Spaces.Rgb(i) ?? (R: (byte)115, G: (byte)120, B: (byte)133);
                var dot = new Border { Width = 22, Height = 22, CornerRadius = new CornerRadius(11), Margin = new Thickness(0, 0, 8, 0), Background = new SolidColorBrush(Color.FromRgb(c.R, c.G, c.B)), BorderThickness = new Thickness(2), BorderBrush = index == colour ? (Brush)FindResource("Ink") : Brushes.Transparent, ToolTip = Spaces.ColourNames[i], Cursor = Cursors.Arrow };
                dot.MouseLeftButtonUp += (_, _) => { colour = index; Paint(); };
                dots.Children.Add(dot);
            }
        }
        Paint();
        var shares = new CheckBox { Content = "Signed in wherever Personal is", IsChecked = true, Margin = new Thickness(0, 10, 0, 0), FontSize = 12.5 };
        shares.SetResourceReference(Control.ForegroundProperty, "Ink");
        var create = new Button { Content = "Create", Height = 28, Padding = new Thickness(16, 0, 16, 0), HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
        void Make()
        {
            popup.IsOpen = false;
            var s = Shared.Book.AddSpace(profileId, name.Text, colour, shares.IsChecked == true);
            Shared.Prefs.Update(d => d.SpacesOn = true);
            SwitchSpace(s);
        }
        create.Click += (_, _) => Make();
        name.PreviewKeyDown += (_, e) => { if (e.Key == Key.Enter) { Make(); e.Handled = true; } else if (e.Key == Key.Escape) { popup.IsOpen = false; e.Handled = true; } };
        var panel = new StackPanel();
        var title = new TextBlock { Text = "New Space", FontSize = 13, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 10) };
        title.SetResourceReference(TextBlock.ForegroundProperty, "Ink");
        panel.Children.Add(title); panel.Children.Add(name); panel.Children.Add(dots); panel.Children.Add(shares); panel.Children.Add(create);
        var sheet = new Border { Style = (Style)FindResource("Sheet"), Padding = new Thickness(14), Width = 260, Margin = new Thickness(10), Child = panel, Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = Colors.Black, Opacity = 0.10, BlurRadius = 18, ShadowDepth = 5, Direction = 270 } };
        popup.Child = sheet;
        popup.IsOpen = true;
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, () =>
        {
            if (PresentationSource.FromVisual(sheet) is System.Windows.Interop.HwndSource src) Native.SetForegroundWindow(src.Handle);
            name.Focus(); Keyboard.Focus(name);
        });
    }

    // MARK: - tab menu: move to a space

    void MoveToSpace(Tab t, Space target)
    {
        if (target.Id == space.Id) return;
        var wasLive = t == live;
        if (t.View is { } v) { Stage.Children.Remove(v); v.Dispose(); t.View = null; }
        (t.IsPin ? pins : tabs).Remove(t);
        t.Live = false;
        StateOf(target).Tabs.Add(t);
        if (!StateOf(target).Restored) { /* its session will be read on first visit; save this tab into it now */ var shape = Session.Load(target.Id) ?? new SessionShape(); shape.Tabs.Add(t.ToEntry()); StateOf(target).Tabs.Remove(t); Session.Save(shape, target.Id); }
        if (wasLive) { var next = tabs.LastOrDefault() ?? pins.LastOrDefault(); if (next is null) NewTab(); else Select(next); }
        SaveSoon();
    }

    // MARK: - profiles

    void ProfileDoor_Click(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu { PlacementTarget = (UIElement)sender, Placement = PlacementMode.Top };
        foreach (var p in Shared.Book.Profiles.ToList())
        {
            var item = new MenuItem { Header = p.Name, IsChecked = p.Id == profileId };
            var id = p.Id;
            item.Click += (_, _) => OpenProfile(id);
            menu.Items.Add(item);
        }
        menu.Items.Add(new Separator());
        var manage = new MenuItem { Header = "Manage Profiles…" };
        manage.Click += (_, _) => OpenPage("search://settings/tabs");
        menu.Items.Add(manage);
        menu.IsOpen = true;
    }

    /// <summary>A profile has a window of its own: the one that is open, or a new one with its spaces' tabs from last time.</summary>
    public static void OpenProfile(Guid id)
    {
        var open = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault(w => w.profileId == id);
        if (open is not null) { if (open.WindowState == WindowState.Minimized) open.WindowState = WindowState.Normal; open.Activate(); return; }
        new MainWindow(null, persistent: true, profile: id).Show();
    }

    // MARK: - the colour behind the tabs

    /// <summary>The space's colour, faint, over the ground behind the column, the strip and the card (never the page).</summary>
    void ApplyWash()
    {
        var current = Shared.Book.Spaces.FirstOrDefault(s => s.Id == space.Id) ?? space;
        if (!SpacesOn || Spaces.Rgb(current.Colour) is not { } first)
        {
            Resources.Remove("Frame"); Resources.Remove("Frame.Color");
            return;
        }
        var ground = (Color)Application.Current.Resources["Frame.Color"];
        var a = Spaces.Wash(current.Strength, Palette.IsDark);
        Color Mix((byte R, byte G, byte B) c) => Color.FromRgb(
            (byte)Math.Round(ground.R * (1 - a) + c.R * a), (byte)Math.Round(ground.G * (1 - a) + c.G * a), (byte)Math.Round(ground.B * (1 - a) + c.B * a));
        Brush brush;
        if (Spaces.Rgb(current.Colour2) is { } second)
            brush = new LinearGradientBrush(Mix(first), Mix(second), new Point(0, 0), new Point(1, 1));
        else brush = new SolidColorBrush(Mix(first));
        brush.Freeze();
        Resources["Frame"] = brush;
        Resources["Frame.Color"] = Mix(first);
    }
}
