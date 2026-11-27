using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using Search.Core;

namespace Search.App;

/// <summary>
/// Search's own pages and the features around them: History, Bookmarks, Downloads and Settings as
/// pages (Sources/Search/InternalPages.swift), the downloads float, find on page, the bookmark card
/// and bar, zoom, print, HTTPS upgrade.
/// A page is served from https://search.internal/ and shown as search://kind in the tab.
/// </summary>
public partial class MainWindow
{
    public const string PagesHost = "search.internal";
    public static readonly string PagesDir = Path.Combine(AppContext.BaseDirectory, "Pages");

    static readonly JsonSerializerOptions Wire = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    void InitPages()
    {
        DownloadList.ItemsSource = Shared.Downloads.Items;
        DownloadsPopup.Closed += (_, _) => downloadsClosed = Environment.TickCount64;
        OnDownloadsChanged();
    }

    // MARK: - addresses

    /// <summary>search://history → https://search.internal/history.html; everything else as it is.</summary>
    static string NavTarget(string url)
    {
        if (InternalPages.Parse(url) is not { } p) return url;
        return $"https://{PagesHost}/{p.Kind}.html" + (p.Detail is null ? "" : "#" + p.Detail);
    }

    /// <summary>The reverse, for the address a tab shows. Null when it isn't one of ours.</summary>
    static string? PageUrl(string source)
    {
        if (!Uri.TryCreate(source, UriKind.Absolute, out var u) || !u.Host.Equals(PagesHost, StringComparison.OrdinalIgnoreCase)) return null;
        var kind = Path.GetFileNameWithoutExtension(u.AbsolutePath);
        if (!InternalPages.Kinds.Contains(kind)) return null;
        var detail = u.Fragment.TrimStart('#');
        return $"search://{kind}" + (detail.Length > 0 ? "/" + detail : "");
    }

    void OpenPage(string url)
    {
        var want = InternalPages.Parse(url);
        var have = Ordered().FirstOrDefault(t => InternalPages.Parse(t.Url) is { } p && p.Kind == want?.Kind);
        if (have is not null)
        {
            if (have != live) Select(have);
            if (want?.Detail is not null && have.Url != url) Navigate(url);
            return;
        }
        if (live is { IsBlank: true }) Navigate(url); else Open(url);
    }

    // MARK: - what a page may ask

    void OnPageMessage(Tab tab, CoreWebView2 core, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (LinkMessage(tab, e) || FormMessage(tab, core, e)) return;
        // Any web page can post a message; only our own pages are listened to.
        if (PageUrl(e.Source) is null || PageUrl(core.Source) is null) return;
        try
        {
            using var doc = JsonDocument.Parse(e.WebMessageAsJson);
            var m = doc.RootElement;
            string S(string k) => m.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
            string? N(string k) => m.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            Guid? G(string k) => Guid.TryParse(N(k), out var g) ? g : null;
            var type = S("type");
            switch (type)
            {
                case "navigate":
                    if (S("url") is { Length: > 0 } to && (InternalPages.Parse(to) is not null || to.StartsWith("http", StringComparison.OrdinalIgnoreCase)))
                        { if (tab == libraryTab) Open(to); else if (tab == live) Navigate(to); }
                    break;

                case "history.query":
                    Send(core, new { type = "history.items", items = history.Query(S("q"), 500).Select(v => new { url = v.Url, title = v.Title, last = v.Last.ToUniversalTime().ToString("o") }) });
                    break;
                case "history.remove":
                    history.Remove(S("url")); history.Save();
                    break;

                case "bookmarks.get": Send(core, BookmarksMessage()); break;
                case "bookmarks.remove": if (G("id") is Guid rm) Marks.Remove(rm); break;
                case "bookmarks.rename": if (G("id") is Guid rn) Marks.Rename(rn, S("title")); break;
                case "bookmarks.addFolder": Marks.AddFolder(S("name")); break;
                case "bookmarks.renameFolder": if (G("id") is Guid rf) Marks.RenameFolder(rf, S("name")); break;
                case "bookmarks.removeFolder": if (G("id") is Guid df) Marks.RemoveFolder(df); break;
                case "bookmarks.move":
                    if (G("id") is Guid mv)
                    {
                        Marks.Move(mv, G("folder"));
                        var order = Marks.Items.Select(b => b.Id).Where(i => i != mv).ToList();
                        var at = G("before") is Guid before ? order.IndexOf(before) : -1;
                        Marks.Reorder(mv, at < 0 ? int.MaxValue : at);
                    }
                    break;

                case "extensions.get" or "extensions.update" or "extensions.add" or "extensions.pick" or "extensions.toggle" or "extensions.remove" or "extensions.popup" or "extensions.options":
                    ExtensionsMessageHandled(type, m.Clone(), core); break;
                case "spaces.get": Send(core, SpacesMessage()); break;
                case "spaces.switch": if (G("id") is Guid spaceId && Shared.Book.In(profileId).FirstOrDefault(x => x.Id == spaceId) is { } sp) { SwitchSpace(sp); Send(core, SpacesMessage()); } break;
                case "spaces.new": NewSpacePopup(libraryOpen ? LibraryRail : Side); break;
                case "spaces.on": Shared.Prefs.Update(d => d.SpacesOn = true); RebuildStrip(); Send(core, SpacesMessage()); break;

                case "archive.get": Send(core, ArchiveMessage()); break;
                case "archive.open": if (G("id") is Guid ao && Shared.ArchiveOf(profileId).Items.FirstOrDefault(i => i.Id == ao) is { } kept) { Open(kept.Url); Shared.ArchiveOf(profileId).Remove(ao); } break;
                case "archive.remove": if (G("id") is Guid ar) Shared.ArchiveOf(profileId).Remove(ar); break;
                case "archive.clear": Shared.ArchiveOf(profileId).Clear(); break;
                case "downloads.get": Send(core, DownloadsMessage()); break;
                case "downloads.openFolder": OpenFolder(Shared.Prefs.EffectiveDownloadFolder); break;
                case "downloads.clear": Shared.Downloads.ClearFinished(); break;
                case "downloads.open" or "downloads.show" or "downloads.pause" or "downloads.resume" or "downloads.cancel" or "downloads.remove":
                    if (G("id") is Guid did && Shared.Downloads.Find(did) is { } item) DoDownload(type[10..], item);
                    break;

                case "settings.get": Send(core, SettingsMessage()); break;
                case var sk when sk.StartsWith("shortcuts."): ShortcutsMessageHandled(sk, m.Clone(), core); break;
                case "permissions.get": Send(core, PermissionsMessage()); break;
                case "permissions.reset":
                    if (Enum.TryParse<PermissionKind>(S("kind"), out var pk)) Shared.Perms.Reset(S("origin"), pk);
                    break;
                case "permissions.resetAll": Shared.Perms.ResetAll(); break;
                case "styles.get": Send(core, StylesMessage()); break;
                case "vault.get": Send(core, VaultMessage()); break;
                case "passkeys.settings": try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ms-settings:savedpasskeys") { UseShellExecute = true }); } catch (Exception ex) { App.Log(ex); } break;
                case "sync.get": Send(core, SyncMessage()); break;
                case "sync.set":
                    Shared.Sync.Configure(m.TryGetProperty("on", out var syncOn) && syncOn.ValueKind == JsonValueKind.True, S("folder").Length > 0 ? S("folder") : Shared.Sync.Config.Folder);
                    App.SyncNow(); PostToPages(SyncMessage(), "settings");
                    break;
                case "sync.pick":
                    {
                        var dlg = new Microsoft.Win32.OpenFolderDialog { Title = "Choose a folder to sync through (OneDrive, Dropbox, a shared disk)" };
                        if (dlg.ShowDialog(this) == true) { Shared.Sync.Configure(true, dlg.FolderName); App.SyncNow(); }
                        PostToPages(SyncMessage(), "settings");
                    }
                    break;
                case "sync.now": App.SyncNow(); PostToPages(SyncMessage(), "settings"); break;
                case "import.scan": Send(core, new { type = "import.sources", sources = Importer.Scan().Select(s => new { id = s.Id, label = s.Label, browser = s.Browser, firefox = s.Firefox }) }); break;
                case "import.run":
                    if (Importer.Scan().FirstOrDefault(s => s.Id == S("id")) is { } src)
                    {
                        bool Kind(string k) => m.TryGetProperty("kinds", out var ks) && ks.ValueKind == JsonValueKind.Array && ks.EnumerateArray().Any(x => x.GetString() == k);
                        var wantBookmarks = Kind("bookmarks"); var wantHistory = Kind("history"); var wantPasswords = Kind("passwords");
                        var wantSpaces = Kind("spaces");
                        _ = Task.Run(() =>
                        {
                            (Search.Core.Import.ImportRead Read, List<string> Notes) got;
                            try { got = Importer.Read(src, wantBookmarks, wantHistory, wantPasswords); }
                            catch (Exception ex) { App.Log(ex); got = (new Search.Core.Import.ImportRead(), [$"{src.Browser} couldn't be read: {ex.Message}"]); }
                            Dispatcher.BeginInvoke(() =>
                            {
                                ImportSummary sum;
                                try { sum = Importer.Apply(got.Read, got.Notes, Marks, history, Shared.Vault); }
                                catch (Exception ex) { App.Log(ex); sum = new ImportSummary(0, 0, 0, [.. got.Notes, $"Bringing it over stopped: {ex.Message}"]); }
                                if (wantSpaces) { try { var sp = Importer.Spaces(src, Shared.Book, profileId); sum = sum with { Notes = [.. sum.Notes, .. sp.Notes], Spaces = sp.Spaces, Pins = sp.Pins }; } catch (Exception ex) { App.Log(ex); } }
                                Send(core, new { type = "import.done", id = src.Id, bookmarks = sum.Bookmarks, visits = sum.Visits, logins = sum.Logins, spaces = sum.Spaces, pins = sum.Pins, notes = sum.Notes });
                            });
                        });
                    }
                    break;
                case "vault.forget": Shared.Vault.Forget(S("host"), S("user")); break;
                case "vault.forgetAll": Shared.Vault.ForgetAll(); break;
                case "vault.never.forget": Shared.Vault.ForgetNever(S("host")); break;
                case "vault.save":
                    if (!Shared.Vault.Save(S("host").Trim().ToLowerInvariant(), S("user").Trim(), S("password"))) PostToPages(new { type = "vault.refused" }, "settings");
                    break;
                case "vault.reveal": _ = RevealPassword(core, S("host"), S("user"), copy: false); break;
                case "vault.copy": _ = RevealPassword(core, S("host"), S("user"), copy: true); break;
                case "styles.set": Shared.Styles.Set(S("host"), S("css")); break;
                case "space.add": Shared.Book.AddSpace(G("profile") ?? profileId, S("name"), 1, true); break;
                case "space.update" when G("id") is Guid sid:
                    Shared.Book.UpdateSpace(sid, sp =>
                    {
                        if (m.TryGetProperty("name", out var nm) && nm.GetString() is { Length: > 0 } n) sp.Name = n.Trim();
                        if (m.TryGetProperty("colour", out var c1)) sp.Colour = Math.Clamp(c1.GetInt32(), 0, Spaces.Colours.Length - 1);
                        if (m.TryGetProperty("colour2", out var c2)) sp.Colour2 = Math.Clamp(c2.GetInt32(), 0, Spaces.Colours.Length - 1);
                        if (m.TryGetProperty("strength", out var st) && Enum.TryParse<SpaceStrength>(st.GetString(), out var sv)) sp.Strength = sv;
                        if (m.TryGetProperty("icon", out var ic)) sp.Icon = Math.Clamp(ic.GetInt32(), 0, Spaces.Icons.Length - 1);
                        if (m.TryGetProperty("shares", out var sh)) sp.SharesSignIns = sh.GetBoolean();
                    });
                    break;
                case "space.delete": if (G("id") is Guid sd) Shared.Book.DeleteSpace(sd); break;
                case "space.move": if (G("id") is Guid sm && m.TryGetProperty("by", out var by)) Shared.Book.Move(sm, by.GetInt32()); break;
                case "profile.add": Shared.Book.AddProfile(S("name")); break;
                case "profile.rename": if (G("id") is Guid pr) Shared.Book.RenameProfile(pr, S("name")); break;
                case "profile.delete": if (G("id") is Guid pd) Shared.Book.DeleteProfile(pd); break;
                case "profile.open": if (G("id") is Guid po) OpenProfile(po); break;
                case "settings.set": SetSetting(S("key"), m.GetProperty("value")); break;
                case "settings.chooseFolder":
                    var dialog = new Microsoft.Win32.OpenFolderDialog { Title = "Where downloads go", InitialDirectory = Shared.Prefs.EffectiveDownloadFolder };
                    if (dialog.ShowDialog(this) == true) Shared.Prefs.Update(d => d.DownloadFolder = dialog.FolderName);
                    break;
                case "settings.defaultBrowser": Process.Start(new ProcessStartInfo("ms-settings:defaultapps") { UseShellExecute = true }); break;
                case "settings.openData": OpenFolder(Path.GetDirectoryName(Store.File("x"))!); break;
                case "settings.checkUpdates": _ = CheckForUpdates(byHand: true, report: text => { updateLine = text; PostToPages(new { type = "update", text }); }); break;
                case "settings.clear": _ = ClearData(core, S("range"), m.GetProperty("kinds").EnumerateArray().Select(k => k.GetString() ?? "").ToList()); break;
            }
        }
        catch (Exception ex) { App.Log(ex); }
    }

    static void Send(CoreWebView2 core, object message)
    {
        try { core.PostWebMessageAsJson(JsonSerializer.Serialize(message, Wire)); } catch { /* the page went away */ }
    }

    /// <summary>To every one of this window's pages of ours that is open.</summary>
    void PostToPages(object message, string? kind = null)
    {
        if (libraryView?.CoreWebView2 is { } lib && PageUrl(lib.Source) is { } there && (kind is null || InternalPages.Parse(there)?.Kind == kind)) Send(lib, message);
        foreach (var t in Ordered())
            if (t.View?.CoreWebView2 is { } c && PageUrl(c.Source) is { } own && (kind is null || InternalPages.Parse(own)?.Kind == kind))
                Send(c, message);
    }

    object BookmarksMessage() => new
    {
        type = "bookmarks",
        items = Marks.Items.Select(b => new { id = b.Id, title = b.Title, url = b.Url, folder = b.Folder }),
        folders = Marks.Folders.Select(f => new { id = f.Id, name = f.Name }),
    };

    object ArchiveMessage() => new
    {
        type = "archive",
        days = Shared.Prefs.Data.CloseIdleDays,
        items = Shared.ArchiveOf(profileId).Items.Select(i => new { id = i.Id, url = i.Url, title = i.Title, closed = i.Closed.ToUniversalTime().ToString("o") }),
    };

    object DownloadsMessage() => new
    {
        type = "downloads",
        folder = Shared.Prefs.EffectiveDownloadFolder,
        items = Shared.Downloads.Items.Select(d => new
        {
            id = d.Id, name = d.Name, state = d.State.ToString().ToLowerInvariant(), received = d.Received, total = d.Total, paused = d.Paused, exists = d.Exists,
        }),
    };

    object PermissionsMessage() => new
    {
        type = "permissions",
        items = Shared.Perms.All().Select(p => new { origin = p.Origin, kind = p.Kind.ToString(), choice = p.Choice.ToString() }),
    };

    object StylesMessage() => new
    {
        type = "styles",
        items = Shared.Styles.Hosts.OrderBy(h => h.Key).Select(h => new { host = h.Key, css = h.Value }),
    };

    string? updateLine;

    object SyncMessage()
    {
        var c = Shared.Sync.Config; var r = Shared.LastSync;
        var status = !c.On ? "Off" : c.Folder.Length == 0 ? "Choose a folder" : r is null ? "Waiting for the first pass" : r.Problem ?? (r.Devices == 0 ? "Synced; no other PC has written here yet" : $"Synced with {r.Devices} other PC{(r.Devices == 1 ? "" : "s")}: " + string.Join(", ", r.Seen.Select(x => x.Name)));
        return new { type = "sync", on = c.On, folder = c.Folder, status };
    }

    object SettingsMessage()
    {
        var d = Shared.Prefs.Data;
        string engineVersion; try { engineVersion = CoreWebView2Environment.GetAvailableBrowserVersionString(); } catch { engineVersion = ""; }
        return new
        {
            type = "settings", engineSuggests = d.EngineSuggests, startFresh = d.StartFresh, bookmarksBar = d.BookmarksBar, httpsUpgrade = d.HttpsUpgrade,
            engine = d.Engine.ToString(), ask = d.Ask.ToString(), askOn = d.AskOn, customEngine = d.CustomEngine, look = d.Look.ToString(), downloadFolder = Shared.Prefs.EffectiveDownloadFolder,
            spacesOn = d.SpacesOn, closeIdleDays = d.CloseIdleDays, littleLinks = d.LittleLinks, peek = d.Peek, splitView = d.SplitView, altSplit = d.AltSplit, peekClick = d.PeekClick, peekStays = d.PeekStays, pinsPeek = d.PinsPeek, layout = d.Layout.ToString(), pinsAsRows = d.PinsAsRows, currentProfile = profileId,
            blockTrackers = d.BlockTrackers, siteSearch = d.SiteSearch, siteStylesOn = d.SiteStylesOn, proxyMode = d.ProxyMode.ToString(), proxyServer = d.ProxyServer, proxyUser = d.ProxyUser, proxyHasPassword = ProxyVault.Has, askHello = d.AskHello, fillFromCard = d.FillFromCard,
            spaces = Shared.Book.Spaces.Select(sp => new { id = sp.Id, name = sp.Name, colour = sp.Colour, colour2 = sp.Colour2, strength = sp.Strength.ToString(), icon = sp.IconIndex, shares = sp.SharesSignIns, profile = sp.ProfileId, first = sp.IsFirst }),
            profiles = Shared.Book.Profiles.Select(pf => new { id = pf.Id, name = pf.Name, personal = pf.IsPersonal }),
            iconGlyphs = Spaces.Icons, iconNames = Spaces.IconNames, colourNames = Spaces.ColourNames,
            version = Version.Current, engineVersion, dataFolder = Path.GetDirectoryName(Store.File("x")), updateText = updateLine,
        };
    }

    void SetSetting(string key, JsonElement value)
    {
        bool B() => value.ValueKind == JsonValueKind.True;
        string S() => value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";
        if (key == "proxyPassword") { ProxyVault.Write(S()); PostToPages(SettingsMessage(), "settings"); return; }
        Shared.Prefs.Update(d =>
        {
            switch (key)
            {
                case "engineSuggests": d.EngineSuggests = B(); break;
                case "startFresh": d.StartFresh = B(); break;
                case "askOn": d.AskOn = B(); break;
                case "bookmarksBar": d.BookmarksBar = B(); break;
                case "spacesOn": d.SpacesOn = B(); break;
                case "pinsAsRows": d.PinsAsRows = B(); break;
                case "closeIdleDays": d.CloseIdleDays = value.ValueKind == JsonValueKind.Number ? Math.Max(0, value.GetInt32()) : 0; break;
                case "littleLinks": d.LittleLinks = B(); break;
                case "peek": d.Peek = B(); break;
                case "splitView": d.SplitView = B(); break;
                case "altSplit": d.AltSplit = B(); break;
                case "peekStays": d.PeekStays = B(); break;
                case "pinsPeek": d.PinsPeek = B(); break;
                case "peekClick": if (S() is "shift" or "altShift" or "altCtrl") d.PeekClick = S(); break;
                case "layout": if (Enum.TryParse<TabsLayout>(S(), out var lay)) d.Layout = lay; break;
                case "httpsUpgrade": d.HttpsUpgrade = B(); break;
                case "blockTrackers": d.BlockTrackers = B(); break;
                case "siteSearch": d.SiteSearch = B(); break;
                case "siteStylesOn": d.SiteStylesOn = B(); break;
                case "askHello": d.AskHello = B(); break;
                case "fillFromCard": d.FillFromCard = B(); break;
                case "proxyMode": if (Enum.TryParse<ProxyMode>(S(), out var pm)) d.ProxyMode = pm; break;
                case "proxyServer": d.ProxyServer = S().Trim(); break;
                case "proxyUser": d.ProxyUser = S().Trim(); if (d.ProxyUser.Length == 0) ProxyVault.Forget(); break;
                case "engine": if (Enum.TryParse<Engine>(S(), out var en)) d.Engine = en; break;
                case "customEngine": d.CustomEngine = S(); break;
                case "ask": if (Enum.TryParse<AskSite>(S(), out var ak)) d.Ask = ak; break;
                case "look": if (Enum.TryParse<LookChoice>(S(), out var lk)) d.Look = lk; break;
            }
        });
    }

    async Task ClearData(CoreWebView2 core, string range, List<string> kinds)
    {
        var since = range switch
        {
            "hour" => DateTime.Now.AddHours(-1), "day" => DateTime.Now.AddDays(-1), "week" => DateTime.Now.AddDays(-7), _ => DateTime.MinValue,
        };
        var done = new List<string>();
        try
        {
            if (kinds.Contains("history")) { history.ClearSince(since); history.Save(); done.Add("history"); }
            if (kinds.Contains("downloads")) { Shared.Downloads.ClearSince(since); done.Add("download list"); }
            var web = default(CoreWebView2BrowsingDataKinds);
            if (kinds.Contains("cookies")) { web |= CoreWebView2BrowsingDataKinds.Cookies; done.Add("cookies"); }
            if (kinds.Contains("cache")) { web |= CoreWebView2BrowsingDataKinds.DiskCache | CoreWebView2BrowsingDataKinds.CacheStorage; done.Add("cache"); }
            if (kinds.Contains("permissions")) { web |= CoreWebView2BrowsingDataKinds.Settings; done.Add("site permissions"); }
            if (web != default(CoreWebView2BrowsingDataKinds))
            {
                if (since == DateTime.MinValue) await core.Profile.ClearBrowsingDataAsync(web);
                else await core.Profile.ClearBrowsingDataAsync(web, since, DateTime.Now.AddMinutes(1));
            }
            PostToPages(new { type = "refresh" }, "history");
            PostToPages(new { type = "cleared", text = done.Count == 0 ? "Nothing to clear." : "Cleared " + string.Join(", ", done) + "." });
        }
        catch (Exception ex)
        {
            App.Log(ex);
            PostToPages(new { type = "cleared", text = "Couldn't clear everything." });
        }
    }

    static void OpenFolder(string path)
    {
        try { Directory.CreateDirectory(path); Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true }); } catch (Exception ex) { App.Log(ex); }
    }

    // MARK: - settings applied

    /// <summary>Look, bar and the address field's engine follow Settings, and so do the pages that show them.</summary>
    void ApplyPrefs()
    {
        var look = Shared.Prefs.Data.Look switch { LookChoice.Light => Palette.Look.Light, LookChoice.Dark => Palette.Look.Dark, _ => Palette.Look.System };
        if (App.Look != look) { App.Look = look; Palette.Apply(Application.Current.Resources, look); }
        foreach (var t in Ordered()) if (t.View?.CoreWebView2 is { } c) { ApplyColorScheme(c); _ = InstallLinkScript(t, c); }
        SyncTrackerFilters();
        ApplyStylesEverywhere();
        BarHost.Visibility = Shared.Prefs.Data.BookmarksBar && !fullScreen ? Visibility.Visible : Visibility.Collapsed;
        BuildBar();
        ApplyAsk();
        ApplyTabsLayout();
        ApplyLayout();
        ApplyWash();
        RebuildStrip();
        PostToPages(SettingsMessage(), "settings");
    }

    void ApplyColorScheme(CoreWebView2 core)
    {
        try
        {
            core.Profile.PreferredColorScheme = Shared.Prefs.Data.Look switch
            {
                LookChoice.Light => CoreWebView2PreferredColorScheme.Light,
                LookChoice.Dark => CoreWebView2PreferredColorScheme.Dark,
                _ => CoreWebView2PreferredColorScheme.Auto,
            };
        }
        catch { }
    }

    void OnBookmarksChanged()
    {
        BuildBar();
        PostToPages(BookmarksMessage(), "bookmarks");
    }

    void OnDownloadsChanged()
    {
        var any = Shared.Downloads.Items.Count > 0;
        DownloadsNone.Visibility = any ? Visibility.Collapsed : Visibility.Visible;
        var busy = Shared.Downloads.Items.Any(i => i.InProgress);
        LibraryDownloads(busy);
        PostToPages(DownloadsMessage(), "downloads");
    }

    // MARK: - downloads

    long downloadsClosed;

    void OnDownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
    {
        e.Handled = true; // ours is the list; the engine's own bubble stays away
        var path = Downloads.UniquePath(Shared.Prefs.EffectiveDownloadFolder, Path.GetFileName(e.ResultFilePath));
        e.ResultFilePath = path;
        Shared.Downloads.Begin(e.DownloadOperation, path);
        if (IsActive || live?.View?.CoreWebView2 == sender)
        {
            DownloadsPopup.PlacementTarget = TopLayout ? TopLibraryDoor : LibraryDoor;
            DownloadsPopup.Placement = TopLayout ? PlacementMode.Bottom : PlacementMode.Top;
            DownloadsPopup.IsOpen = true;
        }
    }

    void DownloadsAll_Click(object sender, RoutedEventArgs e) { DownloadsPopup.IsOpen = false; OpenPage("search://downloads"); }

    DownloadItem? ItemOf(object sender) => (sender as FrameworkElement)?.DataContext as DownloadItem;
    void DownloadOpen_Click(object sender, RoutedEventArgs e) { if (ItemOf(sender) is { } i) DoDownload("open", i); }
    void DownloadShow_Click(object sender, RoutedEventArgs e) { if (ItemOf(sender) is { } i) DoDownload("show", i); }
    void DownloadCancel_Click(object sender, RoutedEventArgs e) { if (ItemOf(sender) is { } i) DoDownload("cancel", i); }

    void DoDownload(string what, DownloadItem i)
    {
        try
        {
            switch (what)
            {
                case "open" when i.CanOpen: Process.Start(new ProcessStartInfo(i.Path) { UseShellExecute = true }); break;
                case "show" when i.Exists: Process.Start("explorer.exe", $"/select,\"{i.Path}\""); break;
                case "pause": Shared.Downloads.Pause(i); break;
                case "resume": Shared.Downloads.Resume(i); break;
                case "cancel": Shared.Downloads.Cancel(i); break;
                case "remove": Shared.Downloads.Remove(i); break;
            }
        }
        catch (Exception ex) { App.Log(ex); }
    }

    // MARK: - the column's foot

    void BookmarksDoor_Click(object sender, RoutedEventArgs e) => OpenPage("search://bookmarks");

    // MARK: - HTTPS upgrade (Settings › Privacy)

    void TryUpgrade(Tab t, CoreWebView2 core, CoreWebView2NavigationStartingEventArgs e)
    {
        if (!Shared.Prefs.Data.HttpsUpgrade || !e.Uri.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) return;
        if (t.NoUpgrade == e.Uri) { t.NoUpgrade = null; return; }
        if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out var u) || Address.IsLocal(u.Host) || !u.IsDefaultPort) return;
        e.Cancel = true;
        t.UpgradedFrom = e.Uri;
        var secure = "https://" + e.Uri["http://".Length..];
        Dispatcher.BeginInvoke(() => core.Navigate(secure));
    }

    /// <summary>The site has no https: load the page as it was written.</summary>
    void FallBackFromHttps(Tab t, CoreWebView2 core, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (e.WebErrorStatus == CoreWebView2WebErrorStatus.OperationCanceled) return;
        if (t.UpgradedFrom is not { } from) return;
        t.UpgradedFrom = null;
        t.NoUpgrade = from;
        Dispatcher.BeginInvoke(() => core.Navigate(from));
    }

    // MARK: - find on page (Ctrl+F)

    string findText = "";

    void PlaceTopRight(System.Windows.Controls.Primitives.Popup popup)
    {
        if (popup.Child is not FrameworkElement child) return;
        child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        popup.HorizontalOffset = Math.Max(0, Card.ActualWidth - child.DesiredSize.Width);
        popup.VerticalOffset = 0;
    }

    void OpenFind()
    {
        if (live?.View?.CoreWebView2 is null || live.IsBlank) return;
        PlaceTopRight(FindPopup);
        FindPopup.IsOpen = true;
        FocusPopup(FindPopup, FindBox);
        if (FindBox.Text.Length > 0) _ = Find(0);
    }

    /// <summary>A popup is a window of its own; the page's window keeps the keyboard until the popup is brought forward.</summary>
    void FocusPopup(System.Windows.Controls.Primitives.Popup popup, TextBox box)
    {
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, () =>
        {
            if (popup.Child is { } child && PresentationSource.FromVisual(child) is System.Windows.Interop.HwndSource source)
                Native.SetForegroundWindow(source.Handle);
            box.Focus();
            Keyboard.Focus(box);
            box.SelectAll();
        });
    }

    void CloseFind()
    {
        if (!FindPopup.IsOpen) return;
        FindPopup.IsOpen = false;
        _ = RunFind("", 0);
    }

    void FindStep(int by)
    {
        if (!FindPopup.IsOpen) { OpenFind(); return; }
        _ = Find(by);
    }

    async Task Find(int step)
    {
        var (index, count) = await RunFind(FindBox.Text, step);
        FindCount.Text = FindBox.Text.Length == 0 ? "" : count == 0 ? "No matches" : $"{index} of {count}";
    }

    async Task<(int, int)> RunFind(string text, int step)
    {
        if (live?.View?.CoreWebView2 is not { } core) return (0, 0);
        try
        {
            var script = FindScript.Replace("__Q__", JsonSerializer.Serialize(text)).Replace("__STEP__", step.ToString());
            var raw = await core.ExecuteScriptAsync(script);
            var inner = JsonSerializer.Deserialize<string>(raw);
            var pair = JsonSerializer.Deserialize<int[]>(inner ?? "[0,0]");
            return pair is { Length: 2 } ? (pair[0], pair[1]) : (0, 0);
        }
        catch { return (0, 0); }
    }

    void FindBox_TextChanged(object sender, TextChangedEventArgs e) { findText = FindBox.Text; _ = Find(0); }

    void FindBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { _ = Find(Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? -1 : 1); e.Handled = true; }
        else if (e.Key == Key.Escape) { CloseFind(); e.Handled = true; }
    }

    void FindPrev_Click(object sender, RoutedEventArgs e) => _ = Find(-1);
    void FindNext_Click(object sender, RoutedEventArgs e) => _ = Find(1);
    void FindClose_Click(object sender, RoutedEventArgs e) => CloseFind();

    /// <summary>Highlights with the page's own CSS Custom Highlight API, so nothing in the page is changed.</summary>
    const string FindScript = """
        (function (q, step) {
          const st = window.__searchFind ??= { q: '', ranges: [], i: -1 };
          const clear = () => { try { CSS.highlights.delete('search-find'); CSS.highlights.delete('search-find-now'); } catch (e) {} };
          if (!q) { clear(); st.q = ''; st.ranges = []; st.i = -1; return JSON.stringify([0, 0]); }
          if (!document.getElementById('search-find-style')) {
            const s = document.createElement('style'); s.id = 'search-find-style';
            s.textContent = '::highlight(search-find){background:#ffe58a;color:#000}::highlight(search-find-now){background:#ff9a3c;color:#000}';
            (document.head || document.documentElement).appendChild(s);
          }
          if (q !== st.q || step === 0) {
            st.q = q; st.ranges = []; const needle = q.toLowerCase();
            const root = document.body || document.documentElement;
            const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT, { acceptNode(n) {
              const p = n.parentElement; if (!p) return NodeFilter.FILTER_REJECT;
              const tag = p.tagName; if (tag === 'SCRIPT' || tag === 'STYLE' || tag === 'NOSCRIPT') return NodeFilter.FILTER_REJECT;
              return n.nodeValue.trim() ? NodeFilter.FILTER_ACCEPT : NodeFilter.FILTER_REJECT; } });
            let n;
            while ((n = walker.nextNode()) && st.ranges.length < 5000) {
              const t = n.nodeValue.toLowerCase(); let at = 0;
              while ((at = t.indexOf(needle, at)) >= 0) {
                const r = document.createRange(); r.setStart(n, at); r.setEnd(n, at + needle.length);
                const box = r.getBoundingClientRect();
                if (box.width > 0 || box.height > 0) st.ranges.push(r);
                at += needle.length;
              }
            }
            st.i = st.ranges.length ? 0 : -1;
          } else if (st.ranges.length) st.i = (st.i + step + st.ranges.length) % st.ranges.length;
          clear();
          if (st.ranges.length) {
            CSS.highlights.set('search-find', new Highlight(...st.ranges));
            const now = new Highlight(st.ranges[st.i]); now.priority = 1; CSS.highlights.set('search-find-now', now);
            const el = st.ranges[st.i].startContainer.parentElement; if (el) el.scrollIntoView({ block: 'center', inline: 'nearest' });
          }
          return JSON.stringify([st.ranges.length ? st.i + 1 : 0, st.ranges.length]);
        })(__Q__, __STEP__)
        """;

    // MARK: - bookmarks: the card (Ctrl+Shift+B) and the bar

    static readonly BookmarkFolder NoFolder = new() { Id = Guid.Empty, Name = "No folder" };
    Bookmark? marked;

    void AddBookmark()
    {
        if (live is not { IsBlank: false } t || t.IsInternal) return;
        marked = Marks.Add(t.Url, t.Label);
        MarkName.Text = marked.Title;
        MarkFolder.ItemsSource = new[] { NoFolder }.Concat(Marks.Folders).ToList();
        MarkFolder.SelectedItem = ((IEnumerable<BookmarkFolder>)MarkFolder.ItemsSource).FirstOrDefault(f => f.Id == (marked.Folder ?? Guid.Empty)) ?? NoFolder;
        PlaceTopRight(MarkPopup);
        MarkPopup.IsOpen = true;
        FocusPopup(MarkPopup, MarkName);
    }

    void MarkDone_Click(object sender, RoutedEventArgs e)
    {
        if (marked is not null)
        {
            Marks.Rename(marked.Id, MarkName.Text);
            Marks.Move(marked.Id, MarkFolder.SelectedItem is BookmarkFolder f && f.Id != Guid.Empty ? f.Id : null);
        }
        MarkPopup.IsOpen = false;
        marked = null;
    }

    void MarkRemove_Click(object sender, RoutedEventArgs e)
    {
        if (marked is not null) Marks.Remove(marked.Id);
        MarkPopup.IsOpen = false;
        marked = null;
    }

    void MarkName_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { MarkDone_Click(sender, e); e.Handled = true; }
        else if (e.Key == Key.Escape) { MarkPopup.IsOpen = false; e.Handled = true; }
    }

    void BuildBar()
    {
        BarItems.Children.Clear();
        if (BarHost.Visibility != Visibility.Visible) return;
        foreach (var b in Marks.In(null)) BarItems.Children.Add(BarButton(b.Title, b.Url, null, () => Navigate(b.Url), b));
        foreach (var f in Marks.Folders)
        {
            var folder = f;
            var button = BarButton(f.Name, null, "", null, null);
            button.MouseLeftButtonUp += (_, _) =>
            {
                var menu = new ContextMenu { PlacementTarget = button, Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom };
                foreach (var b in Marks.In(folder.Id))
                {
                    var item = new MenuItem { Header = b.Title };
                    var url = b.Url;
                    item.Click += (_, _) => Navigate(url);
                    menu.Items.Add(item);
                }
                if (menu.Items.Count == 0) menu.Items.Add(new MenuItem { Header = "Empty", IsEnabled = false });
                menu.IsOpen = true;
            };
            BarItems.Children.Add(button);
        }
    }

    Border BarButton(string title, string? url, string? glyph, Action? click, Bookmark? mark)
    {
        var text = new TextBlock { Text = title, FontSize = 12.5, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, MaxWidth = 160 };
        text.SetResourceReference(TextBlock.ForegroundProperty, "Ink");
        var lead = new TextBlock
        {
            Text = glyph ?? (url is null ? "•" : Address.Host(url) is { Length: > 0 } h ? char.ToUpperInvariant(h[0]).ToString() : "•"),
            FontSize = glyph is null ? 11 : 12, Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.SemiBold,
        };
        if (glyph is not null) lead.FontFamily = (FontFamily)FindResource("Glyphs");
        lead.SetResourceReference(TextBlock.ForegroundProperty, "Muted");
        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(9, 0, 9, 0) };
        row.Children.Add(lead);
        row.Children.Add(text);
        var chip = new Border { CornerRadius = new CornerRadius(8), Height = 26, Margin = new Thickness(0, 0, 2, 0), Background = Brushes.Transparent, Child = row, Cursor = Cursors.Arrow, ToolTip = url };
        chip.MouseEnter += (_, _) => chip.SetResourceReference(Border.BackgroundProperty, "Row.Hover");
        chip.MouseLeave += (_, _) => chip.Background = Brushes.Transparent;
        if (click is not null)
        {
            chip.MouseLeftButtonUp += (_, _) => click();
            chip.MouseUp += (_, e) => { if (e.ChangedButton == MouseButton.Middle && url is not null) Open(url); };
        }
        if (mark is not null)
        {
            var menu = new ContextMenu();
            var open = new MenuItem { Header = "Open in New Tab" }; open.Click += (_, _) => Open(mark.Url);
            var remove = new MenuItem { Header = "Remove Bookmark" }; remove.Click += (_, _) => Marks.Remove(mark.Id);
            menu.Items.Add(open); menu.Items.Add(remove);
            chip.ContextMenu = menu;
        }
        return chip;
    }

    // MARK: - zoom, print, windows

    static readonly double[] ZoomSteps = [0.25, 0.33, 0.5, 0.67, 0.75, 0.8, 0.9, 1, 1.1, 1.25, 1.5, 1.75, 2, 2.5, 3, 4, 5];

    void Zoom(int by)
    {
        if (live?.View is not { CoreWebView2: not null } view) return;
        var now = view.ZoomFactor;
        view.ZoomFactor = by == 0 ? 1
            : by > 0 ? ZoomSteps.FirstOrDefault(z => z > now + 0.001, ZoomSteps[^1])
            : ZoomSteps.LastOrDefault(z => z < now - 0.001, ZoomSteps[0]);
        live.Zoom = view.ZoomFactor;
    }

    void Print()
    {
        try { live?.View?.CoreWebView2?.ShowPrintUI(CoreWebView2PrintDialogKind.Browser); } catch (Exception ex) { App.Log(ex); }
    }

    void NewWindow()
    {
        new MainWindow(null, persistent: true, profile: profileId, window: Guid.NewGuid()).Show();
    }

    /// <summary>A link handed to a running Search (default browser, a second launch).</summary>
    public void OpenExternal(string url)
    {
        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
        var target = File.Exists(url) ? new Uri(Path.GetFullPath(url)).AbsoluteUri : Omnibox.Resolve(url, engine, customEngine);
        if (Shared.Prefs.Data.LittleLinks && target.StartsWith("http", StringComparison.OrdinalIgnoreCase)) { OpenLittle(target); return; }
        Activate();
        Open(target);
    }

    void OpenFile()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Title = "Open File" };
        if (dialog.ShowDialog(this) == true) Open(new Uri(dialog.FileName).AbsoluteUri);
    }

    async void PasteAndGo()
    {
        try { if (Clipboard.GetText() is { Length: > 0 } text) Open(Omnibox.Resolve(text.Trim(), engine, customEngine)); } catch { }
        await Task.CompletedTask;
    }

    void DuplicateTab()
    {
        if (live is { IsBlank: false } t) Open(t.Url);
    }

    void ToggleDevTools() => live?.View?.CoreWebView2?.OpenDevToolsWindow();

    void ClearBrowsingDataPage() => OpenPage("search://settings/privacy");
}

public partial class MainWindow
{
    /// <summary>
    /// The strip's plus follows the last tab until the tabs run out of room; then it stays put at the strip's right edge,
    /// so a new tab is always one click away without scrolling.
    /// </summary>
    void KeepPlusInReach()
    {
        FitTopTabs();
        var pinned = TopPlusPinned.Visibility == Visibility.Visible;
        var room = TopScroll.ViewportWidth + (pinned ? TopPlusPinned.ActualWidth + TopPlusPinned.Margin.Left + TopPlusPinned.Margin.Right : 0);
        var over = TopScroll.ExtentWidth > room + 0.5;
        if (over != pinned)
        {
            TopPlusPinned.Visibility = over ? Visibility.Visible : Visibility.Collapsed;
            TopPlusInline.Visibility = over ? Visibility.Hidden : Visibility.Visible;
        }
        FadeTopEdges(over);
    }

    public static readonly DependencyProperty TopTabWidthProperty = DependencyProperty.Register(nameof(TopTabWidth), typeof(double), typeof(MainWindow), new PropertyMetadata(172.0));
    /// <summary>How wide each tab is along the top: as wide as 172, narrower as more are open, until about ten fit before the strip scrolls.</summary>
    public double TopTabWidth { get => (double)GetValue(TopTabWidthProperty); set => SetValue(TopTabWidthProperty, value); }

    const double TabWidest = 172, TabNarrowest = 72, TabGap = 2;

    void FitTopTabs()
    {
        if (!TopLayout || TopScroll.ActualWidth <= 0) return;
        var rows = TopTabs.ItemsSource as System.Collections.IEnumerable;
        if (rows is null) return;
        int tabsN = 0, heads = 0;
        foreach (var r in rows) { if (r is Tab) tabsN++; else heads++; }
        if (tabsN == 0) return;
        var pinned = TopPlusPinned.Visibility == Visibility.Visible;
        var room = TopScroll.ActualWidth + (pinned ? TopPlusPinned.ActualWidth + TopPlusPinned.Margin.Left + TopPlusPinned.Margin.Right : 0);
        var taken = TopSpaceStrip.ActualWidth + TopSpaceStrip.Margin.Left + TopSpaceStrip.Margin.Right + pins.Count * 40 + heads * 96 + 34;
        var width = Math.Clamp((room - taken) / tabsN - TabGap, TabNarrowest, TabWidest);
        if (Math.Abs(width - TopTabWidth) > 0.5) { TopTabWidth = Math.Floor(width); App.Trace($"top tab width {TopTabWidth} for {tabsN} tabs"); }
    }

    /// <summary>Tabs that run past the strip's edge fade out instead of being cut off.</summary>
    void FadeTopEdges(bool over)
    {
        var w = TopScroll.ActualWidth;
        if (!over || w <= 0) { if (TopScroll.OpacityMask is not null) TopScroll.OpacityMask = null; return; }
        var fade = Math.Min(34 / w, 0.3);
        var left = TopScroll.HorizontalOffset > 1;
        var right = TopScroll.HorizontalOffset + TopScroll.ViewportWidth < TopScroll.ExtentWidth - 1;
        var brush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0) };
        brush.GradientStops.Add(new GradientStop(Color.FromArgb((byte)(left ? 0 : 255), 0, 0, 0), 0));
        brush.GradientStops.Add(new GradientStop(Colors.Black, left ? fade : 0));
        brush.GradientStops.Add(new GradientStop(Colors.Black, right ? 1 - fade : 1));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb((byte)(right ? 0 : 255), 0, 0, 0), 1));
        brush.Freeze();
        TopScroll.OpacityMask = brush;
    }

    void TopScroll_Wheel(object sender, MouseWheelEventArgs e)
    {
        TopScroll.ScrollToHorizontalOffset(TopScroll.HorizontalOffset - e.Delta);
        e.Handled = true;
    }

}
