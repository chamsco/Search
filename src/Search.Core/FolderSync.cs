using System.Text.Json;

namespace Search.Core;

public sealed class SyncConfig
{
    public bool On { get; set; }
    public string Folder { get; set; } = "";
}

public sealed record SyncSeen(string Name, DateTime Written);
public sealed record SyncOutcome(bool Ran, int Devices, int Applied, string? Problem, List<SyncSeen> Seen);

/// <summary>
/// Sync with your other PCs, through a folder you choose (Sync.swift): OneDrive, Dropbox, a shared disk. Search writes small files
/// into "Search Sync/&lt;scope&gt;/&lt;device&gt;.json", one per PC per scope, written only by that PC, and whatever moves the folder does the rest.
/// What syncs: spaces (name, colours, icon, order, whether it has sign-ins of its own), pins (page, name, folder among the pinned rows),
/// pin folders, bookmarks and bookmark folders of each profile, and the settings that follow you. Never history, passwords, cookies, open
/// tabs that aren't pinned, tab groups, the downloads folder or window sizes.
/// </summary>
public sealed class FolderSync(Prefs prefs, SpaceBook book, Func<Guid, Bookmarks> marksOf, Func<Guid, bool>? spaceBusy = null)
{
    public const string Dir = "Search Sync";
    const string ConfigFile = "sync.json", BaseFile = "sync-base.json", DeviceFile = "sync-device.json";

    public SyncConfig Config { get; private set; } = Store.Read<SyncConfig>(ConfigFile) ?? new();
    readonly string device = Device().Id;
    readonly string name = Device().Name;
    public event Action? Applied;

    sealed class DeviceInfo { public string Id { get; set; } = ""; public string Name { get; set; } = ""; }
    static DeviceInfo Device()
    {
        var d = Store.Read<DeviceInfo>(DeviceFile);
        if (d is { Id.Length: > 0 }) return d;
        d = new DeviceInfo { Id = Guid.NewGuid().ToString("N"), Name = Environment.MachineName };
        Store.Write(DeviceFile, d);
        return d;
    }

    public void Configure(bool on, string folder)
    {
        Config = new SyncConfig { On = on, Folder = folder ?? "" };
        Store.Write(ConfigFile, Config);
    }

    // MARK: - what each kind of thing is, as items

    sealed record SyncedSpace(string Name, int Colour, int Colour2, int? Icon, bool Shares, int Strength);
    sealed record SyncedPin(string Url, string Title, string? Name, string? Folder, bool Muted);
    sealed record SyncedFolder(string Name, string? Parent, string? Feed);
    sealed record SyncedMark(string Title, string Url, string? Folder);

    static string Text<T>(T v) => JsonSerializer.Serialize(v);
    static T? Value<T>(string text) { try { return JsonSerializer.Deserialize<T>(text); } catch (JsonException) { return default; } }

    sealed record Setting(string Key, Func<PrefsData, string> Read, Action<PrefsData, string> Write);
    static Setting Set<T>(string key, Func<PrefsData, T> get, Action<PrefsData, T> set) =>
        new(key, d => Text(get(d)), (d, text) => { if (Value<T>(text) is { } v && !EqualityComparer<T>.Default.Equals(get(d), v)) set(d, v); });

    static readonly Setting[] Settings =
    [
        Set("look", d => d.Look.ToString(), (d, v) => { if (Enum.TryParse<LookChoice>(v, out var e)) d.Look = e; }),
        Set("search.engine", d => d.Engine.ToString(), (d, v) => { if (Enum.TryParse<Engine>(v, out var e)) d.Engine = e; }),
        Set("search.custom", d => d.CustomEngine, (d, v) => d.CustomEngine = v),
        Set("search.suggest", d => d.EngineSuggests, (d, v) => d.EngineSuggests = v),
        Set("spaces", d => d.SpacesOn, (d, v) => d.SpacesOn = v),
        Set("tabs.layout", d => d.Layout.ToString(), (d, v) => { if (Enum.TryParse<TabsLayout>(v, out var e)) d.Layout = e; }),
        Set("pins.rows", d => d.PinsAsRows, (d, v) => d.PinsAsRows = v),
        Set("tabs.close.after", d => d.CloseIdleDays, (d, v) => d.CloseIdleDays = v),
        Set("bookmarks.bar", d => d.BookmarksBar, (d, v) => d.BookmarksBar = v),
        Set("privacy.https", d => d.HttpsUpgrade, (d, v) => d.HttpsUpgrade = v),
        Set("privacy.trackers", d => d.BlockTrackers, (d, v) => d.BlockTrackers = v),
        Set("search.site", d => d.SiteSearch, (d, v) => d.SiteSearch = v),
    ];

    public static IReadOnlyList<string> SettingKeys => Settings.Select(s => s.Key).ToList();

    public static string AppScope => "app";
    public static string ProfileScope(Guid id) => "profile-" + id.ToString("N");

    /// <summary>Items this build can read: a newer PC's (an unknown key, a damaged text) are carried along and never turned into tombstones.</summary>
    static bool Understood(string key, string text)
    {
        if (key.StartsWith("set:", StringComparison.Ordinal)) return Settings.Any(s => "set:" + s.Key == key);
        return key.Split(':')[0] is "bm" or "bmfolder" or "space" or "pin" or "pinfolder" or "order" or "pinorder";
    }

    // MARK: - gather

    public Dictionary<string, string> Gather(string scope)
    {
        var o = new Dictionary<string, string>();
        if (scope == AppScope)
        {
            foreach (var s in Settings) o["set:" + s.Key] = s.Read(prefs.Data);
            return o;
        }
        var profile = ProfileOf(scope);
        if (profile is null) return o;
        var marks = marksOf(profile.Value);
        foreach (var f in marks.Folders) o["bmfolder:" + f.Id.ToString("N")] = Text(f.Name);
        foreach (var b in marks.Items) o["bm:" + b.Id.ToString("N")] = Text(new SyncedMark(b.Title, b.Url, b.Folder?.ToString("N")));
        o["order:bm"] = Text(marks.Items.Select(b => b.Id.ToString("N")));
        var spaces = book.In(profile.Value).ToList();
        foreach (var s in spaces)
        {
            var sid = s.Id.ToString("N");
            o["space:" + sid] = Text(new SyncedSpace(s.Name, s.Colour, s.Colour2, s.Icon, s.SharesSignIns, (int)s.Strength));
            var shape = Session.Load(s.Id);
            if (shape is null) continue;
            foreach (var t in shape.Tabs.Where(t => t.PinId is not null))
                o[$"pin:{sid}:{t.PinId:N}"] = Text(new SyncedPin(t.Home ?? t.Url, t.Title, t.Name, t.Folder?.ToString("N"), t.Muted));
            o["pinorder:" + sid] = Text(shape.Tabs.Where(t => t.PinId is not null).Select(t => t.PinId!.Value.ToString("N")));
            foreach (var f in shape.Folders) o[$"pinfolder:{sid}:{f.Id:N}"] = Text(new SyncedFolder(f.Name, f.Parent?.ToString("N"), f.Feed));
        }
        o["order:spaces"] = Text(spaces.Select(s => s.Id.ToString("N")));
        return o;
    }

    Guid? ProfileOf(string scope) =>
        scope.StartsWith("profile-", StringComparison.Ordinal) && Guid.TryParseExact(scope[8..], "N", out var g) && book.Profiles.Any(p => p.Id == g) ? g : null;

    // MARK: - apply

    /// <summary>What the folder now says, made so here. Only what differs is touched.</summary>
    public int Apply(string scope, Dictionary<string, string> live)
    {
        var changed = 0;
        if (scope == AppScope)
        {
            var differs = Settings.Any(s => live.TryGetValue("set:" + s.Key, out var t) && t != s.Read(prefs.Data));
            if (differs)
            {
                prefs.Update(d => { foreach (var s in Settings) if (live.TryGetValue("set:" + s.Key, out var t)) s.Write(d, t); });
                changed++;
            }
            return changed;
        }
        if (ProfileOf(scope) is not { } profile) return 0;
        changed += ApplyBookmarks(profile, live);
        changed += ApplySpaces(profile, live);
        return changed;
    }

    int ApplyBookmarks(Guid profile, Dictionary<string, string> live)
    {
        var marks = marksOf(profile);
        var folders = new Dictionary<string, BookmarkFolder>();
        foreach (var (k, v) in live.Where(kv => kv.Key.StartsWith("bmfolder:", StringComparison.Ordinal)))
            if (Guid.TryParseExact(k[9..], "N", out var id) && Value<string>(v) is { } n) folders[k[9..]] = new BookmarkFolder { Id = id, Name = n };
        var items = new Dictionary<string, Bookmark>();
        var current = marks.Items.ToDictionary(b => b.Id.ToString("N"));
        foreach (var (k, v) in live.Where(kv => kv.Key.StartsWith("bm:", StringComparison.Ordinal)))
            if (Guid.TryParseExact(k[3..], "N", out var id) && Value<SyncedMark>(v) is { } m)
                items[k[3..]] = new Bookmark
                {
                    Id = id, Title = m.Title, Url = m.Url,
                    Folder = m.Folder is not null && folders.ContainsKey(m.Folder) ? Guid.Parse(m.Folder) : null,
                    Added = current.TryGetValue(k[3..], out var had) ? had.Added : DateTime.UtcNow,
                };
        var order = live.TryGetValue("order:bm", out var o) ? Value<List<string>>(o) ?? [] : [];
        var ordered = SyncMerge.Ordered(items.Keys, order).Select(id => items[id]).ToList();
        var folderList = folders.Values.OrderBy(f => marks.Folders.ToList().FindIndex(x => x.Id == f.Id) is var i && i < 0 ? int.MaxValue : i).ThenBy(f => f.Name, StringComparer.Ordinal).ToList();
        var same = ordered.Select(b => (b.Id, b.Title, b.Url, b.Folder)).SequenceEqual(marks.Items.Select(b => (b.Id, b.Title, b.Url, b.Folder)))
            && folderList.Select(f => (f.Id, f.Name)).SequenceEqual(marks.Folders.Select(f => (f.Id, f.Name)));
        if (same) return 0;
        marks.Replace(ordered, folderList);
        return 1;
    }

    int ApplySpaces(Guid profile, Dictionary<string, string> live)
    {
        var changed = 0;
        var wanted = new Dictionary<Guid, SyncedSpace>();
        foreach (var (k, v) in live.Where(kv => kv.Key.StartsWith("space:", StringComparison.Ordinal)))
            if (Guid.TryParseExact(k[6..], "N", out var id) && Value<SyncedSpace>(v) is { } s) wanted[id] = s;
        foreach (var (id, s) in wanted)
        {
            var have = book.Spaces.FirstOrDefault(x => x.Id == id);
            if (have is null)
            {
                book.Spaces.Add(new Space { Id = id, Name = s.Name, Colour = s.Colour, Colour2 = s.Colour2, Icon = s.Icon, SharesSignIns = s.Shares, Strength = (SpaceStrength)s.Strength, Profile = profile == Spaces.PersonalId ? null : profile });
                changed++;
            }
            else if (have.ProfileId == profile && (have.Name, have.Colour, have.Colour2, have.Icon, have.SharesSignIns, (int)have.Strength) != (s.Name, s.Colour, s.Colour2, s.Icon, s.Shares, s.Strength))
            {
                have.Name = s.Name; have.Colour = s.Colour; have.Colour2 = s.Colour2; have.Icon = s.Icon; have.Strength = (SpaceStrength)s.Strength;
                if (!have.IsFirst) have.SharesSignIns = s.Shares;
                changed++;
            }
        }
        // A space deleted elsewhere is deleted here, unless it is one this PC has never agreed on (then it is simply new here).
        foreach (var gone in book.In(profile).Where(x => !wanted.ContainsKey(x.Id) && !x.IsFirst && BaseHas(ProfileScope(profile), "space:" + x.Id.ToString("N"))).ToList())
            if (book.DeleteSpace(gone.Id)) changed++;
        var order = live.TryGetValue("order:spaces", out var o) ? Value<List<string>>(o) ?? [] : [];
        var mine = book.In(profile).Select(s => s.Id.ToString("N")).ToList();
        var want = SyncMerge.Ordered(mine, order);
        if (!want.SequenceEqual(mine))
        {
            var slots = book.Spaces.Select((s, i) => (s, i)).Where(t => t.s.ProfileId == profile).Select(t => t.i).ToList();
            var byId = book.Spaces.ToDictionary(s => s.Id.ToString("N"));
            for (var i = 0; i < slots.Count; i++) book.Spaces[slots[i]] = byId[want[i]];
            changed++;
        }
        foreach (var space in book.In(profile).ToList()) changed += ApplyPins(space, live);
        if (changed > 0) book.Save();
        return changed;
    }

    int ApplyPins(Space space, Dictionary<string, string> live)
    {
        var sid = space.Id.ToString("N");
        if (spaceBusy?.Invoke(space.Id) == true) return 0; // open in a window: its own tabs are the truth until it is left
        var pins = new Dictionary<string, SyncedPin>();
        foreach (var (k, v) in live.Where(kv => kv.Key.StartsWith($"pin:{sid}:", StringComparison.Ordinal)))
            if (Value<SyncedPin>(v) is { } p) pins[k[(5 + sid.Length)..]] = p;
        var folders = new Dictionary<string, SyncedFolder>();
        foreach (var (k, v) in live.Where(kv => kv.Key.StartsWith($"pinfolder:{sid}:", StringComparison.Ordinal)))
            if (Value<SyncedFolder>(v) is { } f) folders[k[(11 + sid.Length)..]] = f;
        var shape = Session.Load(space.Id) ?? new SessionShape();
        var order = live.TryGetValue("pinorder:" + sid, out var o) ? Value<List<string>>(o) ?? [] : [];
        var existing = shape.Tabs.Where(t => t.PinId is not null).ToDictionary(t => t.PinId!.Value.ToString("N"));
        var rows = new List<SessionEntry>();
        foreach (var id in SyncMerge.Ordered(pins.Keys, order))
        {
            var p = pins[id];
            var e = existing.TryGetValue(id, out var had) ? had : new SessionEntry { PinId = Guid.Parse(id), Url = p.Url };
            e.Home = p.Url; e.Name = p.Name; e.Muted = p.Muted;
            if (e.Title.Length == 0) e.Title = p.Title;
            e.Folder = p.Folder is not null && folders.ContainsKey(p.Folder) ? Guid.Parse(p.Folder) : null;
            rows.Add(e);
        }
        var foldersNow = folders.Select(kv => new PinFolder { Id = Guid.Parse(kv.Key), Name = kv.Value.Name, Parent = kv.Value.Parent is not null && folders.ContainsKey(kv.Value.Parent) ? Guid.Parse(kv.Value.Parent) : null, Feed = kv.Value.Feed,
            Collapsed = shape.Folders.FirstOrDefault(f => f.Id.ToString("N") == kv.Key)?.Collapsed ?? false,
            Items = shape.Folders.FirstOrDefault(f => f.Id.ToString("N") == kv.Key)?.Items }).OrderBy(f => f.Id).ToList();
        bool Same()
        {
            var cur = shape.Tabs.Where(t => t.PinId is not null).ToList();
            return cur.Select(t => (t.PinId, t.Home, t.Name, t.Muted, t.Folder)).SequenceEqual(rows.Select(t => (t.PinId, t.Home, t.Name, t.Muted, t.Folder)))
                && shape.Folders.OrderBy(f => f.Id).Select(f => (f.Id, f.Name, f.Parent, f.Feed)).SequenceEqual(foldersNow.Select(f => (f.Id, f.Name, f.Parent, f.Feed)));
        }
        if (pins.Count == 0 && folders.Count == 0 && existing.Count == 0 && shape.Folders.Count == 0) return 0;
        if (Same()) return 0;
        var active = shape.Active >= 0 && shape.Active < shape.Tabs.Count ? shape.Tabs[shape.Active] : null;
        var rest = shape.Tabs.Where(t => t.PinId is null).ToList();
        shape.Tabs = [.. rows, .. rest];
        shape.Folders = foldersNow;
        shape.Active = active is not null && shape.Tabs.IndexOf(active) is var ai and >= 0 ? ai : 0;
        Session.Save(shape, space.Id);
        return 1;
    }

    // MARK: - one pass

    Dictionary<string, Dictionary<string, SyncEntry>> Bases() => Store.Read<Dictionary<string, Dictionary<string, SyncEntry>>>(BaseFile) ?? [];
    bool BaseHas(string scope, string key) => Bases().TryGetValue(scope, out var b) && b.TryGetValue(key, out var e) && e.V is not null;

    /// <summary>Reads the other PCs' files, merges with this PC's, applies what changed here and writes this PC's file. Cheap when nothing moved.</summary>
    public SyncOutcome Run(double? at = null)
    {
        if (!Config.On || Config.Folder.Length == 0) return new(false, 0, 0, null, []);
        if (!Directory.Exists(Config.Folder)) return new(false, 0, 0, "The sync folder isn't there: " + Config.Folder, []);
        var now = at ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
        var bases = Bases();
        var applied = 0;
        var seen = new Dictionary<string, SyncSeen>();
        try
        {
            var scopes = new List<string> { AppScope }.Concat(book.Profiles.Select(p => ProfileScope(p.Id))).ToList();
            foreach (var scope in scopes)
            {
                var dir = Path.Combine(Config.Folder, Dir, scope);
                var theirs = new List<Dictionary<string, SyncEntry>>();
                if (Directory.Exists(dir))
                    foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
                    {
                        if (Path.GetFileNameWithoutExtension(file) == device) continue;
                        try
                        {
                            if (new FileInfo(file).Length > 32 << 20 || SyncMerge.Decode(File.ReadAllText(file), scope) is not { } f || f.Device == device) continue;
                            theirs.Add(f.Items);
                            seen[f.Device] = new SyncSeen(f.Name ?? f.Device[..Math.Min(8, f.Device.Length)], DateTime.UnixEpoch.AddSeconds(f.Written));
                        }
                        catch (IOException) { }
                    }
                var current = Gather(scope);
                bases.TryGetValue(scope, out var baseMap);
                var merged = SyncMerge.Pass(current, baseMap, theirs, device, now, Understood);
                var live = SyncMerge.Live(merged);
                applied += Apply(scope, live);
                // What is now here is the new base; this PC's file is that whole merged view.
                bases[scope] = merged;
                var mine = Path.Combine(dir, device + ".json");
                var text = SyncMerge.Encode(new SyncFile { Format = SyncMerge.Format, Device = device, Name = name, Scope = scope, Written = now, Items = merged });
                Directory.CreateDirectory(dir);
                if (!File.Exists(mine) || !SameItems(File.ReadAllText(mine), scope, merged)) File.WriteAllText(mine, text);
            }
            Store.Write(BaseFile, bases);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return new(false, seen.Count, applied, "The sync folder couldn't be used: " + e.Message, seen.Values.ToList());
        }
        if (applied > 0) Applied?.Invoke();
        return new(true, seen.Count, applied, null, seen.Values.ToList());
    }

    static bool SameItems(string text, string scope, Dictionary<string, SyncEntry> items)
    {
        var f = SyncMerge.Decode(text, scope);
        return f is not null && f.Items.Count == items.Count && items.All(kv => f.Items.TryGetValue(kv.Key, out var e) && e.V == kv.Value.V && e.T == kv.Value.T && e.D == kv.Value.D);
    }
}
