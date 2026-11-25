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

public sealed record ImportSummary(int Bookmarks, int Visits, int Logins, List<string> Notes)
{
    public int Spaces { get; init; }
    public int Pins { get; init; }
}

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
        var (read, notes) = Read(source, bookmarks, history, passwords);
        return Apply(read, notes, marks, visits, vault);
    }

    /// <summary>The slow half, safe off the UI thread: the other browser's files, read.</summary>
    public static (ImportRead Read, List<string> Notes) Read(ImportSource source, bool bookmarks, bool history, bool passwords)
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
        return (read, notes);
    }

    /// <summary>The quick half, on the UI thread, where the bookmarks and history they feed are listened to.</summary>
    public static ImportSummary Apply(ImportRead read, List<string> notes, Bookmarks marks, History visits, PasswordVault vault)
    {
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

    /// <summary>Arc's or Zen's sidebar, if this source has one: spaces and the pages pinned in them.</summary>
    public static Sidebar? ReadSidebar(ImportSource source)
    {
        try
        {
            if (source.Browser == "Zen") return ZenSidebar.Read(source.Dir);
            if (source.Browser != "Arc") return null;
            var userData = Path.GetDirectoryName(source.Dir)!;
            foreach (var dir in new[] { Path.GetDirectoryName(userData)!, userData })
                if (ArcSidebar.Read(Path.Combine(dir, "StorableSidebar.json")) is { } s) return s;
        }
        catch (Exception e) { App.Log(e); }
        return null;
    }

    /// <summary>Each space becomes a space here, its pinned pages and folders pinned in it, and the favourites pinned in every one of them.
    /// A space that is already here by that name is left alone.</summary>
    public static ImportSummary Spaces(ImportSource source, SpaceBook book, Guid profile)
    {
        var notes = new List<string>();
        var side = ReadSidebar(source);
        if (side is null) { notes.Add($"{source.Browser}'s sidebar wasn't found or couldn't be read."); return new ImportSummary(0, 0, 0, notes); }
        int spaces = 0, pins = 0;
        var palette = Core.Spaces.Colours.Length;
        foreach (var sp in side.Spaces)
        {
            if (book.In(profile).Any(x => string.Equals(x.Name, sp.Name, StringComparison.OrdinalIgnoreCase))) { notes.Add($"“{sp.Name}” is already a space here."); continue; }
            var made = book.AddSpace(profile, sp.Name, Math.Clamp(sp.Colour, 0, palette - 1));
            book.UpdateSpace(made.Id, x => { x.Icon = sp.Icon; x.Colour2 = Math.Clamp(sp.Colour2, 0, palette - 1); });
            var shape = new SessionShape();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            void Pin(SidebarItem i, Guid? folder)
            {
                if (!seen.Add(i.Url)) return;
                shape.Tabs.Add(new SessionEntry { Url = i.Url, Title = i.Title, PinId = Guid.NewGuid(), Home = i.Url, Folder = folder });
                pins++;
            }
            foreach (var f in side.Favorites) Pin(f, null);
            void Walk(List<SidebarNode> nodes, Guid? parent)
            {
                foreach (var n in nodes)
                {
                    if (!n.IsFolder) { Pin(n.Item!, parent); continue; }
                    var folder = new PinFolder { Name = n.FolderTitle ?? "Folder", Parent = parent };
                    shape.Folders.Add(folder);
                    Walk(n.Children, folder.Id);
                }
            }
            Walk(sp.Pinned, null);
            if (shape.Tabs.Count > 0) Session.Save(shape, made.Id);
            spaces++;
        }
        if (side.Spaces.Any(x => x.Pinned.Any(n => n.IsFolder))) Shared.Prefs.Update(d => d.PinsAsRows = true);
        return new ImportSummary(0, 0, 0, notes) { Spaces = spaces, Pins = pins };
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
