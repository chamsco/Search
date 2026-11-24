using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using Search.Core;

namespace Search.App;

// Wave 6: what a site may do (permissions, notifications, certificates), what it may load (trackers), and the page tools
// (Copy Clean Link, Site Information, Picture in Picture, reading mode, your own style for a site).
public partial class MainWindow
{
    // MARK: - a card over the page that asks something

    readonly Queue<Action> asks = new();
    Popup? askPopup;
    bool askShown;

    /// <summary>A question for the person, on a card over the page's top right (a popup, so it can lie over the page's own window). One at a time; the rest wait.</summary>
    void Ask(string title, string detail, IEnumerable<(string Label, Action Act)> buttons, UIElement? extra = null, bool notice = false)
    {
        if (notice && asks.Count >= 3) return; // a page that will not stop sending them does not get to fill the queue
        var list = buttons.ToList();
        asks.Enqueue(() => ShowAsk(title, detail, list, extra));
        if (!askShown) NextAsk();
    }

    void NextAsk()
    {
        if (asks.Count == 0) { askShown = false; return; }
        askShown = true;
        asks.Dequeue()();
    }

    void ShowAsk(string title, string detail, List<(string Label, Action Act)> buttons, UIElement? extra)
    {
        var popup = askPopup ??= new Popup { AllowsTransparency = true, Placement = PlacementMode.Relative, PlacementTarget = Card, StaysOpen = true, PopupAnimation = PopupAnimation.Fade };
        var panel = new StackPanel { Width = 330 };
        var head = new TextBlock { Text = title, FontSize = 13, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 4) };
        head.SetResourceReference(TextBlock.ForegroundProperty, "Ink");
        System.Windows.Automation.AutomationProperties.SetName(head, title);
        panel.Children.Add(head);
        if (detail.Length > 0)
        {
            var d = new TextBlock { Text = detail, FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8) };
            d.SetResourceReference(TextBlock.ForegroundProperty, "Muted");
            System.Windows.Automation.AutomationProperties.SetName(d, detail);
            panel.Children.Add(d);
        }
        if (extra is not null) panel.Children.Add(extra);
        var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 6, 0, 0) };
        foreach (var (label, act) in buttons)
        {
            var b = new Button { Content = label, Height = 28, Padding = new Thickness(14, 0, 14, 0), Margin = new Thickness(8, 0, 0, 0) };
            System.Windows.Automation.AutomationProperties.SetName(b, label);
            var action = act;
            b.Click += (_, _) =>
            {
                popup.IsOpen = false;
                try { action(); } catch (Exception e) { App.Log(e); }
                NextAsk();
            };
            row.Children.Add(b);
        }
        panel.Children.Add(row);
        popup.Child = new Border
        {
            Style = (Style)FindResource("Sheet"), Padding = new Thickness(14), Margin = new Thickness(12, 6, 18, 22), CornerRadius = new CornerRadius(12), Child = panel,
            Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = Colors.Black, Opacity = 0.10, BlurRadius = 18, ShadowDepth = 5, Direction = 270 },
        };
        PlaceTopRight(popup);
        popup.IsOpen = true;
    }

    // MARK: - what a site may use (camera, microphone, location, notifications…)

    static PermissionKind? KindOf(CoreWebView2PermissionKind k) => k switch
    {
        CoreWebView2PermissionKind.Camera => PermissionKind.Camera,
        CoreWebView2PermissionKind.Microphone => PermissionKind.Microphone,
        CoreWebView2PermissionKind.Geolocation => PermissionKind.Location,
        CoreWebView2PermissionKind.Notifications => PermissionKind.Notifications,
        CoreWebView2PermissionKind.ClipboardRead => PermissionKind.Clipboard,
        CoreWebView2PermissionKind.OtherSensors => PermissionKind.Sensors,
        _ => null,
    };

    void OnPermission(CoreWebView2PermissionRequestedEventArgs e)
    {
        if (KindOf(e.PermissionKind) is not { } kind) return;
        var origin = SitePermissions.OriginOf(e.Uri);
        if (origin.Length == 0) return;
        if (Shared.Perms.Get(origin, kind) is { } known)
        {
            e.State = known == PermissionChoice.Allow ? CoreWebView2PermissionState.Allow : CoreWebView2PermissionState.Deny;
            e.Handled = true;
            return;
        }
        var deferral = e.GetDeferral();
        e.Handled = true;
        var site = new Uri(origin).Authority;
        void Answer(CoreWebView2PermissionState state, PermissionChoice? remember)
        {
            e.State = state;
            if (remember is { } r) Shared.Perms.Set(origin, kind, r);
            deferral.Complete();
        }
        Ask($"{site} wants to {SitePermissions.Describe(kind)}", "Your answer is kept for this site. Settings › Privacy › Site permissions takes it back.",
        [
            ("Allow", () => Answer(CoreWebView2PermissionState.Allow, PermissionChoice.Allow)),
            ("Block", () => Answer(CoreWebView2PermissionState.Deny, PermissionChoice.Block)),
            ("Not now", () => Answer(CoreWebView2PermissionState.Deny, null)),
        ]);
    }

    // MARK: - web notifications

    void OnNotification(Tab t, CoreWebView2NotificationReceivedEventArgs e)
    {
        e.Handled = true;
        var origin = SitePermissions.OriginOf(e.SenderOrigin);
        var site = origin.Length > 0 ? new Uri(origin).Authority : "A site";
        var n = e.Notification;
        App.Trace($"notification from {site}: {n.Title}");
        Ask($"{site}: {n.Title}", n.Body ?? "", [("Show", () => { if (Ordered().Contains(t)) Select(t); }), ("Dismiss", () => { })], notice: true);
    }

    // MARK: - trackers

    readonly List<CoreWebView2> cores = [];
    readonly HashSet<CoreWebView2> filtered = [];

    void WatchTrackers(Tab t, CoreWebView2 core)
    {
        cores.Add(core);
        core.WebResourceRequested += (_, e) =>
        {
            if (!Shared.Prefs.Data.BlockTrackers || !Trackers.IsTracker(e.Request.Uri)) return;
            e.Response = environment!.CreateWebResourceResponse(null, 403, "Blocked", "");
            App.Trace("tracker blocked " + e.Request.Uri);
        };
        SyncTrackerFilters();
    }

    void SyncTrackerFilters()
    {
        var on = Shared.Prefs.Data.BlockTrackers;
        foreach (var core in cores.ToList())
        {
            try
            {
                var has = filtered.Contains(core);
                if (on == has) continue;
                foreach (var host in Trackers.All)
                    foreach (var pattern in new[] { $"*://{host}/*", $"*://*.{host}/*" })
                    {
                        if (on) core.AddWebResourceRequestedFilter(pattern, CoreWebView2WebResourceContext.All);
                        else core.RemoveWebResourceRequestedFilter(pattern, CoreWebView2WebResourceContext.All);
                    }
                if (on) filtered.Add(core); else filtered.Remove(core);
            }
            catch (Exception ex) { App.Log(ex); cores.Remove(core); }
        }
    }

    // MARK: - a site that asks for a certificate

    readonly Dictionary<string, string> certChoice = [];

    void OnClientCertificate(CoreWebView2ClientCertificateRequestedEventArgs e)
    {
        var host = e.Host + ":" + e.Port;
        var offered = e.MutuallyTrustedCertificates.ToList();
        if (offered.Count == 0) return; // nothing of yours that they accept: the engine goes on without
        e.Handled = true;
        if (certChoice.TryGetValue(host, out var serial) && offered.FirstOrDefault(c => c.DerEncodedSerialNumber == serial) is { } again)
        {
            e.SelectedCertificate = again;
            return;
        }
        var deferral = e.GetDeferral();
        var list = new ListBox { MaxHeight = 150, Margin = new Thickness(0, 0, 0, 4), SelectedIndex = 0 };
        System.Windows.Automation.AutomationProperties.SetName(list, "Certificates");
        foreach (var c in offered) list.Items.Add(new ListBoxItem { Content = $"{c.DisplayName}  ·  {c.Issuer}", Tag = c });
        void Done(CoreWebView2ClientCertificate? chosen)
        {
            if (chosen is null) e.Cancel = true;
            else { e.SelectedCertificate = chosen; certChoice[host] = chosen.DerEncodedSerialNumber; }
            deferral.Complete();
        }
        Ask($"{host} asks who you are", "Choose the certificate to send. Nothing is sent unless you do.", 
        [
            ("Continue", () => Done((list.SelectedItem as ListBoxItem)?.Tag as CoreWebView2ClientCertificate)),
            ("Cancel", () => Done(null)),
        ], list);
    }

    // MARK: - Copy Clean Link, Site Information

    void CopyCleanLink(Tab? t = null)
    {
        t ??= live;
        if (t is null || t.IsBlank) return;
        try { Clipboard.SetText(CleanLink.Strip(t.Url)); } catch { }
    }

    void ShowSiteInfo(Tab? t = null)
    {
        t ??= live;
        if (t is null || t.IsBlank) return;
        var origin = SitePermissions.OriginOf(t.Url);
        var own = origin.Length == 0;
        var secure = t.Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || own;
        var title = own ? "Search" : new Uri(origin).Authority;
        var lines = new List<string> { own ? "A page of Search's own." : secure ? "The connection is secure (https)." : "The connection is not secure: what you send can be read on the way." };
        foreach (var (o, kind, choice) in Shared.Perms.All().Where(p => p.Origin == origin))
            lines.Add($"{kind}: {(choice == PermissionChoice.Allow ? "allowed" : "blocked")}");
        if (!own && lines.Count == 1) lines.Add("No permissions kept for this site.");
        var buttons = new List<(string, Action)> { ("Done", () => { }) };
        if (!own && Shared.Perms.Sites.ContainsKey(origin)) buttons.Insert(0, ("Reset permissions", () => Shared.Perms.ResetSite(origin)));
        Ask(title, string.Join("\n", lines), buttons);
    }

    // MARK: - Picture in Picture

    async void PictureInPicture()
    {
        if (live?.View?.CoreWebView2 is not { } core) return;
        // A script run through the protocol with a gesture counts as the person's own click, which the engine wants.
        const string script = """
        (async () => {
          if (document.pictureInPictureElement) { await document.exitPictureInPicture(); return 'closed'; }
          const vs = [...document.querySelectorAll('video')].filter(v => v.readyState > 0 && !v.disablePictureInPicture);
          const v = vs.find(v => !v.paused) || vs.sort((a, b) => b.videoWidth * b.videoHeight - a.videoWidth * a.videoHeight)[0];
          if (!v) return 'novideo';
          await v.requestPictureInPicture();
          return 'opened';
        })()
        """;
        try
        {
            var args = JsonSerializer.Serialize(new { expression = script, userGesture = true, awaitPromise = true, returnByValue = true });
            var result = await core.CallDevToolsProtocolMethodAsync("Runtime.evaluate", args);
            App.Trace("pip " + result);
        }
        catch (Exception e) { App.Trace("pip failed: " + e.Message); }
    }

    // MARK: - reading mode

    async void ToggleReader()
    {
        if (live?.View?.CoreWebView2 is not { } core || live.IsBlank) return;
        try
        {
            var r = await core.ExecuteScriptAsync(ReaderScript.Source);
            App.Trace("reader " + r);
        }
        catch (Exception e) { App.Trace("reader failed: " + e.Message); }
    }

    // MARK: - your own style for a site

    void WatchStyles(Tab t, CoreWebView2 core)
    {
        core.DOMContentLoaded += (_, _) => ApplyStyle(core);
    }

    void ApplyStyle(CoreWebView2 core)
    {
        try
        {
            var css = Shared.Prefs.Data.SiteStylesOn ? Shared.Styles.For(core.Source) : "";
            var js = "(() => { let s = document.getElementById('__search_style'); const css = " + JsonSerializer.Serialize(css) + "; if (!css) { s?.remove(); return; } if (!s) { s = document.createElement('style'); s.id = '__search_style'; (document.head || document.documentElement).appendChild(s); } s.textContent = css; })()";
            _ = core.ExecuteScriptAsync(js);
            if (css.Length > 0) App.Trace("site style on " + SiteStyles.HostOf(core.Source));
        }
        catch (Exception e) { App.Log(e); }
    }

    void OnPermsChanged() => Dispatcher.BeginInvoke(() => PostToPages(PermissionsMessage(), "settings"));

    void OnStylesChanged() => Dispatcher.BeginInvoke(() => { PostToPages(StylesMessage(), "settings"); ApplyStylesEverywhere(); });

    void ApplyStylesEverywhere()
    {
        foreach (var core in cores.ToList()) ApplyStyle(core);
    }
}
