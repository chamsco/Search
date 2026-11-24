namespace Search.Core;

/// <summary>A profile: its own history, bookmarks and spaces. Port of Sources/Search/Profiles.swift.</summary>
public sealed class Profile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public bool IsPersonal => Id == Spaces.PersonalId;
    public static Profile Personal() => new() { Id = Spaces.PersonalId, Name = "Personal" };
}

/// <summary>How strongly a space's colour washes the chrome (SpaceStrength in the Mac app).</summary>
public enum SpaceStrength { Subtle, Medium, Strong }

/// <summary>A space: its own row of tabs in the one window, and sign-ins of its own if you like. Port of Space in Spaces.swift.</summary>
public sealed class Space
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    /// <summary>Index into <see cref="Spaces.Colours"/>; 0 is none.</summary>
    public int Colour { get; set; }
    /// <summary>A second colour, for a gradient; 0 for the colour alone.</summary>
    public int Colour2 { get; set; }
    public SpaceStrength Strength { get; set; }
    /// <summary>Index into <see cref="Spaces.Icons"/>; null is the first space's house or any other's briefcase.</summary>
    public int? Icon { get; set; }
    /// <summary>Signed in wherever the profile's first space is, rather than a store of its own.</summary>
    public bool SharesSignIns { get; set; } = true;
    /// <summary>Where this space's downloads go; null for the folder in Settings.</summary>
    public string? Downloads { get; set; }
    /// <summary>Null is Personal.</summary>
    public Guid? Profile { get; set; }

    public Guid ProfileId => Profile ?? Spaces.PersonalId;
    public bool IsFirst => Id == Spaces.FirstId;
    public int IconIndex => Icon is int i && i >= 0 && i < Spaces.Icons.Length ? i : IsFirst ? 16 : 0;
    public static Space First() => new() { Id = Spaces.FirstId, Name = "Personal" };
}

public static class Spaces
{
    public static readonly Guid FirstId = new("00000000-0000-0000-0000-000000000001");
    public static readonly Guid PersonalId = new("00000000-0000-0000-0000-0000000000a1");

    /// <summary>Picked to read on the light ground and the dark. The first stands for none.</summary>
    public static readonly (byte R, byte G, byte B)[] Colours =
    [
        (115, 120, 133), (66, 133, 245), (51, 168, 115), (245, 158, 51), (230, 84, 102), (158, 102, 230),
    ];
    public static readonly string[] ColourNames = ["None", "Blue", "Green", "Orange", "Red", "Violet"];

    /// <summary>Segoe Fluent glyphs standing in for the Mac's symbols: work, thinking, leisure, life.</summary>
    public static readonly string[] Icons =
    [
        "", "", "", "", "", "",
        "", "", "", "", "", "",
        "", "", "", "", "", "",
        "", "", "", "", "", "",
    ];
    public static readonly string[] IconNames =
    [
        "Work", "Office", "Desktop", "Laptop", "Code", "Terminal", "AI", "Thinking", "Ideas", "Games", "Leisure", "Café",
        "Music", "Film", "Art", "Photos", "Home", "Reading", "Studies", "Shopping", "Travel", "Sport", "Nature", "Personal",
    ];

    /// <summary>The faintness of the wash behind the tabs, by strength and ground (washLight / washDark).</summary>
    public static double Wash(SpaceStrength s, bool dark) => (s, dark) switch
    {
        (SpaceStrength.Subtle, false) => 0.07, (SpaceStrength.Subtle, true) => 0.11,
        (SpaceStrength.Medium, false) => 0.14, (SpaceStrength.Medium, true) => 0.20,
        (_, false) => 0.24, _ => 0.32,
    };

    public static (byte R, byte G, byte B)? Rgb(int colour) =>
        colour > 0 && colour < Colours.Length ? Colours[colour] : null;
}

/// <summary>The profiles and the spaces in them, kept in profiles.json and spaces.json.</summary>
public sealed class SpaceBook
{
    public List<Profile> Profiles { get; private set; } = [Profile.Personal()];
    public List<Space> Spaces { get; private set; } = [Space.First()];

    public static SpaceBook Load()
    {
        var book = new SpaceBook();
        if (Store.Read<List<Profile>>("profiles.json") is { Count: > 0 } p)
            book.Profiles = p.Any(x => x.Id == Core.Spaces.PersonalId) ? p : [Profile.Personal(), .. p];
        if (Store.Read<List<Space>>("spaces.json") is { Count: > 0 } s)
        {
            book.Spaces = s.GroupBy(x => x.Id).Select(g => g.First()).ToList();
            if (!book.Spaces.Any(x => x.IsFirst)) book.Spaces.Insert(0, Space.First());
            foreach (var sp in book.Spaces.Where(x => x.IsFirst)) sp.Profile = null;
        }
        return book;
    }

    public void Save()
    {
        Store.Write("profiles.json", Profiles);
        Store.Write("spaces.json", Spaces);
    }

    public Profile ProfileOf(Guid id) => Profiles.FirstOrDefault(p => p.Id == id) ?? Profiles[0];
    public IEnumerable<Space> In(Guid profile) => Spaces.Where(s => s.ProfileId == profile);

    /// <summary>The space that holds the profile's sign-ins: its first.</summary>
    public Space HomeOf(Guid profile) => In(profile).FirstOrDefault() ?? Core.Space.First();

    /// <summary>The name of the engine's store a space's pages use. Null is the default store, which the first space of
    /// Personal has always used, so turning spaces on signs nobody out; a profile has a store of its own, and a space
    /// that doesn't share sign-ins has another.</summary>
    public static string? StoreName(Space s)
    {
        if (!s.SharesSignIns && !s.IsFirst) return "S" + s.Id.ToString("N");
        return s.ProfileId == Core.Spaces.PersonalId ? null : "P" + s.ProfileId.ToString("N");
    }

    public Space AddSpace(Guid profile, string name, int colour = 0, bool sharesSignIns = true)
    {
        var s = new Space { Name = string.IsNullOrWhiteSpace(name) ? "New Space" : name.Trim(), Colour = colour, SharesSignIns = sharesSignIns, Profile = profile == Core.Spaces.PersonalId ? null : profile };
        Spaces.Add(s);
        Save();
        return s;
    }

    public void Rename(Guid space, string name)
    {
        if (Spaces.FirstOrDefault(s => s.Id == space) is { } s && !string.IsNullOrWhiteSpace(name)) { s.Name = name.Trim(); Save(); }
    }

    /// <summary>The first space of a profile can't be deleted: it holds the profile's sign-ins.</summary>
    public bool DeleteSpace(Guid space)
    {
        var s = Spaces.FirstOrDefault(x => x.Id == space);
        if (s is null || s.IsFirst || In(s.ProfileId).First() == s) return false;
        Spaces.Remove(s);
        Save();
        return true;
    }

    public void Move(Guid space, int by)
    {
        var s = Spaces.FirstOrDefault(x => x.Id == space);
        if (s is null) return;
        var mine = In(s.ProfileId).ToList();
        var at = mine.IndexOf(s);
        var to = Math.Clamp(at + by, 0, mine.Count - 1);
        if (to == at) return;
        Spaces.Remove(s);
        var anchor = mine[to == at ? at : to];
        var i = Spaces.IndexOf(anchor);
        Spaces.Insert(by > 0 ? i + 1 : i, s);
        Save();
    }

    public Profile AddProfile(string name)
    {
        var p = new Profile { Name = string.IsNullOrWhiteSpace(name) ? "New Profile" : name.Trim() };
        Profiles.Add(p);
        Spaces.Add(new Space { Name = "Personal", Profile = p.Id, SharesSignIns = true });
        Save();
        return p;
    }

    public void RenameProfile(Guid id, string name)
    {
        if (Profiles.FirstOrDefault(p => p.Id == id) is { } p && !string.IsNullOrWhiteSpace(name)) { p.Name = name.Trim(); Save(); }
    }

    /// <summary>Personal can't be deleted; a profile's spaces go with it.</summary>
    public bool DeleteProfile(Guid id)
    {
        if (id == Core.Spaces.PersonalId || Profiles.FirstOrDefault(p => p.Id == id) is not { } p) return false;
        Profiles.Remove(p);
        Spaces.RemoveAll(s => s.ProfileId == id);
        Save();
        return true;
    }

    // MARK: - files of a profile

    public static string HistoryFile(Guid profile) => profile == Core.Spaces.PersonalId ? "history.json" : $"history-{profile:N}.json";
    public static string BookmarksFile(Guid profile) => profile == Core.Spaces.PersonalId ? "bookmarks.json" : $"bookmarks-{profile:N}.json";
}
