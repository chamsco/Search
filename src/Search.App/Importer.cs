using System.IO;
using System.Security.Cryptography;
using Search.Core;
using Search.Core.Import;

namespace Search.App;

/// <summary>Another browser on this PC whose data can come here (Import.swift's sources): its profile folder, and what it keeps.</summary>
public sealed record ImportSource(string Id, string Browser, string Name, string Dir, bool Firefox)
{
    public string Label => Name.Length > 0 && Name != "Default" ? $"{Browser} · {Name}" : Browser;
}

public sealed record ImportSummary(int Bookmarks, int Visits, int Logins, List<string> Notes);

public static class Importer
{
    // The test run points these at a folder of its own; they mean nothing otherwise.
    static string Local => Environment.GetEnvironmentVariable("SEARCH_IMPORT_LOCALAPPDATA") ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    static string Roaming => Environment.GetEnvironmentVariable("SEARCH_IMPORT_APPDATA") ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

    public static List<ImportSource> Scan()
    {
        var found = new List<ImportSource>();
        void Chromium(string browser, string userData)
        {
            if (!Directory.Exists(userData)) return;
            var names = ProfileNames(userData);
            foreach (var dir in Directory.EnumerateDirectories(userData).Where(d => Path.GetFileName(d) is "Default" || Path.GetFileName(d).StartsWith("Profile ", StringComparison.Ordinal)).Order())
            {
                if (!File.Exists(Path.Combine(dir, "Bookmarks")) && !File.Exists(Path.Combine(dir, "History")) && !File.Exists(Path.Combine(dir, "Login Data"))) continue;
                var folder = Path.GetFileName(dir);
                found.Add(new ImportSource($"{browser}|{dir}", browser, names.GetValueOrDefault(folder, folder), dir, false));
            }
        }
        Chromium("Chrome", Path.Combine(Local, "Google", "Chrome", "User Data"));
        Chromium("Edge", Path.Combine(Local, "Microsoft", "Edge", "User Data"));
        Chromium("Brave", Path.Combine(Local, "BraveSoftware", "Brave-Browser", "User Data"));
        try
        {
            var packages = Path.Combine(Local, "Packages");
            if (Directory.Exists(packages))
                foreach (var pkg in Directory.EnumerateDirectories(packages, "TheBrowserCompany.Arc_*"))
                    Chromium("Arc", Path.Combine(pkg, "LocalCache", "Local", "Arc", "User Data"));
        }
        catch { }
        void Gecko(string browser, string profiles)
        {
            if (!Directory.Exists(profiles)) return;
            foreach (var dir in Directory.EnumerateDirectories(profiles).Order())
                if (File.Exists(Path.Combine(dir, "places.sqlite")) || File.Exists(Path.Combine(dir, "logins.json")))
                {
                    var name = Path.GetFileName(dir); var dot = name.IndexOf('.');
                    found.Add(new ImportSource($"{browser}|{dir}", browser, dot > 0 ? name[(dot + 1)..] : name, dir, true));
                }
        }
        Gecko("Firefox", Path.Combine(Roaming, "Mozilla", "Firefox", "Profiles"));
        Gecko("Zen", Path.Combine(Roaming, "zen", "Profiles"));
        return found;
    }

    static Dictionary<string, string> ProfileNames(string userData)
    {
        var names = new Dictionary<string, string>();
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(userData, "Local State")));
            foreach (var p in doc.RootElement.GetProperty("profile").GetProperty("info_cache").EnumerateObject())
                if (p.Value.TryGetProperty("name", out var n) && n.GetString() is { Length: > 0 } s) names[p.Name] = s;
        }
        catch { }
        return names;
    }

    /// <summary>Read what the person asked for, and bring it in: bookmarks into folders of their own names, history merged, sign-ins into the vault.</summary>
    public static ImportSummary Run(ImportSource source, bool bookmarks, bool history, bool passwords, Bookmarks marks, History visits, PasswordVault vault)
    {
        var notes = new List<string>();
        var read = new ImportRead();
        string P(string name) => Path.Combine(source.Dir, name);
        try
        {
            if (bookmarks)
            {
                if (source.Firefox) { if (File.Exists(P("places.sqlite"))) read.Bookmarks.AddRange(FirefoxImport.Bookmarks(P("places.sqlite"))); }
                else if (File.Exists(P("Bookmarks"))) read.Bookmarks.AddRange(ChromiumImport.Bookmarks(File.ReadAllText(P("Bookmarks"))));
            }
            if (history)
            {
                if (source.Firefox) { if (File.Exists(P("places.sqlite"))) read.Visits.AddRange(FirefoxImport.History(P("places.sqlite"))); }
                else if (File.Exists(P("History"))) read.Visits.AddRange(ChromiumImport.History(P("History")));
            }
            if (passwords) ReadLogins(source, read, notes);
        }
        catch (Exception e) { notes.Add($"{source.Browser} couldn't be read all the way: {e.Message}"); App.Log(e); }

        var added = 0;
        var folders = marks.Folders.ToDictionary(f => f.Name, f => f.Id, StringComparer.OrdinalIgnoreCase);
        foreach (var b in read.Bookmarks)
        {
            if (marks.Contains(b.Url)) continue;
            Guid? folder = null;
            if (b.Folder is { Length: > 0 } fn)
            {
                if (!folders.TryGetValue(fn, out var id)) { id = marks.AddFolder(fn).Id; folders[fn] = id; }
                folder = id;
            }
            marks.Add(b.Url, b.Title, folder); added++;
        }
        var newVisits = 0;
        foreach (var v in read.Visits) if (visits.Merge(v.Url, v.Title, v.Count, v.Last)) newVisits++;
        if (read.Visits.Count > 0) visits.Save();
        var kept = 0;
        foreach (var l in read.Logins)
        {
            if (!Uri.TryCreate(l.Url, UriKind.Absolute, out var u)) continue;
            var host = u.Host.ToLowerInvariant();
            if (vault.KeptFor(host).Any(k => k.User == l.User)) continue; // what is kept here stays
            if (vault.Save(host, l.User, l.Password, null, u.Scheme == "http")) kept++;
        }
        return new ImportSummary(added, newVisits, kept, notes);
    }

    static void ReadLogins(ImportSource source, ImportRead read, List<string> notes)
    {
        if (source.Firefox)
        {
            var (result, fxLogins) = FirefoxImport.Logins(source.Dir);
            read.Logins.AddRange(fxLogins);
            if (result == FirefoxImport.LoginsResult.NeedsPrimaryPassword) notes.Add($"{source.Browser}'s passwords are locked with a primary password: take it off there to bring them over.");
            else if (result == FirefoxImport.LoginsResult.Unreadable) notes.Add($"{source.Browser}'s password store couldn't be read.");
            return;
        }
        var userData = Path.GetDirectoryName(source.Dir)!;
        var db = Path.Combine(source.Dir, "Login Data");
        if (!File.Exists(db)) return;
        var wrapped = File.Exists(Path.Combine(userData, "Local State")) ? ChromiumImport.WrappedKey(File.ReadAllText(Path.Combine(userData, "Local State"))) : null;
        if (wrapped is null) { notes.Add($"{source.Browser}'s password key was not found."); return; }
        byte[] key;
        try { key = ProtectedData.Unprotect(wrapped, null, DataProtectionScope.CurrentUser); }
        catch { notes.Add($"{source.Browser}'s password key couldn't be opened by this Windows account."); return; }
        var (logins, unreadable) = ChromiumImport.Logins(db, key);
        read.Logins.AddRange(logins);
        if (unreadable > 0) notes.Add($"{unreadable} password{(unreadable == 1 ? "" : "s")} in {source.Browser} use newer protection (app-bound) and couldn't be read.");
    }
}
