using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Search.Core;

namespace Search.App;

/// <summary>
/// One window: the column of tabs on the left, the page's card on the right.
/// Milestone 1 of the port of Sources/Search/Browser.swift and Side.swift.
/// </summary>
public partial class MainWindow : Window
{
    const double SideMin = 176, SideMax = 440, CardRadius = 10;

    readonly ObservableCollection<Tab> pins = [];
    readonly ObservableCollection<Tab> tabs = [];
    readonly ClosedTabs closed = new();
    readonly History history = History.Load();
    readonly DispatcherTimer saveSoon;
    Engine engine = Engines.Standard;
    bool engineSuggests = true;
    CoreWebView2Environment? environment;
    Tab? live;
    string? typedBeforeCompletion;
    readonly Dictionary<TextBox, string> lastTyped = [];
    bool restoring;

    public MainWindow(string? open = null)
    {
        InitializeComponent();
        PinGrid.ItemsSource = pins;
        TabList.ItemsSource = tabs;
        saveSoon = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        saveSoon.Tick += (_, _) => { saveSoon.Stop(); SaveNow(); };

        SourceInitialized += (_, _) => { RoundWindowCorners(); AddSystemMenuItems(); };
        StateChanged += (_, _) => FitMaximized();
        Loaded += async (_, _) => await StartAsync(open);
        Closing += (_, _) => { SaveNow(); history.Save(); };
        PreviewKeyDown += Window_PreviewKeyDown;
        PreviewMouseDown += Window_PreviewMouseDown;
        Stage.SizeChanged += (_, _) => RoundLiveView();
        LocationChanged += (_, _) => InlinePopup.IsOpen = false;
        Deactivated += (_, _) => InlinePopup.IsOpen = false;
    }

    // MARK: - start and session

    async Task StartAsync(string? open)
    {
        var data = Store.File("Engine");
        try
        {
            environment = await CoreWebView2Environment.CreateAsync(null, data,
                new CoreWebView2EnvironmentOptions { AreBrowserExtensionsEnabled = true });
        }
        catch (WebView2RuntimeNotFoundException)
        {
            MessageBox.Show(this,
                "Search needs the Microsoft Edge WebView2 Runtime, which comes with Windows 11 and most Windows 10 PCs.\n\nInstall it from go.microsoft.com/fwlink/p/?LinkId=2124703, then open Search again.",
                "Search", MessageBoxButton.OK, MessageBoxImage.Warning);
            Close();
            return;
        }

        restoring = true;
        var shape = Session.Load();
        if (shape is not null)
        {
            if (shape.SideWidth is double w) SideColumn.Width = new GridLength(Math.Clamp(w, SideMin, SideMax));
            foreach (var e in shape.Tabs)
            {
                var t = Tab.From(e);
                (t.IsPin ? pins : tabs).Add(t);
            }
        }
        restoring = false;

        var all = Ordered().ToList();
        if (open is not null)
        {
            Open(File.Exists(open) ? new Uri(Path.GetFullPath(open)).AbsoluteUri : Omnibox.Resolve(open, engine));
        }
        else if (all.Count == 0) NewTab();
        else Select(all[Math.Clamp(shape?.Active ?? 0, 0, all.Count - 1)]);
        StartUpdates();
    }

    IEnumerable<Tab> Ordered() => pins.Concat(tabs);

    void SaveSoon() { if (!restoring) { saveSoon.Stop(); saveSoon.Start(); } }

    void SaveNow()
    {
        var all = Ordered().ToList();
        Session.Save(new SessionShape
        {
            Tabs = all.Select(t => t.ToEntry()).ToList(),
            Active = live is null ? 0 : Math.Max(0, all.IndexOf(live)),
            SideWidth = SideColumn.Width.Value,
        });
    }

    // MARK: - tabs

    Tab NewTab(int? at = null)
    {
        var t = new Tab();
        tabs.Insert(at ?? tabs.Count, t);
        Select(t);
        SaveSoon();
        return t;
    }

    Tab Open(string url, bool background = false, int? at = null)
    {
        var t = new Tab { Url = url };
        tabs.Insert(at ?? tabs.Count, t);
        if (background) { SaveSoon(); return t; }
        Select(t);
        SaveSoon();
        return t;
    }

    void Select(Tab t)
    {
        EndEditing(commit: false);
        if (live is not null) { live.Live = false; if (live.View is not null) live.View.Visibility = Visibility.Hidden; }
        live = t;
        t.Live = true;
        ShowLive();
        SaveSoon();
    }

    void ShowLive()
    {
        if (live is null) return;
        Title = live.IsBlank ? "Search" : $"{live.Label} - Search";
        if (live.IsBlank)
        {
            if (live.View is not null) live.View.Visibility = Visibility.Hidden;
            Blank.Visibility = Visibility.Visible;
            BlankField.Text = "";
            Dispatcher.BeginInvoke(() => { BlankField.Focus(); Keyboard.Focus(BlankField); }, DispatcherPriority.Input);
        }
        else
        {
            Blank.Visibility = Visibility.Collapsed;
            BlankOffersSheet.Visibility = Visibility.Collapsed;
            var view = Wake(live);
            view.Visibility = Visibility.Visible;
            RoundLiveView();
            Dispatcher.BeginInvoke(() => view.Focus(), DispatcherPriority.Input);
        }
        UpdateDoors();
    }

    /// <summary>A tab restored from last time holds no page until it is first shown.</summary>
    WebView2 Wake(Tab t)
    {
        if (t.View is not null) return t.View;
        var view = new WebView2 { Visibility = Visibility.Hidden, DefaultBackgroundColor = System.Drawing.Color.Transparent };
        t.View = view;
        Stage.Children.Add(view);
        _ = AttachSafely(t, view);
        return view;
    }

    async Task AttachSafely(Tab t, WebView2 view)
    {
        try { await Attach(t, view, navigate: true); }
        catch (Exception e) { App.Log(e); }
    }

    async Task Attach(Tab t, WebView2 view, bool navigate)
    {
        await view.EnsureCoreWebView2Async(environment);
        var core = view.CoreWebView2;
        core.Settings.IsStatusBarEnabled = true;
        core.Settings.AreDevToolsEnabled = true;
        core.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Auto;

        core.DocumentTitleChanged += (_, _) =>
        {
            t.Title = core.DocumentTitle;
            history.Retitle(core.Source, core.DocumentTitle);
            if (t == live) Title = $"{t.Label} - Search";
            SaveSoon();
        };
        core.SourceChanged += (_, _) =>
        {
            if (core.Source != "about:blank") t.Url = core.Source;
            UpdateDoors();
            SaveSoon();
        };
        core.NavigationStarting += (_, _) => t.Loading = true;
        core.NavigationCompleted += (_, e) =>
        {
            t.Loading = false;
            if (e.IsSuccess) history.Record(core.Source, core.DocumentTitle);
            UpdateDoors();
        };
        core.HistoryChanged += (_, _) => UpdateDoors();
        core.FaviconChanged += async (_, _) =>
        {
            try { t.Icon = await Favicon(core); } catch (Exception e) { App.Log(e); }
        };
        core.NewWindowRequested += (_, e) => OnNewWindow(t, e);
        core.WindowCloseRequested += (_, _) => Close(t);
        // The engine's own processes can fail (a page's renderer, the GPU). Reload what was lost rather than leave a grey card.
        core.ProcessFailed += (_, e) =>
        {
            App.Log(new Exception($"WebView2 process failed: {e.ProcessFailedKind} ({e.Reason}) on {t.Url}"));
            t.Loading = false;
            if (e.ProcessFailedKind is CoreWebView2ProcessFailedKind.RenderProcessExited or CoreWebView2ProcessFailedKind.RenderProcessUnresponsive)
                try { core.Reload(); } catch (Exception ex) { App.Log(ex); }
        };
        core.ContainsFullScreenElementChanged += (_, _) => FullScreen(core.ContainsFullScreenElement);

        if (t == live) RoundLiveView();
        if (navigate && !t.IsBlank) core.Navigate(t.Url);
    }

    static async Task<ImageSource?> Favicon(CoreWebView2 core)
    {
        // Any icon a site serves must not be able to bring the browser down:
        // copied into memory first (the engine's stream can't seek), and any
        // failure to decode it just leaves the letter showing.
        try
        {
            using var source = await core.GetFaviconAsync(CoreWebView2FaviconImageFormat.Png);
            if (source is null) return null;
            var memory = new MemoryStream();
            await source.CopyToAsync(memory);
            if (memory.Length == 0) return null;
            memory.Position = 0;
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = memory;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception e)
        {
            App.Log(e);
            return null;
        }
    }

    /// <summary>A page asking for a window gets a tab next to it, which keeps window.opener so sign-in pop-ups work.</summary>
    async void OnNewWindow(Tab opener, CoreWebView2NewWindowRequestedEventArgs e)
    {
        var deferral = e.GetDeferral();
        try
        {
            var at = tabs.IndexOf(opener);
            var t = new Tab { Url = e.Uri };
            tabs.Insert(at < 0 ? tabs.Count : at + 1, t);
            var view = new WebView2 { Visibility = Visibility.Hidden };
            t.View = view;
            Stage.Children.Add(view);
            await Attach(t, view, navigate: false);
            e.NewWindow = view.CoreWebView2;
            e.Handled = true;
            Select(t);
        }
        catch (Exception ex) { App.Log(ex); }
        finally { deferral.Complete(); }
    }

    void Close(Tab t)
    {
        var list = t.IsPin ? pins : tabs;
        var index = list.IndexOf(t);
        if (index < 0) return;
        if (!t.IsBlank) closed.Push(t.ToEntry(), index);
        list.RemoveAt(index);
        if (t.View is not null) { Stage.Children.Remove(t.View); t.View.Dispose(); t.View = null; }
        if (t == live)
        {
            live = null;
            // The one below takes its place, or the one above when it was last, as on the Mac.
            var next = tabs.Count > 0 ? tabs[Math.Clamp(t.IsPin ? 0 : index, 0, tabs.Count - 1)]
                     : pins.LastOrDefault();
            if (next is not null) Select(next); else NewTab();
        }
        SaveSoon();
    }

    void Reopen()
    {
        if (closed.Pop() is not var (entry, index)) return;
        var t = Tab.From(entry);
        if (t.IsPin) pins.Insert(Math.Min(index, pins.Count), t);
        else tabs.Insert(Math.Min(index, tabs.Count), t);
        Select(t);
    }

    void Pin(Tab t)
    {
        if (t.IsPin || t.IsBlank) return;
        tabs.Remove(t);
        t.PinId = Guid.NewGuid();
        t.Home = t.Url;
        pins.Add(t);
        SaveSoon();
    }

    void Unpin(Tab t)
    {
        if (!t.IsPin) return;
        pins.Remove(t);
        t.PinId = null;
        t.Home = null;
        tabs.Insert(0, t);
        SaveSoon();
    }

    void Duplicate(Tab t)
    {
        if (t.IsBlank) return;
        var at = tabs.IndexOf(t);
        Open(t.Url, at: at < 0 ? tabs.Count : at + 1);
    }

    void Navigate(string url)
    {
        if (live is null) return;
        live.Url = url;
        var view = Wake(live);
        if (view.CoreWebView2 is not null) view.CoreWebView2.Navigate(url);
        ShowLive();
    }

    void UpdateDoors()
    {
        var core = live?.View?.CoreWebView2;
        BackDoor.IsEnabled = core?.CanGoBack ?? false;
        ForwardDoor.IsEnabled = core?.CanGoForward ?? false;
        ReloadDoor.IsEnabled = core is not null && live is { IsBlank: false };
    }

    // MARK: - the page's corners

    void RoundLiveView()
    {
        if (live?.View is not { } view) return;
        var source = PresentationSource.FromVisual(this);
        var scale = source?.CompositionTarget?.TransformToDevice.M11 ?? 1;
        if (view.Handle != IntPtr.Zero)
            Native.Round(view.Handle, (int)(Stage.ActualWidth * scale), (int)(Stage.ActualHeight * scale), (int)(Card.CornerRadius.TopLeft * scale));
    }

    // MARK: - the field on an empty tab

    void BlankField_TextChanged(object sender, TextChangedEventArgs e)
    {
        Placeholder.Visibility = BlankField.Text.Length == 0 ? Visibility.Visible : Visibility.Hidden;
        if (typedBeforeCompletion is not null) return;
        var typed = BlankField.SelectionLength > 0 ? BlankField.Text[..BlankField.SelectionStart] : BlankField.Text;
        var offers = Offers(typed);
        ShowOffers(BlankOffers, offers);
        var was = BlankOffersSheet.Visibility;
        BlankOffersSheet.Visibility = offers.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        if (was != Visibility.Visible && offers.Count > 0) Appear(BlankOffersSheet);
        ShowCompletion(BlankField, offers);
        AskEngine(typed, BlankOffers, offers, () => BlankOffersSheet.Visibility = Visibility.Visible);
    }

    static void ShowOffers(ListBox list, List<Suggestion> offers)
    {
        list.ItemsSource = offers;
        list.SelectedIndex = -1;
    }

    /// <summary>The list arrives: a little scale and a fade (Motion.quick on the Mac).</summary>
    static void Appear(FrameworkElement sheet)
    {
        var quick = new Duration(TimeSpan.FromMilliseconds(140));
        var ease = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut };
        sheet.BeginAnimation(OpacityProperty, new System.Windows.Media.Animation.DoubleAnimation(0, 1, quick));
        if (sheet.RenderTransform is ScaleTransform scale)
        {
            var grow = new System.Windows.Media.Animation.DoubleAnimation(0.98, 1, quick) { EasingFunction = ease };
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, grow);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, grow);
        }
    }

    static readonly System.Net.Http.HttpClient http = new() { Timeout = TimeSpan.FromSeconds(4) };
    readonly EngineSuggester suggester = new(http);
    CancellationTokenSource? asking;

    /// <summary>Words from the engine, a moment after the last key, added under the rows already shown.</summary>
    async void AskEngine(string typed, ListBox list, List<Suggestion> offers, Action show)
    {
        asking?.Cancel();
        if (!engineSuggests || string.IsNullOrWhiteSpace(typed) || Address.Url(typed) is not null) return;
        var cancel = (asking = new CancellationTokenSource()).Token;
        try
        {
            await Task.Delay(120, cancel);
            var words = await suggester.Suggest(engine, typed, cancel);
            if (cancel.IsCancellationRequested || words.Count == 0) return;
            var picked = list.SelectedIndex;
            list.ItemsSource = Omnibox.WithEngine(offers, typed, words, engine);
            list.SelectedIndex = picked;
            show();
        }
        catch (OperationCanceledException) { }
        catch (Exception e) { App.Log(e); }
    }

    void BlankField_PreviewKeyDown(object sender, KeyEventArgs e) => FieldKeys(BlankField, BlankOffers, e, () =>
    {
        BlankField.Text = "";
        BlankOffersSheet.Visibility = Visibility.Collapsed;
    });

    List<Suggestion> Offers(string typed) =>
        Omnibox.Offers(typed, history, engine, Ordered().Where(t => t != live && !t.IsBlank).Select(t => (t.Id, t.Url, t.Label)));

    /// <summary>The rest of the best match, written after the caret and selected, so typing on replaces it (History.completion).</summary>
    void ShowCompletion(TextBox field, List<Suggestion> offers)
    {
        if (typedBeforeCompletion is not null) return;
        var typed = field.SelectionLength > 0 ? field.Text[..field.SelectionStart] : field.Text;
        if (field.SelectionStart + field.SelectionLength != field.Text.Length) return;
        if (lastTyped.TryGetValue(field, out var before) && before.StartsWith(typed, StringComparison.Ordinal) && before.Length > typed.Length)
        {
            // Deleting never brings the completion back: you are taking letters away.
            lastTyped[field] = typed;
            return;
        }
        lastTyped[field] = typed;
        if (History.Completion(typed, offers) is not { } rest) return;
        typedBeforeCompletion = typed;
        field.Text = typed + rest;
        field.Select(typed.Length, rest.Length);
        typedBeforeCompletion = null;
    }

    void FieldKeys(TextBox field, ListBox list, KeyEventArgs e, Action escape)
    {
        var items = list.ItemsSource as List<Suggestion>;
        switch (e.Key)
        {
            case Key.Down when items is { Count: > 0 }:
                list.SelectedIndex = Math.Min(list.SelectedIndex + 1, items.Count - 1);
                e.Handled = true;
                break;
            case Key.Up when items is { Count: > 0 }:
                list.SelectedIndex = Math.Max(list.SelectedIndex - 1, -1);
                e.Handled = true;
                break;
            case Key.Back or Key.Delete when field.SelectionLength > 0 && field.SelectionStart + field.SelectionLength == field.Text.Length:
                // Backspace takes away the greyed completion, not a letter you typed.
                typedBeforeCompletion = "";
                field.SelectedText = "";
                typedBeforeCompletion = null;
                e.Handled = true;
                break;
            case Key.Enter:
                e.Handled = true;
                // A row you moved to with the arrows; otherwise what is in the field, completion included.
                var picked = items is { Count: > 0 } && list.SelectedIndex >= 0 ? items[list.SelectedIndex] : null;
                Go(picked, field.Text);
                break;
            case Key.Escape:
                e.Handled = true;
                escape();
                break;
        }
    }

    void Go(Suggestion? picked, string text)
    {
        if (string.IsNullOrWhiteSpace(text) && picked is null) return;
        InlinePopup.IsOpen = false;
        if (picked?.Kind == SuggestionKind.Open && picked.Tab is Guid id && Ordered().FirstOrDefault(t => t.Id == id) is { } open)
        {
            var empty = live is { IsBlank: true } ? live : null;
            Select(open);
            if (empty is not null) Close(empty);
            return;
        }
        var url = picked?.Url ?? Omnibox.Resolve(text, engine);
        EndEditing(commit: false);
        Navigate(url);
    }

    void Offer_Click(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is Suggestion s) Go(s, s.Key);
    }

    // MARK: - editing a page's address in its row (Ctrl+L)

    TextBox? inlineField;

    void BeginEditing()
    {
        if (live is null) return;
        if (live.IsBlank) { BlankField.Focus(); BlankField.SelectAll(); return; }
        live.Editing = true;
        Dispatcher.BeginInvoke(() =>
        {
            var row = Container(live);
            inlineField = row is null ? null : FindChild<TextBox>(row, "Field");
            if (inlineField is null || row is null) { live.Editing = false; return; }
            inlineField.Text = Address.Editable(live.Url);
            inlineField.Focus();
            Keyboard.Focus(inlineField);
            inlineField.SelectAll();
            InlinePopup.PlacementTarget = row;
            InlineOffers.ItemsSource = null;
        }, DispatcherPriority.Loaded);
    }

    void EndEditing(bool commit)
    {
        InlinePopup.IsOpen = false;
        asking?.Cancel();
        foreach (var t in Ordered()) t.Editing = false;
        inlineField = null;
        renaming = null;
    }

    Tab? renaming;

    /// <summary>Rename: the row's title becomes a field holding the name; Enter keeps it, an empty name goes back to the page's title.</summary>
    void BeginRenaming(Tab t)
    {
        if (t.IsPin) return;
        EndEditing(false);
        t.Editing = true;
        renaming = t;
        Dispatcher.BeginInvoke(() =>
        {
            var row = Container(t);
            inlineField = row is null ? null : FindChild<TextBox>(row, "Field");
            if (inlineField is null) { t.Editing = false; renaming = null; return; }
            inlineField.Text = t.Label;
            inlineField.Focus();
            Keyboard.Focus(inlineField);
            inlineField.SelectAll();
        }, DispatcherPriority.Loaded);
    }

    void InlineField_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox field || !field.IsKeyboardFocused || renaming is not null) return;
        if (typedBeforeCompletion is not null) return;
        var typed = field.SelectionLength > 0 ? field.Text[..field.SelectionStart] : field.Text;
        var offers = Offers(typed);
        ShowOffers(InlineOffers, offers);
        InlinePopup.IsOpen = offers.Count > 0;
        ShowCompletion(field, offers);
        AskEngine(typed, InlineOffers, offers, () => InlinePopup.IsOpen = true);
    }

    void InlineField_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox field) return;
        if (renaming is { } tab)
        {
            if (e.Key == Key.Enter) { tab.Name = string.IsNullOrWhiteSpace(field.Text) || field.Text == tab.Title ? null : field.Text.Trim(); EndEditing(true); SaveSoon(); e.Handled = true; }
            else if (e.Key == Key.Escape) { EndEditing(false); e.Handled = true; }
            return;
        }
        FieldKeys(field, InlineOffers, e, () => { EndEditing(false); live?.View?.Focus(); });
    }

    void InlineField_LostFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        // Clicking a suggestion moves focus into the popup first; let the click land.
        if (InlinePopup.IsMouseOver) return;
        EndEditing(false);
    }

    // MARK: - rows and pins

    Point pressAt;

    void Row_MouseDown(object sender, MouseButtonEventArgs e)
    {
        pressAt = e.GetPosition(this);
        if ((sender as FrameworkElement)?.DataContext is Tab t && e.ClickCount == 1)
        {
            if (t != live) Select(t);
            e.Handled = true;
        }
        if (sender is FrameworkElement f && e.ClickCount == 1) StartDrag(f, e);
    }

    void Row_MouseUp(object sender, MouseButtonEventArgs e)
    {
        // The middle button closes, as in every browser.
        if (e.ChangedButton == MouseButton.Middle && (sender as FrameworkElement)?.DataContext is Tab t) { Close(t); e.Handled = true; }
    }

    void CloseTab_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is Tab t) Close(t);
        e.Handled = true;
    }

    void NewTab_Click(object sender, MouseButtonEventArgs e) => NewTab();

    void Row_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: Tab t, ContextMenu: { } menu }) return;
        menu.Items.Clear();
        MenuItem Item(string header, Action act, string? key = null, bool enabled = true)
        {
            var m = new MenuItem { Header = header, InputGestureText = key ?? "", IsEnabled = enabled };
            m.Click += (_, _) => act();
            return m;
        }
        // The same items, in the same order, as the Mac's TabMenu (TabBar.swift).
        if (t.IsPin)
        {
            if (t.Home is { } home && History.Identity(t.Url) != History.Identity(home))
            {
                menu.Items.Add(Item("Back to Pinned Page", () => { Select(t); Navigate(home); }));
                menu.Items.Add(Item("Replace Pinned Page with Current URL", () => { t.Home = t.Url; SaveSoon(); }));
            }
            menu.Items.Add(Item("Unpin", () => Unpin(t), "Ctrl+Shift+D"));
        }
        else
        {
            menu.Items.Add(Item("Pin", () => Pin(t), "Ctrl+Shift+D", enabled: !t.IsBlank));
            menu.Items.Add(new Separator());
            menu.Items.Add(Item("Rename", () => BeginRenaming(t)));
        }
        menu.Items.Add(Item("Duplicate", () => Duplicate(t), enabled: !t.IsBlank));
        menu.Items.Add(Item("Copy Address", () => Clipboard.SetText(t.Url), "Ctrl+Shift+C", enabled: !t.IsBlank));
        menu.Items.Add(Item("Copy as Markdown Link", () => Clipboard.SetText($"[{t.Label}]({t.Url})"), enabled: !t.IsBlank));
        menu.Items.Add(Item("Put to Sleep", () => Sleep(t), enabled: !t.Asleep && t != live));
        menu.Items.Add(new Separator());
        menu.Items.Add(Item("Close Tab", () => Close(t), "Ctrl+W"));
        menu.Items.Add(Item("Close Other Tabs", () => { foreach (var o in tabs.Where(o => o != t).ToList()) Close(o); },
            enabled: tabs.Any(o => o != t)));
    }

    /// <summary>The page is let go of, its memory with it; the row stays and loads again when shown.</summary>
    void Sleep(Tab t)
    {
        if (t.View is null || t == live) return;
        Stage.Children.Remove(t.View);
        t.View.Dispose();
        t.View = null;
        t.Loading = false;
    }

    // Dragging a row: the rows make room as it passes over them, and the
    // pins take it when it is let go over them (or the drop zone, with no pins yet).
    Tab? dragging;

    void StartDrag(FrameworkElement source, MouseButtonEventArgs e)
    {
        void Move(object s, MouseEventArgs m)
        {
            if (m.LeftButton != MouseButtonState.Pressed) { source.PreviewMouseMove -= Move; return; }
            var d = m.GetPosition(this) - pressAt;
            if (Math.Abs(d.Y) < 4 && Math.Abs(d.X) < 4) return;
            source.PreviewMouseMove -= Move;
            if (source.DataContext is not Tab t) return;
            dragging = t;
            PinDrop.Visibility = pins.Count == 0 && !t.IsPin && !t.IsBlank ? Visibility.Visible : Visibility.Collapsed;
            source.Opacity = 0.55;
            try { DragDrop.DoDragDrop(source, new DataObject(typeof(Tab), t), DragDropEffects.Move); }
            finally
            {
                source.Opacity = 1;
                dragging = null;
                PinDrop.Visibility = Visibility.Collapsed;
                SaveSoon();
            }
        }
        source.PreviewMouseMove += Move;
    }

    void Row_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = DragDropEffects.Move;
        e.Handled = true;
        if (dragging is not { } moving || (sender as FrameworkElement)?.DataContext is not Tab target || moving == target) return;
        // Within one list, the row moves as you go, so you see where it will land.
        if (moving.IsPin == target.IsPin)
        {
            var list = moving.IsPin ? pins : tabs;
            int from = list.IndexOf(moving), to = list.IndexOf(target);
            if (from >= 0 && to >= 0 && from != to) list.Move(from, to);
        }
    }

    void Row_Drop(object sender, DragEventArgs e)
    {
        e.Handled = true;
        if (e.Data.GetData(typeof(Tab)) is not Tab moving || (sender as FrameworkElement)?.DataContext is not Tab target || moving == target) return;
        if (moving.IsPin == target.IsPin) return; // already moved while dragging
        if (target.IsPin && moving.IsBlank) return;
        (moving.IsPin ? pins : tabs).Remove(moving);
        if (target.IsPin) { moving.PinId = Guid.NewGuid(); moving.Home = moving.Url; }
        else { moving.PinId = null; moving.Home = null; }
        var to = target.IsPin ? pins : tabs;
        to.Insert(Math.Max(0, to.IndexOf(target)), moving);
    }

    void PinDrop_Drop(object sender, DragEventArgs e)
    {
        if (e.Handled || e.Data.GetData(typeof(Tab)) is not Tab moving || moving.IsPin) return;
        e.Handled = true;
        Pin(moving);
    }

    FrameworkElement? Container(Tab t)
    {
        var list = t.IsPin ? (ItemsControl)PinGrid : TabList;
        return list.ItemContainerGenerator.ContainerFromItem(t) as FrameworkElement;
    }

    static T? FindChild<T>(DependencyObject root, string name) where T : FrameworkElement
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T f && f.Name == name) return f;
            if (FindChild<T>(child, name) is { } found) return found;
        }
        return null;
    }

    // MARK: - keys

    void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        var ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
        var shift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
        var alt = Keyboard.Modifiers.HasFlag(ModifierKeys.Alt);
        var core = live?.View?.CoreWebView2;
        var handled = true;

        if (ctrl && !alt && key == Key.T && shift) Reopen();
        else if (ctrl && !alt && key == Key.T) NewTab();
        else if (ctrl && !alt && key is Key.W or Key.F4 && live is not null) Close(live);
        else if ((ctrl && key == Key.L) || (alt && key == Key.D) || key == Key.F6) BeginEditing();
        else if ((ctrl && key == Key.R) || key == Key.F5)
        {
            if (shift || ctrl && key == Key.F5) core?.CallDevToolsProtocolMethodAsync("Page.reload", "{\"ignoreCache\":true}");
            else core?.Reload();
        }
        else if (alt && key == Key.Left || key == Key.BrowserBack) { if (core?.CanGoBack == true) core.GoBack(); }
        else if (alt && key == Key.Right || key == Key.BrowserForward) { if (core?.CanGoForward == true) core.GoForward(); }
        else if (ctrl && key == Key.Tab) Cycle(shift ? -1 : 1);
        else if (ctrl && key is Key.PageDown) Cycle(1);
        else if (ctrl && key is Key.PageUp) Cycle(-1);
        else if (ctrl && key >= Key.D1 && key <= Key.D9) Jump(key - Key.D1);
        else if (ctrl && shift && key == Key.D && live is not null) { if (live.IsPin) Unpin(live); else Pin(live); }
        else if (ctrl && shift && key == Key.C && live is { IsBlank: false }) Clipboard.SetText(live.Url);
        else if (ctrl && key == Key.S && shift) ToggleSide();
        else if (key == Key.F11) ToggleFullScreen();
        else if (key == Key.Escape && fullScreen && core?.ContainsFullScreenElement != true) ToggleFullScreen();
        else handled = false;

        if (handled) e.Handled = true;
    }

    void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        // The mouse's own back and forward buttons.
        var core = live?.View?.CoreWebView2;
        if (e.ChangedButton == MouseButton.XButton1 && core?.CanGoBack == true) { core.GoBack(); e.Handled = true; }
        if (e.ChangedButton == MouseButton.XButton2 && core?.CanGoForward == true) { core.GoForward(); e.Handled = true; }
    }

    void Cycle(int by)
    {
        var all = Ordered().ToList();
        if (all.Count == 0 || live is null) return;
        Select(all[(all.IndexOf(live) + by + all.Count) % all.Count]);
    }

    /// <summary>Ctrl+1 to Ctrl+8 the nth tab, Ctrl+9 the last, as in every browser.</summary>
    void Jump(int n)
    {
        var all = Ordered().ToList();
        if (all.Count == 0) return;
        Select(n == 8 ? all[^1] : all[Math.Min(n, all.Count - 1)]);
    }

    // MARK: - the column's edge

    void SideEdge_DragDelta(object sender, DragDeltaEventArgs e) =>
        SideColumn.Width = new GridLength(Math.Clamp(SideColumn.ActualWidth + e.HorizontalChange, SideMin, SideMax));

    void SideEdge_DragCompleted(object sender, DragCompletedEventArgs e) => SaveSoon();

    double sideBefore = 232;
    void ToggleSide()
    {
        if (Side.Visibility == Visibility.Visible)
        {
            sideBefore = SideColumn.ActualWidth;
            Side.Visibility = Visibility.Collapsed;
            SideColumn.Width = new GridLength(0);
            Card.Margin = new Thickness(8);
        }
        else
        {
            Side.Visibility = Visibility.Visible;
            SideColumn.Width = new GridLength(sideBefore);
            Card.Margin = new Thickness(0, 8, 8, 8);
        }
    }

    // MARK: - full screen

    bool fullScreen;
    WindowState stateBefore;
    void FullScreen(bool on)
    {
        if (on == fullScreen) return;
        ToggleFullScreen();
    }

    void ToggleFullScreen()
    {
        fullScreen = !fullScreen;
        if (fullScreen)
        {
            stateBefore = WindowState;
            Side.Visibility = Visibility.Collapsed;
            SideColumn.Width = new GridLength(0);
            Card.Margin = new Thickness(0);
            Card.CornerRadius = new CornerRadius(0);
            CardShadow.Visibility = Visibility.Collapsed;
            WindowState = WindowState.Normal;
            WindowState = WindowState.Maximized;
            Root.Margin = new Thickness(0);
        }
        else
        {
            Side.Visibility = Visibility.Visible;
            SideColumn.Width = new GridLength(sideBefore > 0 ? sideBefore : 232);
            Card.Margin = new Thickness(0, 8, 8, 8);
            Card.CornerRadius = new CornerRadius(CardRadius);
            CardShadow.Visibility = Visibility.Visible;
            WindowState = stateBefore;
        }
        FitMaximized();
        RoundLiveView();
    }

    // MARK: - the window's own doors

    void CloseWindow_Click(object sender, RoutedEventArgs e) => Close();
    void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    void Maximize_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    void Back_Click(object sender, RoutedEventArgs e) { if (live?.View?.CoreWebView2 is { CanGoBack: true } c) c.GoBack(); }
    void Forward_Click(object sender, RoutedEventArgs e) { if (live?.View?.CoreWebView2 is { CanGoForward: true } c) c.GoForward(); }
    void Reload_Click(object sender, RoutedEventArgs e) => live?.View?.CoreWebView2?.Reload();

    /// <summary>A borderless window maximised spills past the screen by its resize border; take it back.</summary>
    void FitMaximized()
    {
        MaxDoor.Content = WindowState == WindowState.Maximized ? "" : "";
        Root.Margin = WindowState == WindowState.Maximized && !fullScreen ? new Thickness(7) : new Thickness(0);
    }

    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    /// <summary>Windows 11's rounded window corners (no effect on Windows 10).</summary>
    void RoundWindowCorners()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var round = 2; // DWMWCP_ROUND
        _ = DwmSetWindowAttribute(hwnd, 33, ref round, sizeof(int));
    }
}
