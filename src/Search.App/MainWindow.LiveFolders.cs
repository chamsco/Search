using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Search.Core;

namespace Search.App;

// Live folders (LiveFolders.swift): a pin folder filled from a web feed. It holds the feed's latest posts, each a line that opens
// its page in a new tab as a bookmark would. Only where pin folders are (Pins as rows): with that off nothing is fetched.
// Fetched when made, a little after launch, every half hour, and on Refresh in the folder's menu. Each fetch is a plain request
// that keeps nothing (no cookies, no sign-in, no cache) and gives up past a timeout or 2 MB; one that fails keeps the posts it had.
public partial class MainWindow
{
    readonly Dictionary<Guid, FeedRow> feedRows = [];
    readonly HashSet<Guid> fetching = [];
    static readonly HttpClient FeedClient = MakeFeedClient();
    DispatcherTimer? feedTimer;

    static HttpClient MakeFeedClient()
    {
        var c = new HttpClient(new HttpClientHandler { UseCookies = false, UseDefaultCredentials = false, AllowAutoRedirect = true }) { Timeout = TimeSpan.FromSeconds(20) };
        c.DefaultRequestHeaders.Add("Accept", "application/rss+xml, application/atom+xml, application/xml;q=0.9, text/xml;q=0.9, text/html;q=0.5, */*;q=0.1");
        c.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
        return c;
    }

    void StartLiveFolders()
    {
        if (feedTimer is not null) return;
        feedTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(30) };
        feedTimer.Tick += (_, _) => RefreshAllLiveFolders();
        feedTimer.Start();
        var first = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        first.Tick += (_, _) => { first.Stop(); RefreshAllLiveFolders(); };
        first.Start();
    }

    void RefreshAllLiveFolders()
    {
        if (!FoldersShown) return;
        foreach (var f in StateOf(space).Folders.Where(f => f.IsLive).ToList()) RefreshLiveFolder(f.Id);
    }

    async void RefreshLiveFolder(Guid id)
    {
        var st = StateOf(space);
        if (st.Folders.FirstOrDefault(f => f.Id == id) is not { IsLive: true } folder || !Uri.TryCreate(folder.Feed, UriKind.Absolute, out var url) || !fetching.Add(id)) return;
        var read = await LoadFeed(url);
        fetching.Remove(id);
        App.Trace(read is null ? $"live folder: no feed at {url}" : $"live folder: {read.Items.Count} posts from {url.Host}");
        if (read is null || st.Folders.FirstOrDefault(f => f.Id == id) is not { } now) return;
        var items = Feed.KeepingIds(read.Items, now.Items ?? []);
        if ((now.Items ?? []).Select(i => i.Link + "\n" + i.Title).SequenceEqual(items.Select(i => i.Link + "\n" + i.Title))) return;
        now.Items = items;
        if (st == StateOf(space)) RebuildRows(st);
        SaveSoon();
    }

    /// <summary>The feed at the address, or the feed a page there points to; null when there is none or it cannot be had.</summary>
    static async Task<Feed.Read?> LoadFeed(Uri url)
    {
        for (var hop = 0; hop < 2; hop++)
        {
            string? body;
            try
            {
                using var response = await FeedClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength > Feed.MaxBytes) return null;
                await using var stream = await response.Content.ReadAsStreamAsync();
                var buffer = new MemoryStream();
                var chunk = new byte[16384]; int n;
                while ((n = await stream.ReadAsync(chunk)) > 0) { buffer.Write(chunk, 0, n); if (buffer.Length > Feed.MaxBytes) return null; }
                body = System.Text.Encoding.UTF8.GetString(buffer.ToArray());
            }
            catch { return null; }
            if (Feed.Parse(body, url) is { } read) return read;
            if (hop == 0 && Feed.Discover(body, url) is { } found && found != url) { url = found; continue; }
            return null;
        }
        return null;
    }

    void FeedRow_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || (sender as FrameworkElement)?.DataContext is not FeedRow row) return;
        Open(row.Item.Link);
        e.Handled = true;
    }

    // MARK: - New Live Folder

    void NewLiveFolderPopup(Guid? parent)
    {
        var popup = new Popup { AllowsTransparency = true, PlacementTarget = Card, Placement = PlacementMode.Relative, StaysOpen = false, PopupAnimation = PopupAnimation.Fade };
        var title = new TextBlock { Text = "New Live Folder", FontSize = 13, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) };
        title.SetResourceReference(TextBlock.ForegroundProperty, "Ink");
        var detail = new TextBlock { Text = "A feed’s address, or a site’s that has one. Its latest posts fill the folder, kept fresh every half hour.", FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8) };
        detail.SetResourceReference(TextBlock.ForegroundProperty, "Muted");
        var address = new TextBox { Height = 30, FontSize = 13, Padding = new Thickness(8, 0, 8, 0), VerticalContentAlignment = VerticalAlignment.Center, Background = Brushes.Transparent, CaretBrush = (Brush)FindResource("Ink") };
        System.Windows.Automation.AutomationProperties.SetAutomationId(address, "LiveFolderAddress");
        System.Windows.Automation.AutomationProperties.SetName(address, "Feed address");
        address.SetResourceReference(Control.ForegroundProperty, "Ink");
        address.SetResourceReference(Control.BorderBrushProperty, "Hairline");
        var said = new TextBlock { FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 0), Visibility = Visibility.Collapsed };
        said.SetResourceReference(TextBlock.ForegroundProperty, "Muted");
        var add = new Button { Content = "Add", Height = 28, Padding = new Thickness(16, 0, 16, 0), HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
        System.Windows.Automation.AutomationProperties.SetName(add, "Add");
        var working = false;
        async void Make()
        {
            if (working) return;
            if (Feed.Address(address.Text) is not { } url) { said.Text = "That isn’t a web address. A feed’s address starts with https:// or http://."; said.Visibility = Visibility.Visible; return; }
            working = true; said.Text = "Looking for the feed…"; said.Visibility = Visibility.Visible;
            var read = await LoadFeed(url);
            working = false;
            if (read is null) { said.Text = $"No RSS or Atom feed found at {url.AbsoluteUri}."; return; }
            popup.IsOpen = false;
            var st = StateOf(space);
            var p = parent is Guid g && PinTree.Depth(g, st.Folders) < PinTree.MaxDepth ? parent : null;
            st.Folders.Add(new PinFolder { Name = read.Title, Parent = p, Feed = read.Feed, Items = read.Items });
            if (!FoldersShown) Shared.Prefs.Update(d => d.PinsAsRows = true);
            RebuildRows(st);
            SaveSoon();
        }
        add.Click += (_, _) => Make();
        address.PreviewKeyDown += (_, e) => { if (e.Key == Key.Enter) { Make(); e.Handled = true; } else if (e.Key == Key.Escape) { popup.IsOpen = false; e.Handled = true; } };
        var panel = new StackPanel();
        panel.Children.Add(title); panel.Children.Add(detail); panel.Children.Add(address); panel.Children.Add(said); panel.Children.Add(add);
        popup.Child = new Border { Style = (Style)FindResource("Sheet"), Padding = new Thickness(14), Width = 340, Margin = new Thickness(12, 6, 18, 22), CornerRadius = new CornerRadius(12), Child = panel,
            Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = Colors.Black, Opacity = 0.10, BlurRadius = 18, ShadowDepth = 5, Direction = 270 } };
        PlaceTopRight(popup);
        popup.IsOpen = true;
        Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
        {
            if (PresentationSource.FromVisual(popup.Child) is System.Windows.Interop.HwndSource src) Native.SetForegroundWindow(src.Handle);
            address.Focus(); Keyboard.Focus(address);
        });
    }
}
