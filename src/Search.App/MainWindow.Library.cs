using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Search.Core;

namespace Search.App;

/// <summary>
/// The Library (Zen's, ZenLibrary.mjs): the column widens, a rail of sections runs down its left and the chosen
/// section fills the rest. The sections are our own pages (search.internal), shown here instead of in a tab.
/// </summary>
public partial class MainWindow
{
    static readonly (string Id, string Name, string Glyph, string Page)[] LibrarySections =
    [
        ("spaces", "Spaces", "", "spaces.html"),
        ("history", "History", "", "history.html"),
        ("downloads", "Downloads", "", "downloads.html"),
        ("bookmarks", "Bookmarks", "", "bookmarks.html"),
        ("sites", "Site styles", "", "settings.html#sites"),
        ("archive", "Archived Tabs", "", "archive.html"),
    ];

    const double LibraryWidth = 520;
    WebView2? libraryView;
    readonly Tab libraryTab = new();
    bool libraryOpen, libraryBuilt;
    double libBefore = 232;
    string librarySection = "spaces";
    readonly Dictionary<string, Button> railButtons = [];
    readonly Dictionary<string, Border> railWashes = [];

    void ToggleLibrary(string? section = null)
    {
        if (libraryOpen && (section is null || section == librarySection)) { CloseLibrary(); return; }
        OpenLibrary(section);
    }

    void LibraryDoor_Click(object sender, RoutedEventArgs e) => ToggleLibrary();
    void LibraryBack_Click(object sender, RoutedEventArgs e) => CloseLibrary();
    void LibraryTab_Click(object sender, RoutedEventArgs e)
    {
        var page = LibrarySections.First(x => x.Id == librarySection);
        var kind = page.Id == "sites" ? "settings/sites" : page.Id;
        CloseLibrary();
        OpenPage("search://" + kind);
    }

    void BuildLibrary()
    {
        if (libraryBuilt) return;
        libraryBuilt = true;
        foreach (var (id, name, glyph, _) in LibrarySections)
        {
            var b = new Button { Style = (Style)FindResource("Door"), Content = glyph, ToolTip = name, Margin = new Thickness(0, 0, 0, 4), Width = 32, Height = 32 };
            System.Windows.Automation.AutomationProperties.SetAutomationId(b, "Lib-" + id);
            System.Windows.Automation.AutomationProperties.SetName(b, name);
            var section = id;
            b.Click += (_, _) => ShowSection(section);
            railButtons[id] = b;
            var wash = new Border { CornerRadius = new CornerRadius(9), Margin = new Thickness(0, 0, 0, 4), Child = b };
            b.Margin = new Thickness(0);
            railWashes[id] = wash;
            LibraryRail.Children.Add(wash);
        }
        // The icon tilts on hover and comes back, as Zen's sprite plays and reverses.
        LibraryDoor.MouseEnter += (_, _) => Tilt(-10);
        LibraryDoor.MouseLeave += (_, _) => Tilt(0);
    }

    void Tilt(double to)
    {
        if (!Moves) return;
        LibraryTilt.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(to, TimeSpan.FromMilliseconds(280)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
    }

    void OpenLibrary(string? section = null)
    {
        BuildLibrary();
        var want = section ?? Shared.Prefs.Data.LibrarySection;
        if (LibrarySections.All(x => x.Id != want)) want = "spaces";
        // The column must be there: with it hidden or the tabs along the top, the section opens as a page instead.
        if (Side.Visibility != Visibility.Visible || TopLayout)
        {
            var kind = want == "sites" ? "settings/sites" : want;
            OpenPage("search://" + kind);
            return;
        }
        if (!libraryOpen)
        {
            libraryOpen = true;
            libBefore = SideColumn.ActualWidth > 0 ? SideColumn.ActualWidth : 232;
            LibraryHost.Visibility = Visibility.Visible;
            LibraryHost.Opacity = 0;
            var to = Math.Max(LibraryWidth, libBefore);
            FoldLibrary(libBefore, to, () => { SideColumn.Width = new GridLength(to); });
            if (Moves) LibraryHost.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220)));
            else LibraryHost.Opacity = 1;
            LibraryDoor.Opacity = 1;
        }
        ShowSection(want);
    }

    void CloseLibrary(bool animate = true)
    {
        if (!libraryOpen) return;
        libraryOpen = false;
        var from = SideColumn.ActualWidth;
        void Done() { LibraryHost.Visibility = Visibility.Collapsed; LibraryHost.BeginAnimation(OpacityProperty, null); SideColumn.Width = new GridLength(libBefore); }
        if (!animate || !Moves) { Done(); return; }
        LibraryHost.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(160)));
        FoldLibrary(from, libBefore, Done);
    }

    /// <summary>The column's width between two values over 0.3 s, on Zen's curve (.36,.06,0,.94).</summary>
    void FoldLibrary(double from, double to, Action done)
    {
        if (!Moves) { SideColumn.Width = new GridLength(to); done(); return; }
        var start = DateTime.UtcNow;
        const double ms = 300;
        EventHandler? step = null;
        step = (_, _) =>
        {
            var t = Math.Min(1, (DateTime.UtcNow - start).TotalMilliseconds / ms);
            SideColumn.Width = new GridLength(from + (to - from) * Bezier(t));
            if (t < 1) return;
            CompositionTarget.Rendering -= step;
            done();
        };
        CompositionTarget.Rendering += step;
    }

    /// <summary>cubic-bezier(.36, .06, 0, .94) solved for y at x = t.</summary>
    static double Bezier(double x)
    {
        const double x1 = .36, y1 = .06, x2 = 0, y2 = .94;
        double Cx(double u) => 3 * x1 * u * (1 - u) * (1 - u) + 3 * x2 * u * u * (1 - u) + u * u * u;
        double Cy(double u) => 3 * y1 * u * (1 - u) * (1 - u) + 3 * y2 * u * u * (1 - u) + u * u * u;
        double lo = 0, hi = 1;
        for (var i = 0; i < 24; i++) { var mid = (lo + hi) / 2; if (Cx(mid) < x) lo = mid; else hi = mid; }
        return Cy((lo + hi) / 2);
    }

    async void ShowSection(string id)
    {
        var sec = LibrarySections.FirstOrDefault(x => x.Id == id);
        if (sec.Id is null) return;
        librarySection = id;
        Shared.Prefs.Update(d => d.LibrarySection = id);
        LibraryTitle.Text = sec.Name;
        foreach (var (key, w) in railWashes)
        {
            w.Background = key == id ? (Brush)FindResource("Row.Wash") : Brushes.Transparent;
            railButtons[key].SetResourceReference(Control.ForegroundProperty, key == id ? "Ink" : "Row.Muted");
        }
        try
        {
            if (libraryView is null)
            {
                libraryView = new WebView2 { DefaultBackgroundColor = System.Drawing.Color.Transparent };
                LibraryFrame.Child = libraryView;
                await libraryView.EnsureCoreWebView2Async(environment);
                var core = libraryView.CoreWebView2;
                ApplyColorScheme(core);
                core.SetVirtualHostNameToFolderMapping(PagesHost, PagesDir, CoreWebView2HostResourceAccessKind.Allow);
                core.WebMessageReceived += (_, e) => OnPageMessage(libraryTab, core, e);
                core.Settings.AreDevToolsEnabled = true;
                core.Settings.IsStatusBarEnabled = false;
                core.DownloadStarting += OnDownloadStarting;
                // A link in a section opens as a tab; the column stays as it is.
                core.NewWindowRequested += (_, e) => { e.Handled = true; Open(e.Uri); };
                core.NavigationStarting += (_, e) =>
                {
                    if (PageUrl(e.Uri) is not null || e.Uri.StartsWith("about:", StringComparison.OrdinalIgnoreCase)) return;
                    if (Uri.TryCreate(e.Uri, UriKind.Absolute, out var u) && u.Host.Equals(PagesHost, StringComparison.OrdinalIgnoreCase)) return;
                    e.Cancel = true;
                    if (e.Uri.StartsWith("http", StringComparison.OrdinalIgnoreCase)) Open(e.Uri);
                };
            }
            // A cross-fade between sections: the page dips and comes back (0.1 s each way).
            if (Moves) libraryView.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(110)));
            libraryView.CoreWebView2.Navigate($"https://{PagesHost}/" + sec.Page.Replace(".html", ".html?embed=1"));
            App.Trace("library " + id);
        }
        catch (Exception ex) { App.Log(ex); }
    }

    object SpacesMessage() => new
    {
        type = "spaces",
        on = SpacesOn,
        items = Shared.Book.In(profileId).Select(s =>
        {
            var rgb = Spaces.Rgb(s.Colour);
            return new
            {
                id = s.Id,
                name = s.Name,
                current = s.Id == space.Id,
                glyph = Spaces.Icons[s.IconIndex],
                colour = rgb is { } c ? $"rgb({c.R},{c.G},{c.B})" : "",
                tabs = states.TryGetValue(s.Id, out var st) && st.Restored ? st.Pins.Count + st.Tabs.Count : -1,
            };
        }),
    };

    /// <summary>The Library icon wears a dot while something downloads; it drops in when a download starts.</summary>
    void LibraryDownloads(bool busy)
    {
        if (LibraryBadge is null) return;
        var was = LibraryBadge.Visibility == Visibility.Visible;
        LibraryBadge.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        if (busy && !was && Moves)
            LibraryBadgeDrop.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(-12, 0, TimeSpan.FromMilliseconds(380)) { EasingFunction = new BounceEase { Bounces = 1, Bounciness = 3, EasingMode = EasingMode.EaseOut } });
    }
}
