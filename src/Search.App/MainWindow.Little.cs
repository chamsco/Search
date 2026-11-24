using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using Search.Core;

namespace Search.App;

/// <summary>
/// Little windows (Little.swift): a page in a small window of its own, which is where a link from another app can go,
/// with an Open in Search button to bring it into the row. And Peek (Peek.swift): click a link with a key held
/// and its page opens over this one, to be read and put away or kept as a tab.
/// </summary>
public partial class MainWindow
{
    MiniWindow? peek;
    Window? dim;
    Tab? peekFrom;
    readonly Dictionary<Guid, (string Id, string Source)> linkScripts = [];

    void InitLittle()
    {
        LocationChanged += (_, _) => ClosePeek();
        Stage.SizeChanged += (_, _) => ClosePeek();
        StateChanged += (_, _) => { if (WindowState == WindowState.Minimized) ClosePeek(); };
    }

    /// <summary>The page, as it is, in a window of its own.</summary>
    public async void OpenLittle(string url)
    {
        try
        {
            var w = await Mini(url, little: true, place: w =>
            {
                w.Width = 440; w.Height = 700;
                w.WindowStartupLocation = WindowStartupLocation.Manual;
                w.Left = Math.Max(0, Left + Width - 440 - 24);
                w.Top = Math.Max(0, Top + 60);
            });
            if (w is null) return;
            w.OpenInSearch.Click += (_, _) => { var at = w.View.CoreWebView2?.Source ?? url; w.Close(); Open(at); Activate(); };
            w.Shut.Click += (_, _) => w.Close();
            w.PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape || (e.Key == Key.W && Keyboard.Modifiers == ModifierKeys.Control)) { w.Close(); e.Handled = true; } };
        }
        catch (Exception e) { App.Log(e); }
    }

    async Task<MiniWindow?> Mini(string url, bool little, Action<MiniWindow> place)
    {
        if (environment is null) return null;
        var w = new MiniWindow(little) { Owner = this };
        place(w);
        w.Show(); // a web view starts only once its window exists
        App.Trace("mini: shown");
        var name = live is null ? null : StoreNameOf(live);
        if (name is null) await w.View.EnsureCoreWebView2Async(environment);
        else
        {
            var options = environment.CreateCoreWebView2ControllerOptions();
            options.ProfileName = name;
            await w.View.EnsureCoreWebView2Async(environment, options);
        }
        var core = w.View.CoreWebView2;
        ApplyColorScheme(core);
        core.DownloadStarting += OnDownloadStarting;
        core.NewWindowRequested += (_, e) => { App.Trace("mini: new window asked for " + e.Uri); e.Handled = true; core.Navigate(e.Uri); };
        core.DocumentTitleChanged += (_, _) => w.Say(core.DocumentTitle, core.Source);
        core.SourceChanged += (_, _) => w.Say(core.DocumentTitle, core.Source);
        w.Say("", url);
        core.Navigate(url);
        return w;
    }

    // MARK: - links with keys held

    string LinkScript()
    {
        var d = Shared.Prefs.Data;
        var cfg = JsonSerializer.Serialize(new { peek = d.Peek, key = d.PeekClick, split = d.SplitView && d.AltSplit });
        return """
        (() => {
          if (window.__searchLinks) { window.__searchLinks = CFG; return; }
          window.__searchLinks = CFG;
          addEventListener('click', e => {
            if (e.button !== 0) return;
            const a = e.target && e.target.closest ? e.target.closest('a[href]') : null;
            if (!a || !/^https?:/i.test(a.href)) return;
            const c = window.__searchLinks, s = e.shiftKey, al = e.altKey, ct = e.ctrlKey;
            let type = null;
            if (c.peek && ((c.key === 'shift' && s && !al && !ct) || (c.key === 'altShift' && s && al && !ct) || (c.key === 'altCtrl' && al && ct && !s))) type = 'peek';
            else if (c.split && al && !s && !ct) type = 'splitLink';
            if (!type) return;
            e.preventDefault(); e.stopPropagation();
            chrome.webview.postMessage({ type, url: a.href });
          }, true);
        })();
        """.Replace("CFG", cfg);
    }

    async Task InstallLinkScript(Tab t, CoreWebView2 core)
    {
        try
        {
            var src = LinkScript();
            if (linkScripts.TryGetValue(t.Id, out var old))
            {
                if (old.Source == src) return;
                core.RemoveScriptToExecuteOnDocumentCreated(old.Id);
            }
            linkScripts[t.Id] = (await core.AddScriptToExecuteOnDocumentCreatedAsync(src), src);
            _ = core.ExecuteScriptAsync(src);
        }
        catch (Exception e) { App.Log(e); }
    }

    /// <summary>A page asks to peek at, or open beside it, a link that was clicked with keys held. Any page can ask; only the page on screen is heard.</summary>
    bool LinkMessage(Tab tab, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            using var doc = JsonDocument.Parse(e.WebMessageAsJson);
            var m = doc.RootElement;
            if (m.ValueKind != JsonValueKind.Object || !m.TryGetProperty("type", out var ty)) return false;
            var type = ty.GetString();
            if (type is not ("peek" or "splitLink")) return false;
            if (tab != live || !m.TryGetProperty("url", out var u) || u.GetString() is not { } url) return true;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")) return true;
            var d = Shared.Prefs.Data;
            if (type == "peek" && d.Peek) ShowPeek(uri.AbsoluteUri);
            else if (type == "splitLink" && d.SplitView && d.AltSplit) OpenLinkBeside(uri.AbsoluteUri);
            return true;
        }
        catch (JsonException) { return false; }
    }

    void OpenLinkBeside(string url)
    {
        if (live is null || live.IsBlank) { Open(url); return; }
        var from = live;
        var at = tabs.IndexOf(from);
        var t = new Tab { Url = url };
        tabs.Insert(at < 0 ? tabs.Count : at + 1, t);
        if (Pair(from, t) is null) { Select(t); return; }
        Select(t);
    }

    // MARK: - peek

    public async void ShowPeek(string url)
    {
        App.Trace("peek: asked for " + url);
        if (live is null || WindowState == WindowState.Minimized) return;
        ClosePeek();
        var from = live;
        try
        {
            var source = PresentationSource.FromVisual(Card);
            if (source is null) return;
            var back = source.CompositionTarget.TransformFromDevice;
            var topLeft = back.Transform(Card.PointToScreen(new Point(0, 0)));
            double w = Card.ActualWidth, h = Card.ActualHeight;

            dim = new Window
            {
                WindowStyle = WindowStyle.None, AllowsTransparency = true, ShowInTaskbar = false, ShowActivated = false, Owner = this,
                Background = new SolidColorBrush(Color.FromArgb(0x66, 0, 0, 0)), ResizeMode = ResizeMode.NoResize,
                Left = topLeft.X, Top = topLeft.Y, Width = w, Height = h,
            };
            dim.MouseDown += (_, _) => ClosePeek();
            dim.Show();
            App.Trace("peek: dim shown");

            var panel = await Mini(url, little: false, place: p =>
            {
                p.Width = Math.Round(w * 0.82);
                p.Height = Math.Round(h * 0.9);
                p.WindowStartupLocation = WindowStartupLocation.Manual;
                p.Left = topLeft.X + (w - p.Width) / 2;
                p.Top = topLeft.Y + (h - p.Height) / 2;
                peek = p;
                peekFrom = from;
            });
            App.Trace($"peek: web view started, panel {(panel is null ? "none" : "ok")}, dim {(dim is null ? "gone" : "ok")}, same tab {live == from}");
            if (panel is null || dim is null || live != from) { panel?.Close(); ClosePeek(); return; }
            panel.Shut.Click += (_, _) => ClosePeek();
            panel.OpenInSearch.Content = "Open as tab";
            panel.OpenInSearch.Click += (_, _) =>
            {
                var at = panel.View.CoreWebView2?.Source ?? url;
                var index = tabs.IndexOf(from);
                ClosePeek();
                Open(at, at: index < 0 ? null : index + 1);
            };
            panel.PreviewKeyDown += (_, e) =>
            {
                if (e.Key == Key.Escape || (e.Key == Key.W && Keyboard.Modifiers == ModifierKeys.Control)) { ClosePeek(); e.Handled = true; }
            };
            panel.Deactivated += (_, _) => { if (peek == panel && panel.IsVisible && !Shared.Prefs.Data.PeekStays) Dispatcher.BeginInvoke(() => { if (peek == panel && !panel.IsActive && !(dim?.IsActive ?? false)) ClosePeek(); }); };
        }
        catch (Exception e) { App.Log(e); ClosePeek(); }
    }

    public void ClosePeek()
    {
        if (peek is not null || dim is not null) App.Trace("peek: closed");
        var p = peek; var d = dim;
        peek = null; dim = null; peekFrom = null;
        try { p?.Close(); } catch { }
        try { d?.Close(); } catch { }
    }

    /// <summary>Another tab was chosen: a peek goes with the tab it came from, or is let go.</summary>
    void PeekFollows()
    {
        if (peek is null) return;
        if (live == peekFrom) { dim?.Show(); peek.Show(); return; }
        if (Shared.Prefs.Data.PeekStays) { dim?.Hide(); peek.Hide(); } else ClosePeek();
    }

    /// <summary>A link from a pinned tab to another site opens in a peek, so the pin stays on its page.</summary>
    bool PeekInstead(Tab t, CoreWebView2 core, CoreWebView2NavigationStartingEventArgs e)
    {
        var d = Shared.Prefs.Data;
        if (!d.PinsPeek || !d.Peek || !t.IsPin || t != live || !e.IsUserInitiated || e.IsRedirected) return false;
        if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out var to) || to.Scheme is not ("http" or "https")) return false;
        if (!Uri.TryCreate(core.Source, UriKind.Absolute, out var from) || from.Scheme is not ("http" or "https")) return false;
        static string Site(string host) { var p = host.Split('.'); return string.Join('.', p.Skip(Math.Max(0, p.Length - 2))); }
        if (Site(to.Host) == Site(from.Host)) return false;
        e.Cancel = true;
        ShowPeek(to.AbsoluteUri);
        return true;
    }
}
