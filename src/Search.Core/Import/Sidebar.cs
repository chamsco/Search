using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Data.Sqlite;

namespace Search.Core.Import;

/// <summary>A pinned page.</summary>
public sealed record SidebarItem(string Title, string Url);

/// <summary>A pinned page or a folder of them (ImportArc.swift's Node).</summary>
public sealed class SidebarNode
{
    public SidebarItem? Item { get; init; }
    public string? FolderTitle { get; init; }
    public List<SidebarNode> Children { get; init; } = [];
    public bool IsFolder => Item is null;
    public static SidebarNode Page(SidebarItem i) => new() { Item = i };
    public static SidebarNode Folder(string title, List<SidebarNode> inside) => new() { FolderTitle = title, Children = inside };
    public int Count => IsFolder ? Children.Sum(c => c.Count) : 1;
}

public sealed class SidebarSpace
{
    public string Name { get; set; } = "Space";
    /// <summary>An index into Spaces.IconNames.</summary>
    public int Icon { get; set; }
    /// <summary>An index into Spaces.Colours; 0 for none.</summary>
    public int Colour { get; set; }
    public int Colour2 { get; set; }
    /// <summary>The Chromium profile folder the space signs in with ("Default", "Profile 1").</summary>
    public string Profile { get; set; } = "Default";
    public List<SidebarNode> Pinned { get; set; } = [];
}

/// <summary>A browser's spaces and what is pinned in each, and the favourites above them (Arc's, Zen's Essentials).</summary>
public sealed class Sidebar
{
    public List<SidebarItem> Favorites { get; set; } = [];
    public List<SidebarSpace> Spaces { get; set; } = [];
    public int PinnedCount => Spaces.Sum(s => s.Pinned.Sum(n => n.Count)) + Favorites.Count;
}

/// <summary>Arc's sidebar file: spaces, and items (a tab, a folder, or a container of items) in flat lists, joined by parentID and childrenIds (ImportArc.swift).</summary>
public static class ArcSidebar
{
    public const int MaxBytes = 32 << 20;

    public static Sidebar? Read(string file)
    {
        try
        {
            var info = new FileInfo(file);
            if (!info.Exists || info.Length > MaxBytes) return null;
            return Parse(File.ReadAllText(file));
        }
        catch { return null; }
    }

    public static Sidebar? Parse(string json)
    {
        JsonNode? top;
        try { top = JsonNode.Parse(json); } catch { return null; }
        var containers = top?["sidebar"]?["containers"] as JsonArray;
        var main = containers?.OfType<JsonObject>().FirstOrDefault(c => c["spaces"] is not null);
        if (main is null) return null;
        var items = new Dictionary<string, JsonObject>();
        foreach (var item in (main["items"] as JsonArray)?.OfType<JsonObject>() ?? []) if (item["id"]?.GetValue<string>() is { } id) items[id] = item;

        var taken = new HashSet<string>();
        var total = 0;
        List<SidebarNode> Nodes(string id, int depth)
        {
            var found = new List<SidebarNode>();
            if (depth >= 64 || !items.TryGetValue(id, out var parent) || parent["childrenIds"] is not JsonArray kids) return found;
            foreach (var kid in kids)
            {
                if (kid is not JsonValue v || !v.TryGetValue<string>(out var childId) || total >= 5000 || !taken.Add(childId) || !items.TryGetValue(childId, out var item) || item["data"] is not JsonObject data) continue;
                if (data["tab"] is JsonObject tab)
                {
                    if (SidebarText.Page(tab["savedURL"]?.GetValue<string>()) is not { } url) continue;
                    total++;
                    var title = SidebarText.Named(item["title"]?.GetValue<string>()) ?? SidebarText.Named(tab["savedTitle"]?.GetValue<string>()) ?? new Uri(url).Host;
                    found.Add(SidebarNode.Page(new SidebarItem(title, url)));
                }
                else if (data["list"] is not null)
                {
                    var inside = Nodes(childId, depth + 1);
                    if (inside.Count > 0) found.Add(SidebarNode.Folder(SidebarText.Named(item["title"]?.GetValue<string>()) ?? "Folder", inside));
                }
            }
            return found;
        }

        var result = new Sidebar();
        foreach (var space in (main["spaces"] as JsonArray)?.OfType<JsonObject>() ?? [])
        {
            var name = SidebarText.Named(space["title"]?.GetValue<string>()) ?? "Space";
            var icon = space["customInfo"]?["iconType"] as JsonObject;
            var pinned = Container("pinned", space["newContainerIDs"] as JsonArray) ?? Container("pinned", space["containerIDs"] as JsonArray);
            result.Spaces.Add(new SidebarSpace { Name = name, Profile = ProfileName(space["profile"]), Icon = SidebarText.IconFor(icon?["emoji_v2"]?.GetValue<string>(), name), Pinned = pinned is null ? [] : Nodes(pinned, 0) });
        }
        // The favourites: a profile followed by its container's id.
        var tops = main["topAppsContainerIDs"] as JsonArray ?? [];
        for (var i = 0; i < tops.Count; i++)
        {
            if (tops[i] is JsonValue sv && sv.TryGetValue<string>(out _)) continue;
            if (i + 1 >= tops.Count || tops[i + 1] is not JsonValue idv || !idv.TryGetValue<string>(out var cid)) continue;
            foreach (var n in Nodes(cid, 0)) if (!n.IsFolder && result.Favorites.All(f => f.Url != n.Item!.Url)) result.Favorites.Add(n.Item!);
        }
        return result;
    }

    static string? Container(string label, JsonArray? list)
    {
        if (list is null) return null;
        for (var i = 0; i + 1 < list.Count; i++)
        {
            var matches = (list[i] is JsonValue v && v.TryGetValue<string>(out var s) && s == label) || (list[i] is JsonObject o && o[label] is not null);
            if (matches && list[i + 1] is JsonValue idv && idv.TryGetValue<string>(out var id)) return id;
        }
        return null;
    }

    /// <summary>"Default", or the folder of the profile a space belongs to: {"default": {}} or {"custom": {"_0": {"directoryBasename": …}}}.</summary>
    static string ProfileName(JsonNode? value)
    {
        if (value?["custom"] is not JsonObject custom) return "Default";
        var inner = custom["_0"] as JsonObject ?? custom;
        return inner["directoryBasename"]?.GetValue<string>() is { Length: > 0 } s ? s : "Default";
    }
}

/// <summary>What Zen keeps of its spaces (it calls them workspaces), pins, folders and Essentials: in zen-sessions.jsonlz4, before that in tables of places.sqlite, before those in the session store (ImportZen.swift).</summary>
public static class ZenSidebar
{
    public static Sidebar? Read(string profileDir)
    {
        var backups = Path.Combine(profileDir, "zen-sessions-backup");
        var dated = Directory.Exists(backups)
            ? Directory.EnumerateFiles(backups, "zen-sessions-*.jsonlz4").OrderByDescending(f => f, StringComparer.Ordinal).Take(5)
            : [];
        foreach (var file in new[] { Path.Combine(profileDir, "zen-sessions.jsonlz4"), Path.Combine(backups, "clean.jsonlz4"), Path.Combine(backups, "recovery.baklz4") }.Concat(dated))
            if (MozLz4.Json(file) is { } top && Parse(top) is { Spaces.Count: > 0 } found) return found;
        if (FromPlaces(Path.Combine(profileDir, "places.sqlite")) is { Spaces.Count: > 0 } old) return old;
        if (SessionWindow(profileDir) is { } window)
        {
            if ((window["spaces"] as JsonArray)?.Count is null or 0 && File.Exists(Path.Combine(profileDir, "zen-workspaces.json")))
            {
                try { window["spaces"] = JsonNode.Parse(File.ReadAllText(Path.Combine(profileDir, "zen-workspaces.json")))?["workspaces"]?.DeepClone(); } catch { }
            }
            if (Parse(window) is { Spaces.Count: > 0 } older) return older;
        }
        return null;
    }

    static JsonObject? SessionWindow(string dir)
    {
        var candidates = new[] { Path.Combine(dir, "sessionstore-backups", "recovery.jsonlz4"), Path.Combine(dir, "sessionstore.jsonlz4"), Path.Combine(dir, "sessionstore-backups", "recovery.baklz4") }
            .Where(File.Exists).OrderByDescending(File.GetLastWriteTimeUtc);
        foreach (var file in candidates)
            if (MozLz4.Json(file) is { } top)
                return (top["windows"] as JsonArray)?.OfType<JsonObject>().FirstOrDefault(w => w["isPopup"]?.GetValue<bool>() != true && w["isPrivate"]?.GetValue<bool>() != true && w["isTaskbarTab"]?.GetValue<bool>() != true);
        return null;
    }

    sealed class Fold { public string? Name; public string? Parent; public bool Split, Live; }

    sealed class Tree
    {
        public readonly Dictionary<string, List<(SidebarItem? Item, string? Folder)>> Roots = [], Children = [];
        public readonly Dictionary<string, string> Names = [];
        readonly HashSet<string> placed = [];
        public int Count;

        /// <summary>chain runs from the folder the page is in out to the top one.</summary>
        public void Place(SidebarItem item, List<string> chain, string space, Dictionary<string, string?> names)
        {
            if (Count >= 5000) return;
            Count++;
            (SidebarItem? Item, string? Folder) child = (item, null);
            foreach (var id in chain)
            {
                if (!Children.TryGetValue(id, out var list)) Children[id] = list = [];
                list.Add(child);
                if (names.TryGetValue(id, out var n) && n is not null) Names[id] = n;
                if (!placed.Add(id)) return;
                child = (null, id);
            }
            if (!Roots.TryGetValue(space, out var roots)) Roots[space] = roots = [];
            roots.Add(child);
        }

        public List<SidebarNode> Nodes(string space) => Build(Roots.GetValueOrDefault(space) ?? [], 0);

        List<SidebarNode> Build(List<(SidebarItem? Item, string? Folder)> entries, int depth)
        {
            var found = new List<SidebarNode>();
            if (depth >= 64) return found;
            foreach (var e in entries)
            {
                if (e.Item is not null) { found.Add(SidebarNode.Page(e.Item)); continue; }
                var inside = Build(Children.GetValueOrDefault(e.Folder!) ?? [], depth + 1);
                if (inside.Count > 0) found.Add(SidebarNode.Folder(SidebarText.Named(Names.GetValueOrDefault(e.Folder!)) ?? "Folder", inside));
            }
            return found;
        }
    }

    static bool Truthy(JsonNode? n) => n is JsonValue v && (v.TryGetValue<bool>(out var b) ? b : v.TryGetValue<string>(out var s) ? s == "true" : v.TryGetValue<double>(out var d) && d != 0);

    /// <summary>Zen's sidebar object, or a session window, which has the same fields: spaces, tabs (each pinned tab with the space it is in), folders.</summary>
    public static Sidebar? Parse(JsonObject top)
    {
        var raw = (top["spaces"] as JsonArray)?.OfType<JsonObject>().Take(200).ToList() ?? [];
        if (raw.Count == 0) return null;
        var order = new List<string>();
        var spaces = new Dictionary<string, SidebarSpace>();
        foreach (var r in raw)
        {
            if (r["uuid"]?.GetValue<string>() is not { } id || spaces.ContainsKey(id)) continue;
            order.Add(id);
            spaces[id] = MakeSpace(r["name"]?.GetValue<string>(), r["icon"]?.GetValue<string>(), r["theme"] as JsonObject);
        }
        if (order.Count == 0) return null;
        var first = order[0];
        var folders = new Dictionary<string, Fold>();
        foreach (var f in (top["folders"] as JsonArray)?.OfType<JsonObject>() ?? [])
            if (f["id"]?.GetValue<string>() is { } fid)
                folders[fid] = new Fold { Name = f["name"]?.GetValue<string>(), Parent = f["parentId"]?.GetValue<string>(), Split = Truthy(f["splitViewGroup"]), Live = Truthy(f["isLiveFolder"]) };
        var names = folders.ToDictionary(k => k.Key, k => k.Value.Name);
        var tree = new Tree();
        var essentials = new List<SidebarItem>();
        foreach (var tab in (top["tabs"] as JsonArray)?.OfType<JsonObject>() ?? [])
        {
            var essential = Truthy(tab["zenEssential"]);
            if (!(Truthy(tab["pinned"]) || essential) || Truthy(tab["zenIsGlance"]) || Truthy(tab["zenIsEmpty"])) continue;
            if (tab["zenLiveFolderItemId"]?.GetValue<string>() is { Length: > 0 }) continue;
            if (PinnedItem(tab) is not { } item) continue;
            if (essential)
            {
                if (essentials.All(e => e.Url != item.Url) && tree.Count < 5000) { essentials.Add(item); tree.Count++; }
                continue;
            }
            var workspace = tab["zenWorkspace"]?.GetValue<string>() ?? tab["attributes"]?["zen-workspace-id"]?.GetValue<string>();
            var space = workspace is not null && spaces.ContainsKey(workspace) ? workspace : first;
            var chain = new List<string>();
            var at = tab["groupId"]?.GetValue<string>();
            var live = false;
            while (at is not null && folders.TryGetValue(at, out var folder) && chain.Count < 64 && !chain.Contains(at))
            {
                if (folder.Live) live = true;
                if (!folder.Split) chain.Add(at);
                at = folder.Parent;
            }
            if (live) continue;
            tree.Place(item, chain, space, names);
        }
        var result = new Sidebar { Favorites = essentials };
        foreach (var id in order) { var s = spaces[id]; s.Pinned = tree.Nodes(id); result.Spaces.Add(s); }
        return result;
    }

    /// <summary>A pinned tab's page: the one it was pinned at, which Zen keeps apart from wherever the tab has since gone, else where it is now.</summary>
    static SidebarItem? PinnedItem(JsonObject tab)
    {
        var entries = (tab["entries"] as JsonArray)?.OfType<JsonObject>().ToList() ?? [];
        var index = tab["index"] is JsonValue iv && iv.TryGetValue<int>(out var i) ? Math.Min(Math.Max(i, 1), Math.Max(entries.Count, 1)) - 1 : entries.Count - 1;
        var current = index >= 0 && index < entries.Count ? entries[index] : null;
        var initial = tab["_zenPinnedInitialState"]?["entry"] as JsonObject;
        var url = SidebarText.Page(initial?["url"]?.GetValue<string>()) ?? SidebarText.Page(current?["url"]?.GetValue<string>());
        if (url is null) return null;
        var title = SidebarText.Named(tab["zenStaticLabel"]?.GetValue<string>()) ?? SidebarText.Named(initial?["title"]?.GetValue<string>()) ?? SidebarText.Named(current?["title"]?.GetValue<string>()) ?? new Uri(url).Host;
        return new SidebarItem(title, url);
    }

    static SidebarSpace MakeSpace(string? name, string? icon, JsonObject? theme)
    {
        var n = SidebarText.Named(name) ?? "Space";
        string? emoji = null, picture = null;
        icon = icon?.Trim();
        if (!string.IsNullOrEmpty(icon))
        {
            if (icon.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)) picture = Path.GetFileNameWithoutExtension(icon.Split('/').Last());
            else if (icon.Length <= 8) emoji = icon;
        }
        var space = new SidebarSpace { Name = n, Icon = SidebarText.IconFor(emoji, n, picture) };
        var colours = (theme?["gradientColors"] as JsonArray)?.OfType<JsonObject>().ToList() ?? [];
        var ordered = colours.Where(c => Truthy(c["isPrimary"])).Concat(colours.Where(c => !Truthy(c["isPrimary"]))).Select(c => Rgb(c["c"])).Where(c => c is not null).Select(c => c!.Value).ToList();
        if (ordered.Count > 0) space.Colour = Nearest(ordered[0]);
        if (ordered.Count > 1) space.Colour2 = Nearest(ordered[1]);
        return space;
    }

    /// <summary>A colour as Zen keeps a dot's: [r, g, b] out of 255, or "#rrggbb" or "rgb(r, g, b)".</summary>
    internal static (double R, double G, double B)? Rgb(JsonNode? value)
    {
        var parts = new List<double>();
        if (value is JsonArray a) foreach (var x in a.Take(3)) { if (x is JsonValue v && v.TryGetValue<double>(out var d)) parts.Add(d); }
        else if (value is JsonValue sv && sv.TryGetValue<string>(out var text))
        {
            text = text.Trim();
            if (text.StartsWith('#') && text.Length == 7 && int.TryParse(text[1..], System.Globalization.NumberStyles.HexNumber, null, out var n)) parts = [n >> 16 & 255, n >> 8 & 255, n & 255];
            else parts = System.Text.RegularExpressions.Regex.Matches(text, @"[0-9.]+").Select(m => m.Value).Take(3).Select(s => double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : double.NaN).ToList();
        }
        if (parts.Count != 3 || parts.Any(p => double.IsNaN(p) || p < 0 || p > 255)) return null;
        return (parts[0] / 255, parts[1] / 255, parts[2] / 255);
    }

    /// <summary>The nearest of Spaces.Colours by hue, or none for a grey: blue, green, orange, red and violet.</summary>
    internal static int Nearest((double R, double G, double B) c)
    {
        var high = Math.Max(c.R, Math.Max(c.G, c.B)); var low = Math.Min(c.R, Math.Min(c.G, c.B));
        if (high <= 0 || (high - low) / high < 0.2 || high - low < 0.08) return 0;
        var delta = high - low;
        double hue = high == c.R ? (c.G - c.B) / delta : high == c.G ? 2 + (c.B - c.R) / delta : 4 + (c.R - c.G) / delta;
        hue = hue * 60 % 360; if (hue < 0) hue += 360;
        (int Index, double Hue)[] hues = [(1, 218), (2, 146), (3, 33), (4, 353), (5, 270)];
        double Distance(double a) { var d = Math.Abs(a - hue); return Math.Min(d, 360 - d); }
        return hues.OrderBy(h => Distance(h.Hue)).First().Index;
    }

    // MARK: - places.sqlite, before window sync

    /// <summary>zen_workspaces and zen_pins, as Zen kept them until late 2025. Columns are read by name: they came one release at a time.</summary>
    public static Sidebar? FromPlaces(string file)
    {
        if (!File.Exists(file)) return null;
        Sidebar? found = null;
        try
        {
            ChromiumImport.WithCopy(file, copy =>
            {
                using var db = ChromiumImport.Open(copy);
                var spacesRows = Rows(db, "select * from zen_workspaces order by position asc, created_at asc");
                if (spacesRows.Count == 0) return;
                var order = new List<string>(); var spaces = new Dictionary<string, SidebarSpace>();
                foreach (var r in spacesRows.Take(200))
                {
                    if (r.GetValueOrDefault("uuid") as string is not { } id || spaces.ContainsKey(id)) continue;
                    order.Add(id);
                    JsonObject? theme = null;
                    if (r.GetValueOrDefault("theme_colors") is string colours && !string.IsNullOrEmpty(colours))
                    {
                        try { theme = new JsonObject { ["gradientColors"] = JsonNode.Parse(colours) }; } catch { }
                    }
                    spaces[id] = MakeSpace(r.GetValueOrDefault("name") as string, r.GetValueOrDefault("icon") as string, theme);
                }
                if (order.Count == 0) return;
                var pins = Rows(db, "select * from zen_pins order by position asc");
                var names = new Dictionary<string, string?>(); var parents = new Dictionary<string, string>();
                foreach (var p in pins.Where(p => Convert.ToInt64(p.GetValueOrDefault("is_group") ?? 0) != 0))
                {
                    if (p.GetValueOrDefault("uuid") as string is not { } id) continue;
                    names[id] = p.GetValueOrDefault("title") as string;
                    if (p.GetValueOrDefault("folder_parent_uuid") as string is { } par) parents[id] = par;
                }
                var tree = new Tree(); var essentials = new List<SidebarItem>();
                foreach (var p in pins.Where(p => Convert.ToInt64(p.GetValueOrDefault("is_group") ?? 0) == 0))
                {
                    if (SidebarText.Page(p.GetValueOrDefault("url") as string) is not { } url) continue;
                    var item = new SidebarItem(SidebarText.Named(p.GetValueOrDefault("title") as string) ?? new Uri(url).Host, url);
                    if (Convert.ToInt64(p.GetValueOrDefault("is_essential") ?? 0) != 0) { if (essentials.All(e => e.Url != url) && tree.Count < 5000) { essentials.Add(item); tree.Count++; } continue; }
                    var ws = p.GetValueOrDefault("workspace_uuid") as string;
                    var space = ws is not null && spaces.ContainsKey(ws) ? ws : order[0];
                    var chain = new List<string>(); var at = p.GetValueOrDefault("folder_parent_uuid") as string;
                    while (at is not null && names.ContainsKey(at) && chain.Count < 64 && !chain.Contains(at)) { chain.Add(at); at = parents.GetValueOrDefault(at); }
                    tree.Place(item, chain, space, names);
                }
                found = new Sidebar { Favorites = essentials };
                foreach (var id in order) { var s = spaces[id]; s.Pinned = tree.Nodes(id); found.Spaces.Add(s); }
            });
        }
        catch { return null; }
        return found;
    }

    static List<Dictionary<string, object?>> Rows(SqliteConnection db, string sql)
    {
        var rows = new List<Dictionary<string, object?>>();
        try
        {
            using var cmd = db.CreateCommand(); cmd.CommandText = sql;
            using var r = cmd.ExecuteReader();
            while (r.Read() && rows.Count < 10000)
            {
                var row = new Dictionary<string, object?>();
                for (var i = 0; i < r.FieldCount; i++) row[r.GetName(i)] = r.IsDBNull(i) ? null : r.GetValue(i);
                rows.Add(row);
            }
        }
        catch { }
        return rows;
    }
}

public static class SidebarText
{
    /// <summary>A web address, with any password in it left behind; null for anything but http and https.</summary>
    public static string? Page(string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || !Uri.TryCreate(text.Trim(), UriKind.Absolute, out var u) || u.Scheme is not ("http" or "https")) return null;
        var b = new UriBuilder(u) { UserName = "", Password = "" };
        return b.Uri.AbsoluteUri;
    }

    public static string? Named(string? text)
    {
        var t = text?.Trim();
        return string.IsNullOrEmpty(t) ? null : t.Length > 300 ? t[..300] : t;
    }

    static readonly (int Icon, string[] Emoji)[] ByEmoji =
    [
        (0, ["💼", "👔", "📊", "📈", "🗂"]), (1, ["🏢", "🏛", "🏙"]), (16, ["🏠", "🏡", "🛋"]), (2, ["🖥"]), (3, ["💻"]), (5, ["⌨", "🧑‍💻", "👨‍💻", "👩‍💻"]),
        (4, ["🧑‍🔧", "🛠", "🔧", "⚙"]), (6, ["✨", "🤖", "🪄", "⭐", "🌟"]), (7, ["🧠"]), (8, ["💡"]), (9, ["🎮", "🕹", "👾"]), (10, ["🏖", "🌴", "⛱", "🏝"]),
        (11, ["☕", "🍵", "🫖"]), (12, ["🎵", "🎶", "🎧", "🎸", "🎹"]), (13, ["🎬", "🍿", "📺", "🎥"]), (14, ["🎨", "🖌", "🖍", "✏"]), (15, ["📷", "📸"]),
        (17, ["📚", "📖", "📕", "📘", "📰"]), (18, ["🎓", "🏫", "✍"]), (19, ["🛒", "🛍", "💸", "💰", "💳"]), (20, ["✈", "🧳", "🌍", "🌎", "🌏", "🗺"]),
        (21, ["🏋", "💪", "🏃", "⚽", "🏀"]), (22, ["🌿", "🌱", "🍃", "🦋", "🌸", "🌳"]), (23, ["❤", "💖", "💕", "🥰", "😍"]),
    ];

    static readonly (int Icon, string[] Words)[] ByWord =
    [
        (0, ["work", "job", "office", "boulot", "travail", "client"]), (16, ["home", "perso", "personal", "maison", "family", "famille"]), (4, ["code", "dev", "github", "eng"]),
        (6, ["ai", "gpt", "claude", "llm"]), (18, ["school", "study", "studies", "course", "cours", "uni", "école"]), (17, ["read", "lecture", "news", "research"]),
        (14, ["design", "art", "creative", "figma"]), (12, ["music", "musique"]), (9, ["game", "gaming", "jeu"]), (19, ["shop", "achat", "money", "finance"]),
        (20, ["travel", "trip", "voyage"]), (21, ["sport", "gym", "fitness"]), (13, ["film", "movie", "video", "watch"]),
    ];

    static readonly Dictionary<string, int> ByZenPicture = new()
    {
        ["airplane"] = 20, ["globe"] = 20, ["map"] = 20, ["planet"] = 20, ["weight"] = 21, ["basket"] = 19, ["card"] = 19, ["wallet"] = 19, ["coins"] = 19, ["shirt"] = 19, ["present"] = 19,
        ["bed"] = 16, ["book"] = 17, ["bookmark"] = 17, ["page"] = 17, ["briefcase"] = 0, ["inbox"] = 0, ["mail"] = 0, ["stats-chart"] = 0, ["time"] = 0, ["brush"] = 14, ["palette"] = 14, ["shapes"] = 14,
        ["image"] = 15, ["bug"] = 4, ["code"] = 4, ["logo-github"] = 4, ["build"] = 4, ["construct"] = 4, ["terminal"] = 5, ["cafe"] = 11, ["cutlery"] = 11, ["fast-food"] = 11, ["pizza"] = 11,
        ["game-controller"] = 9, ["heart"] = 23, ["people"] = 23, ["leaf"] = 22, ["water"] = 22, ["paw"] = 22, ["fish"] = 22, ["lightning"] = 8, ["flame"] = 8, ["flask"] = 7, ["nuclear"] = 7,
        ["school"] = 18, ["music"] = 12, ["volume-high"] = 12, ["video"] = 13, ["ticket"] = 13, ["star"] = 6, ["tada"] = 6, ["rocket"] = 6, ["sun"] = 10,
    };

    /// <summary>The nearest of Search's space pictures: Zen's own picture, then the emoji, then the space's name, then the briefcase.</summary>
    public static int IconFor(string? emoji, string name, string? zenPicture = null)
    {
        if (zenPicture is not null && ByZenPicture.TryGetValue(zenPicture.ToLowerInvariant(), out var z)) return z;
        if (emoji is not null)
        {
            var bare = emoji.Replace("️", "");
            foreach (var (icon, list) in ByEmoji) if (list.Contains(bare)) return icon;
        }
        var words = System.Text.RegularExpressions.Regex.Matches(name.ToLowerInvariant(), @"[\p{L}\p{N}]+").Select(m => m.Value).ToList();
        foreach (var (icon, keys) in ByWord)
            if (keys.Any(k => words.Any(w => w == k || (k.Length >= 4 && w.StartsWith(k, StringComparison.Ordinal))))) return icon;
        return 0;
    }
}

/// <summary>Mozilla's .jsonlz4: "mozLz40\0", the size unpacked as four bytes, then one raw LZ4 block.</summary>
public static class MozLz4
{
    static readonly byte[] Magic = "mozLz40\0"u8.ToArray();
    public const int Limit = 160 << 20;

    public static byte[]? Decode(byte[] data, int limit = Limit)
    {
        if (data.Length <= 12 || !data.AsSpan(0, 8).SequenceEqual(Magic)) return null;
        var size = data[8] | data[9] << 8 | data[10] << 16 | data[11] << 24;
        if (size <= 0 || size > limit) return null;
        var output = new byte[size];
        return Block(data.AsSpan(12), output) == size ? output : null;
    }

    /// <summary>One raw LZ4 block, unpacked; the length written, or -1 for a damaged one.</summary>
    public static int Block(ReadOnlySpan<byte> input, Span<byte> output)
    {
        int i = 0, o = 0;
        while (i < input.Length)
        {
            var token = input[i++];
            var literal = token >> 4;
            if (literal == 15) { int b; do { if (i >= input.Length) return -1; b = input[i++]; literal += b; } while (b == 255); }
            if (i + literal > input.Length || o + literal > output.Length) return -1;
            input.Slice(i, literal).CopyTo(output[o..]);
            i += literal; o += literal;
            if (i >= input.Length) break; // the last sequence has literals only
            if (i + 2 > input.Length) return -1;
            var offset = input[i] | input[i + 1] << 8; i += 2;
            if (offset == 0 || offset > o) return -1;
            var match = token & 15;
            if (match == 15) { int b; do { if (i >= input.Length) return -1; b = input[i++]; match += b; } while (b == 255); }
            match += 4;
            if (o + match > output.Length) return -1;
            for (var k = 0; k < match; k++) { output[o] = output[o - offset]; o++; }
        }
        return o;
    }

    public static JsonObject? Json(string file)
    {
        try
        {
            var info = new FileInfo(file);
            if (!info.Exists || info.Length > Limit) return null;
            return Decode(File.ReadAllBytes(file)) is { } bytes ? JsonNode.Parse(bytes) as JsonObject : null;
        }
        catch { return null; }
    }
}
