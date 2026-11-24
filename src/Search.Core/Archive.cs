namespace Search.Core;

public sealed class Archived
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
    public DateTime Closed { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// The tabs Search closed by itself, as Arc's archive keeps them: newest first, for thirty days or the last five hundred.
/// Only what Search closed; a tab you closed yourself is one you meant to. Port of Sources/Search/Archive.swift.
/// </summary>
public sealed class Archive
{
    public const int Cap = 500;
    public static readonly TimeSpan Lasts = TimeSpan.FromDays(30);

    string fileName = "archive.json";
    readonly Func<DateTime> now;
    List<Archived> items = [];
    public event Action? Changed;

    public Archive(Func<DateTime>? clock = null) => now = clock ?? (() => DateTime.UtcNow);

    public IReadOnlyList<Archived> Items => items;

    public static string FileFor(Guid profile) => profile == Spaces.PersonalId ? "archive.json" : $"archive-{profile:N}.json";

    public static Archive Load(string fileName = "archive.json", Func<DateTime>? clock = null)
    {
        var a = new Archive(clock) { fileName = fileName, items = Store.Read<List<Archived>>(fileName) ?? [] };
        a.Prune();
        return a;
    }

    void Save() { Store.Write(fileName, items); Changed?.Invoke(); }

    void Prune()
    {
        var cutoff = now() - Lasts;
        items = items.Where(i => i.Closed >= cutoff).OrderByDescending(i => i.Closed).Take(Cap).ToList();
    }

    /// <summary>A page that is worth keeping: not blank, not one of Search's own.</summary>
    public static bool Keeps(string url) =>
        !string.IsNullOrWhiteSpace(url) && !url.StartsWith("search://", StringComparison.OrdinalIgnoreCase) && !url.StartsWith("about:", StringComparison.OrdinalIgnoreCase);

    public void Add(string url, string title)
    {
        if (!Keeps(url)) return;
        items.RemoveAll(i => History.Identity(i.Url) == History.Identity(url));
        items.Insert(0, new Archived { Url = url, Title = title, Closed = now() });
        Prune();
        Save();
    }

    public void Remove(Guid id) { if (items.RemoveAll(i => i.Id == id) > 0) Save(); }
    public void Clear() { items.Clear(); Save(); }

    public IEnumerable<Archived> Search(string typed)
    {
        var words = Fuzzy.Words(typed);
        return words.Count == 0 ? items : items.Where(i => { var hay = (i.Title + " " + History.KeyOf(i.Url)).ToLowerInvariant(); return words.All(w => hay.Contains(w, StringComparison.Ordinal)); });
    }
}

/// <summary>The tabs that have been left alone longest: what "Close tabs left alone" picks.</summary>
public static class IdleTabs
{
    /// <summary>Of tabs (id, last looked at, may be closed), those unattended for DAYS or more as of NOW; none when DAYS is 0.</summary>
    public static List<Guid> Due(IEnumerable<(Guid Id, DateTime LastSeen, bool Closable)> tabs, int days, DateTime now) =>
        days <= 0 ? [] : tabs.Where(t => t.Closable && now - t.LastSeen >= TimeSpan.FromDays(days)).Select(t => t.Id).ToList();
}

/// <summary>The windows that were open, so a restart brings them all back (Windows.swift).</summary>
public sealed class WindowEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid Profile { get; set; } = Spaces.PersonalId;
    public WindowPlacement? Placement { get; set; }
}

public static class WindowList
{
    const string FileName = "windows.json";
    public static List<WindowEntry> Load() => Store.Read<List<WindowEntry>>(FileName) ?? [];
    public static void Save(IEnumerable<WindowEntry> windows) => Store.Write(FileName, windows.ToList());
}
