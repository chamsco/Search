namespace Search.Core;

public sealed class Bookmark
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    /// <summary>Null: loose, shown on the bookmarks bar.</summary>
    public Guid? Folder { get; set; }
    public DateTime Added { get; set; } = DateTime.UtcNow;
}

public sealed class BookmarkFolder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
}

public sealed class BookmarkFile
{
    public List<Bookmark> Items { get; set; } = [];
    public List<BookmarkFolder> Folders { get; set; } = [];
}

/// <summary>
/// Bookmarks in the order you choose, folders of your own, a card to name a
/// bookmark as you add it. Port of the model in Sources/Search/Bookmarks.swift.
/// </summary>
public sealed class Bookmarks
{
    string FileName = "bookmarks.json";
    BookmarkFile file = new();
    public event Action? Changed;

    public IReadOnlyList<Bookmark> Items => file.Items;
    public IReadOnlyList<BookmarkFolder> Folders => file.Folders;

    public static Bookmarks Load(string fileName = "bookmarks.json") => new() { FileName = fileName, file = Store.Read<BookmarkFile>(fileName) ?? new() };
    void Save() { Store.Write(FileName, file); Changed?.Invoke(); }

    public Bookmark? Find(string url) =>
        file.Items.FirstOrDefault(b => History.Identity(b.Url) == History.Identity(url));

    public bool Contains(string url) => Find(url) is not null;

    public Bookmark Add(string url, string title, Guid? folder = null)
    {
        if (Find(url) is { } existing) return existing;
        var b = new Bookmark { Url = url, Title = string.IsNullOrWhiteSpace(title) ? Address.Pretty(url) : title.Trim(), Folder = folder };
        file.Items.Add(b);
        Save();
        return b;
    }

    public void Rename(Guid id, string title)
    {
        if (file.Items.FirstOrDefault(b => b.Id == id) is { } b && !string.IsNullOrWhiteSpace(title)) { b.Title = title.Trim(); Save(); }
    }

    public void Remove(Guid id) { if (file.Items.RemoveAll(b => b.Id == id) > 0) Save(); }

    public void Remove(string url) { if (Find(url) is { } b) Remove(b.Id); }

    public void Move(Guid id, Guid? folder)
    {
        if (file.Items.FirstOrDefault(b => b.Id == id) is { } b) { b.Folder = folder; Save(); }
    }

    /// <summary>Put a bookmark at a place in the order.</summary>
    public void Reorder(Guid id, int index)
    {
        var at = file.Items.FindIndex(b => b.Id == id);
        if (at < 0) return;
        var b = file.Items[at];
        file.Items.RemoveAt(at);
        file.Items.Insert(Math.Clamp(index, 0, file.Items.Count), b);
        Save();
    }

    public BookmarkFolder AddFolder(string name)
    {
        var f = new BookmarkFolder { Name = string.IsNullOrWhiteSpace(name) ? "New Folder" : name.Trim() };
        file.Folders.Add(f);
        Save();
        return f;
    }

    public void RenameFolder(Guid id, string name)
    {
        if (file.Folders.FirstOrDefault(f => f.Id == id) is { } f && !string.IsNullOrWhiteSpace(name)) { f.Name = name.Trim(); Save(); }
    }

    /// <summary>Deleting a folder keeps its bookmarks, loose.</summary>
    public void RemoveFolder(Guid id)
    {
        foreach (var b in file.Items.Where(b => b.Folder == id)) b.Folder = null;
        file.Folders.RemoveAll(f => f.Id == id);
        Save();
    }

    public IEnumerable<Bookmark> In(Guid? folder) => file.Items.Where(b => b.Folder == folder);

    /// <summary>Bookmarks in the address field as you type, by name or address.</summary>
    public IEnumerable<Suggestion> Suggest(string typed, int limit = 3)
    {
        var words = Fuzzy.Words(typed);
        if (words.Count == 0) return [];
        return file.Items
            .Where(b => { var hay = (History.KeyOf(b.Url) + " " + b.Title).ToLowerInvariant(); return words.All(w => hay.Contains(w, StringComparison.Ordinal)); })
            .Take(limit)
            .Select(b => new Suggestion(History.KeyOf(b.Url), b.Title, b.Url, SuggestionKind.Bookmark));
    }
}
