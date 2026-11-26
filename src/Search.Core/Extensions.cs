using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Search.Core;

/// <summary>One extension kept in Search's own folder: where it is, and what its manifest says about it.</summary>
public sealed class ExtensionEntry
{
    public string Key { get; set; } = "";        // folder name under Extensions/
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string Description { get; set; } = "";
    public string? Icon { get; set; }            // relative path inside the folder
    public string? Popup { get; set; }           // action default_popup, relative
    public string? Options { get; set; }         // options page, relative
    public string? StoreId { get; set; }         // the Web Store id, when it came from there
    public bool Enabled { get; set; } = true;
    public List<string> Permissions { get; set; } = [];
    public List<string> Sites { get; set; } = [];
    /// <summary>Counts up each time its files are replaced (an update); a web profile that has an older one swaps it.</summary>
    public int Revision { get; set; }
    /// <summary>Per web profile, the revision it was last loaded at.</summary>
    public Dictionary<string, int> Applied { get; set; } = [];
}

/// <summary>
/// Extensions as the Mac's Extensions.swift keeps them: unpacked in a folder of their own, a record per extension.
/// Installing reads a folder, a .crx/.zip, or a Chrome Web Store link or id; the engine loads what is kept here.
/// </summary>
public sealed class Extensions
{
    sealed class FileShape { public List<ExtensionEntry> Items { get; set; } = []; public DateTime LastUpdateCheck { get; set; } public List<string> OwnedIds { get; set; } = []; }

    const string FileName = "extensions.json";
    FileShape file = new();
    public event Action? Changed;

    public static string Folder => Path.Combine(Store.Root, "Extensions");
    public IReadOnlyList<ExtensionEntry> Items => file.Items;
    /// <summary>Anything kept, or anything of ours that may still sit in a profile.</summary>
    public bool Any => file.Items.Count > 0 || file.OwnedIds.Count > 0;

    public static Extensions Load()
    {
        // Half-made installs from a crash are not kept.
        try { if (Directory.Exists(Folder)) foreach (var d in Directory.GetDirectories(Folder, ".new-*")) Directory.Delete(d, true); } catch { }
        var read = Store.Read<FileShape>(FileName);
        var all = new Extensions { file = read ?? new() };
        if (read is not null) all.Sweep(); // never from an unreadable list: that would take every folder
        return all;
    }
    void Save() { Store.Write(FileName, file); Changed?.Invoke(); }

    public string PathOf(ExtensionEntry e) => Path.Combine(Folder, e.Key);
    public ExtensionEntry? Find(string key) => file.Items.FirstOrDefault(i => i.Key == key);

    /// <summary>A Web Store address or a bare id → the 32-letter id (a–p), or null.</summary>
    public static string? StoreIdOf(string text)
    {
        text = text.Trim();
        var m = Regex.Match(text, @"(?:^|[/=])([a-p]{32})(?:$|[/?#&])");
        return m.Success ? m.Groups[1].Value : null;
    }

    /// <summary>Where Chrome's own updater downloads a store extension from.</summary>
    public static string CrxUrl(string id) =>
        $"https://clients2.google.com/service/update2/crx?response=redirect&prodversion=130.0.6723.0&acceptformat=crx2,crx3&x=id%3D{id}%26installsource%3Dondemand%26uc";

    /// <summary>The zip inside a .crx (versions 2 and 3 have a header in front of it); a plain zip comes back as it is.</summary>
    public static byte[] ZipOf(byte[] data)
    {
        if (data.Length < 12 || data[0] != 'C' || data[1] != 'r' || data[2] != '2' || data[3] != '4') return data;
        var version = BitConverter.ToUInt32(data, 4);
        int start;
        if (version == 2) start = 16 + (int)BitConverter.ToUInt32(data, 8) + (int)BitConverter.ToUInt32(data, 12);
        else if (version == 3) start = 12 + (int)BitConverter.ToUInt32(data, 8);
        else throw new InvalidDataException("Unknown extension package version " + version);
        if (start < 0 || start >= data.Length) throw new InvalidDataException("The extension package is damaged.");
        return data[start..];
    }

    /// <summary>Install from bytes of a .crx or .zip.</summary>
    public ExtensionEntry InstallPackage(byte[] data, string? storeId = null)
    {
        var temp = Path.Combine(Folder, ".new-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(temp);
        try
        {
            using var zip = new ZipArchive(new MemoryStream(ZipOf(data)));
            var root = Path.GetFullPath(temp) + Path.DirectorySeparatorChar;
            foreach (var entry in zip.Entries)
            {
                if (entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\')) continue;
                var target = Path.GetFullPath(Path.Combine(temp, entry.FullName.Replace('/', Path.DirectorySeparatorChar)));
                if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase)) continue; // never outside its own folder
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                entry.ExtractToFile(target, true);
            }
            return Adopt(temp, storeId, move: true);
        }
        catch { try { Directory.Delete(temp, true); } catch { } throw; }
    }

    /// <summary>Install an unpacked folder (copied, so the original can go away).</summary>
    public ExtensionEntry InstallFolder(string source)
    {
        if (!File.Exists(Path.Combine(source, "manifest.json"))) throw new InvalidDataException("That folder has no manifest.json.");
        var temp = Path.Combine(Folder, ".new-" + Guid.NewGuid().ToString("N")[..8]);
        CopyAll(source, temp);
        try { return Adopt(temp, null, move: true); }
        catch { try { Directory.Delete(temp, true); } catch { } throw; }
    }

    static void CopyAll(string from, string to)
    {
        Directory.CreateDirectory(to);
        foreach (var f in Directory.GetFiles(from)) File.Copy(f, Path.Combine(to, Path.GetFileName(f)), true);
        foreach (var d in Directory.GetDirectories(from)) CopyAll(d, Path.Combine(to, Path.GetFileName(d)));
    }

    ExtensionEntry Adopt(string temp, string? storeId, bool move)
    {
        // A zip made from a folder may hold everything one level down.
        var outer = temp;
        var manifestAt = Path.Combine(temp, "manifest.json");
        if (!File.Exists(manifestAt))
        {
            var inner = Directory.GetDirectories(temp).FirstOrDefault(d => File.Exists(Path.Combine(d, "manifest.json")));
            if (inner is null) throw new InvalidDataException("There is no manifest.json in it: not an extension.");
            temp = inner;
        }
        var entry = Describe(temp);
        entry.StoreId = storeId;
        // The same extension again replaces the old copy (an update).
        var same = file.Items.FirstOrDefault(i => (storeId is not null && i.StoreId == storeId) || (i.Name == entry.Name && i.StoreId is null && storeId is null));
        var key = same?.Key ?? Guid.NewGuid().ToString("N")[..12];
        var dest = Path.Combine(Folder, key);
        if (same is not null) { try { Directory.Delete(dest, true); } catch { } entry.Enabled = same.Enabled; entry.Revision = same.Revision + 1; entry.Applied = same.Applied; file.Items.Remove(same); }
        Directory.CreateDirectory(Folder);
        Directory.Move(temp, dest);
        if (outer != temp) { try { Directory.Delete(outer, true); } catch { } }
        entry.Key = key;
        file.Items.Add(entry);
        Save();
        return entry;
    }

    /// <summary>What a manifest says: name and description (with __MSG_ lookups), icon, popup, options, permissions.</summary>
    public static ExtensionEntry Describe(string folder)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "manifest.json")), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        var m = doc.RootElement;
        string Str(JsonElement e, string k) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
        var messages = LoadMessages(folder, Str(m, "default_locale"));
        string Loc(string s) => s.StartsWith("__MSG_", StringComparison.Ordinal) && s.EndsWith("__") && messages.TryGetValue(s[6..^2].ToLowerInvariant(), out var t) ? t : s;
        var e = new ExtensionEntry { Name = Loc(Str(m, "name")), Version = Str(m, "version"), Description = Loc(Str(m, "description")) };
        if (e.Name.Length == 0) throw new InvalidDataException("The manifest has no name.");
        // Icon: the largest listed, from "icons" or the action's.
        string? best = null; var bestSize = -1;
        void Icons(JsonElement set)
        {
            if (set.ValueKind == JsonValueKind.String) { if (bestSize < 0) { best = set.GetString(); bestSize = 0; } return; }
            if (set.ValueKind != JsonValueKind.Object) return;
            foreach (var p in set.EnumerateObject())
                if (int.TryParse(p.Name, out var size) && size > bestSize && p.Value.ValueKind == JsonValueKind.String) { best = p.Value.GetString(); bestSize = size; }
        }
        if (m.TryGetProperty("icons", out var icons)) Icons(icons);
        var action = m.TryGetProperty("action", out var a) ? a : m.TryGetProperty("browser_action", out var b) ? b : default;
        if (bestSize < 0 && action.ValueKind == JsonValueKind.Object && action.TryGetProperty("default_icon", out var di)) Icons(di);
        e.Icon = best?.TrimStart('/');
        var popup = Str(action, "default_popup");
        e.Popup = popup.Length > 0 ? popup.TrimStart('/') : null;
        var options = Str(m, "options_page");
        if (options.Length == 0 && m.TryGetProperty("options_ui", out var ou)) options = Str(ou, "page");
        e.Options = options.Length > 0 ? options.TrimStart('/') : null;
        foreach (var key in new[] { "permissions", "optional_permissions" })
            if (m.TryGetProperty(key, out var perms) && perms.ValueKind == JsonValueKind.Array)
                foreach (var p in perms.EnumerateArray()) if (p.ValueKind == JsonValueKind.String) e.Permissions.Add(p.GetString()!);
        foreach (var key in new[] { "host_permissions" })
            if (m.TryGetProperty(key, out var hosts) && hosts.ValueKind == JsonValueKind.Array)
                foreach (var h in hosts.EnumerateArray()) if (h.ValueKind == JsonValueKind.String) e.Sites.Add(h.GetString()!);
        if (m.TryGetProperty("content_scripts", out var cs) && cs.ValueKind == JsonValueKind.Array)
            foreach (var script in cs.EnumerateArray())
                if (script.TryGetProperty("matches", out var matches) && matches.ValueKind == JsonValueKind.Array)
                    foreach (var h in matches.EnumerateArray()) if (h.ValueKind == JsonValueKind.String && !e.Sites.Contains(h.GetString()!)) e.Sites.Add(h.GetString()!);
        return e;
    }

    static Dictionary<string, string> LoadMessages(string folder, string preferred)
    {
        var result = new Dictionary<string, string>();
        foreach (var locale in new[] { preferred, "en", "en_US", "en_GB" }.Where(l => l.Length > 0))
        {
            var path = Path.Combine(folder, "_locales", locale, "messages.json");
            if (!File.Exists(path)) continue;
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
                foreach (var p in doc.RootElement.EnumerateObject())
                    if (p.Value.ValueKind == JsonValueKind.Object && p.Value.TryGetProperty("message", out var msg) && msg.ValueKind == JsonValueKind.String)
                        result.TryAdd(p.Name.ToLowerInvariant(), msg.GetString()!);
            }
            catch (JsonException) { }
            if (result.Count > 0) break;
        }
        return result;
    }

    /// <summary>Written down without a change event: the engine now holds this revision.</summary>
    public void MarkApplied(string key, string profile, int revision)
    {
        if (Find(key) is not { } e || (e.Applied.TryGetValue(profile, out var have) && have == revision)) return;
        e.Applied[profile] = revision;
        Store.Write(FileName, file);
    }

    /// <summary>The engine's ids of extensions Search itself added (anything else in a profile is not ours to remove).</summary>
    public bool Owns(string engineId) => file.OwnedIds.Contains(engineId);
    public void Own(string engineId) { if (!file.OwnedIds.Contains(engineId)) { file.OwnedIds.Add(engineId); Store.Write(FileName, file); } }
    public void Disown(string engineId) { if (file.OwnedIds.Remove(engineId)) Store.Write(FileName, file); }

    public bool UpdateDue(DateTime now) => now - file.LastUpdateCheck > TimeSpan.FromHours(20);
    public void CheckedNow(DateTime now) { file.LastUpdateCheck = now; Store.Write(FileName, file); }

    /// <summary>Where Chrome's updater says the newer package is, or null when there is none.</summary>
    public static string UpdateCheckUrl(string id, string version) =>
        $"https://clients2.google.com/service/update2/crx?acceptformat=crx3&prodversion=130.0.6723.0&x=id%3D{id}%26v%3D{Uri.EscapeDataString(version)}%26uc";

    public static string? NewerPackage(string xml, string current)
    {
        var tag = Regex.Match(xml, "<updatecheck\\b[^>]*>");
        if (!tag.Success) return null;
        string? Attr(string name) { var m = Regex.Match(tag.Value, name + "=\"([^\"]*)\""); return m.Success ? System.Net.WebUtility.HtmlDecode(m.Groups[1].Value) : null; }
        if (Attr("status") is { } status && status != "ok") return null;
        var version = Attr("version"); var codebase = Attr("codebase");
        if (version is null || codebase is null || !codebase.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return null;
        return Version.TryParse(version, out var n) && Version.TryParse(current, out var c) ? (n > c ? codebase : null) : (version != current ? codebase : null);
    }

    public void SetEnabled(string key, bool on) { if (Find(key) is { } e && e.Enabled != on) { e.Enabled = on; Save(); } }

    public void Remove(string key)
    {
        if (Find(key) is not { } e) return;
        file.Items.Remove(e);
        try { Directory.Delete(PathOf(e), true); } catch { }
        Save();
    }

    /// <summary>
    /// The engine keeps an extension's files open until it has let go of it, so a folder may refuse to go the moment it is
    /// removed. Whatever folder no entry owns is cleared here, after the engine is done and again at start-up.
    /// </summary>
    public void Sweep()
    {
        try
        {
            if (!Directory.Exists(Folder)) return;
            var kept = file.Items.Select(e => Path.GetFullPath(PathOf(e)).TrimEnd(Path.DirectorySeparatorChar)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var d in Directory.GetDirectories(Folder))
                if (!kept.Contains(Path.GetFullPath(d).TrimEnd(Path.DirectorySeparatorChar))) { try { Directory.Delete(d, true); } catch { } }
        }
        catch { }
    }
}
