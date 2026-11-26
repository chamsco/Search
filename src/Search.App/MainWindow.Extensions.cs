using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Search.Core;

namespace Search.App;

/// <summary>
/// Extensions (Extensions.swift): kept unpacked under Extensions/, loaded into each web profile by the engine's own
/// extension support. Installed from a Chrome Web Store link or id, a .crx/.zip address, or a folder; switched on and off
/// and removed in the Library; an extension's popup and options page open in a small window.
/// </summary>
public partial class MainWindow
{
    /// <summary>Per web profile, the engine's id for each of our extensions (by entry key).</summary>
    static readonly Dictionary<string, Dictionary<string, CoreWebView2BrowserExtension>> loaded = [];
    static readonly Dictionary<string, CoreWebView2Profile> extProfiles = [];
    static bool extWatching;
    static readonly HttpClient web = new() { Timeout = TimeSpan.FromSeconds(60) };

    /// <summary>Make the profile this page lives in hold what Search keeps: add what is missing, switch to match, remove what is gone.</summary>
    async Task EnsureExtensions(CoreWebView2 core)
    {
        if (!extWatching) { extWatching = true; Shared.Extensions.Changed += () => Dispatcher.BeginInvoke(() => { _ = SyncExtensionsAsync(); PostToPages(ExtensionsMessage(), "extensions"); }); }
        var profile = core.Profile;
        var name = profile.ProfileName;
        if (loaded.ContainsKey(name)) return;
        loaded[name] = [];
        extProfiles[name] = profile;
        // With nothing kept there is nothing to wait for: the first page is not held up, and the first install reaches this profile through the change event.
        if (!Shared.Extensions.Any) return;
        await ApplyExtensions(name, profile);
    }

    async Task SyncExtensionsAsync()
    {
        foreach (var (name, profile) in extProfiles.ToList()) await ApplyExtensions(name, profile);
        // The engine may hold a removed extension's files a moment longer: clear what no one owns now and once more shortly after.
        await Task.Run(Shared.Extensions.Sweep);
        _ = Task.Delay(2000).ContinueWith(_ => Shared.Extensions.Sweep());
    }

    async Task ApplyExtensions(string name, CoreWebView2Profile profile)
    {
        try
        {
            var have = loaded[name];
            IReadOnlyList<CoreWebView2BrowserExtension> inEngine = await profile.GetBrowserExtensionsAsync();
            var wanted = Shared.Extensions.Items.ToList();
            // Gone from Search: gone from the engine.
            foreach (var e in inEngine)
                if (Shared.Extensions.Owns(e.Id) && wanted.All(w => w.Name != e.Name))
                {
                    try { await e.RemoveAsync(); Shared.Extensions.Disown(e.Id); } catch (Exception ex) { App.Trace("extension remove failed: " + ex.Message); }
                }
            have.Clear();
            foreach (var w in wanted)
            {
                var ext = inEngine.FirstOrDefault(e => e.Name == w.Name);
                // Its files were replaced (an update) since this profile loaded it: swap it.
                if (ext is not null && w.Applied.TryGetValue(name, out var was) && was != w.Revision)
                {
                    try { await ext.RemoveAsync(); Shared.Extensions.Disown(ext.Id); } catch (Exception ex) { App.Trace("extension swap failed: " + ex.Message); }
                    ext = null;
                }
                if (ext is null)
                {
                    try { ext = await profile.AddBrowserExtensionAsync(Shared.Extensions.PathOf(w)); Shared.Extensions.Own(ext.Id); App.Trace($"extension added {w.Name} {ext.Id}"); }
                    catch (Exception ex) { App.Trace($"extension failed {w.Name}: {ex.Message}"); continue; }
                }
                if (ext.IsEnabled != w.Enabled) { try { await ext.EnableAsync(w.Enabled); } catch (Exception ex) { App.Trace("extension switch failed: " + ex.Message); } }
                have[w.Key] = ext;
                Shared.Extensions.MarkApplied(w.Key, name, w.Revision);
            }
        }
        catch (Exception ex) { App.Trace("extensions: " + ex.Message); }
    }

    // MARK: - what the Library page shows and asks

    object ExtensionsMessage() => new
    {
        type = "extensions",
        items = Shared.Extensions.Items.Select(e => new
        {
            key = e.Key, name = e.Name, version = e.Version, description = e.Description, enabled = e.Enabled,
            icon = IconOf(e), popup = e.Popup is not null, options = e.Options is not null,
            permissions = e.Permissions, sites = e.Sites, store = e.StoreId is not null,
        }),
    };

    static string? IconOf(ExtensionEntry e)
    {
        try
        {
            if (e.Icon is null) return null;
            var path = Path.Combine(Shared.Extensions.PathOf(e), e.Icon.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path) || new FileInfo(path).Length > 400_000) return null;
            var mime = Path.GetExtension(path).ToLowerInvariant() switch { ".svg" => "image/svg+xml", ".jpg" or ".jpeg" => "image/jpeg", ".webp" => "image/webp", _ => "image/png" };
            return $"data:{mime};base64," + Convert.ToBase64String(File.ReadAllBytes(path));
        }
        catch { return null; }
    }

    void ExtensionsMessageHandled(string type, JsonElement m, CoreWebView2 core)
    {
        string S(string k) => m.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
        switch (type)
        {
            case "extensions.get": Send(core, ExtensionsMessage()); break;
            case "extensions.add": _ = InstallExtensionAsync(S("source"), core); break;
            case "extensions.pick":
                var dlg = new Microsoft.Win32.OpenFolderDialog { Title = "Choose the unpacked extension's folder" };
                if (dlg.ShowDialog(this) == true) _ = InstallFolderAsync(dlg.FolderName, core);
                break;
            case "extensions.toggle": Shared.Extensions.SetEnabled(S("key"), m.TryGetProperty("on", out var on) && on.ValueKind == JsonValueKind.True); break;
            case "extensions.remove": Shared.Extensions.Remove(S("key")); break;
            case "extensions.update": _ = UpdateExtensionsAsync(core, byHand: true); break;
            case "extensions.popup": _ = OpenExtensionPage(S("key"), popup: true); break;
            case "extensions.options": _ = OpenExtensionPage(S("key"), popup: false); break;
        }
    }

    async Task InstallFolderAsync(string folder, CoreWebView2 reply)
    {
        try
        {
            Send(reply, new { type = "extensions.busy", text = "Adding…" });
            var e = await Task.Run(() => Shared.Extensions.InstallFolder(folder));
            Send(reply, new { type = "extensions.done", text = $"Added {e.Name}." });
        }
        catch (Exception ex) { Send(reply, new { type = "extensions.error", text = ex.Message }); }
    }

    async Task InstallExtensionAsync(string source, CoreWebView2 reply)
    {
        source = source.Trim();
        if (source.Length == 0) return;
        try
        {
            Send(reply, new { type = "extensions.busy", text = "Downloading…" });
            var id = Extensions.StoreIdOf(source);
            var url = id is not null && !source.EndsWith(".crx", StringComparison.OrdinalIgnoreCase) ? Extensions.CrxUrl(id)
                : Uri.TryCreate(source, UriKind.Absolute, out var u) && (u.Scheme == "https" || u.Scheme == "http") ? source
                : throw new InvalidDataException("That is neither a Chrome Web Store link or id nor a .crx or .zip address.");
            using var response = await web.GetAsync(url);
            if (!response.IsSuccessStatusCode) throw new InvalidDataException($"The download was refused ({(int)response.StatusCode}).");
            var data = await response.Content.ReadAsByteArrayAsync();
            if (data.Length == 0) throw new InvalidDataException("The store sent nothing for that id.");
            if (data.Length > 100_000_000) throw new InvalidDataException("That package is too large.");
            var e = await Task.Run(() => Shared.Extensions.InstallPackage(data, id));
            Send(reply, new { type = "extensions.done", text = $"Added {e.Name}." });
        }
        catch (Exception ex) { App.Log(ex); Send(reply, new { type = "extensions.error", text = ex.Message }); }
    }

    /// <summary>An extension's popup or options page, in a small window of its own.</summary>
    async Task OpenExtensionPage(string key, bool popup)
    {
        if (Shared.Extensions.Find(key) is not { Enabled: true } e) return;
        var page = popup ? e.Popup : e.Options;
        if (page is null) return;
        var w = await Mini("about:blank", little: true, place: win => { win.Width = popup ? 380 : 720; win.Height = popup ? 560 : 640; win.WindowStartupLocation = WindowStartupLocation.CenterOwner; });
        if (w?.View.CoreWebView2 is not { } core) return;
        w.Shut.Click += (_, _) => w.Close();
        w.OpenInSearch.Visibility = Visibility.Collapsed;
        await EnsureExtensions(core);
        if (loaded.TryGetValue(core.Profile.ProfileName, out var have) && have.TryGetValue(key, out var ext))
        {
            var url = $"chrome-extension://{ext.Id}/{page}";
            App.Trace("extension page " + url);
            w.Say(e.Name, url);
            core.Navigate(url);
        }
        else w.Close();
    }
}

public partial class MainWindow
{
    static bool extUpdateStarted;

    /// <summary>Once a day, soon after launch: ask Chrome's updater about each store extension and take a newer package.</summary>
    void StartExtensionUpdates()
    {
        if (extUpdateStarted) return;
        extUpdateStarted = true;
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(30));
            if (Shared.Extensions.UpdateDue(DateTime.Now)) await Dispatcher.InvokeAsync(() => UpdateExtensionsAsync(null, byHand: false)).Task.Unwrap();
        });
    }

    async Task UpdateExtensionsAsync(CoreWebView2? reply, bool byHand)
    {
        var updated = new List<string>();
        try
        {
            if (reply is not null) Send(reply, new { type = "extensions.busy", text = "Checking…" });
            foreach (var e in Shared.Extensions.Items.Where(i => i.StoreId is not null).ToList())
            {
                try
                {
                    var xml = await web.GetStringAsync(Extensions.UpdateCheckUrl(e.StoreId!, e.Version));
                    if (Extensions.NewerPackage(xml, e.Version) is not { } package) continue;
                    var data = await web.GetByteArrayAsync(package);
                    var fresh = await Task.Run(() => Shared.Extensions.InstallPackage(data, e.StoreId));
                    updated.Add(fresh.Name);
                }
                catch (Exception ex) { App.Log(ex); }
            }
            Shared.Extensions.CheckedNow(DateTime.Now);
        }
        catch (Exception ex) { App.Log(ex); }
        if (reply is not null) Send(reply, new { type = "extensions.done", text = updated.Count == 0 ? "Everything is up to date." : "Updated " + string.Join(", ", updated) + "." });
    }
}
