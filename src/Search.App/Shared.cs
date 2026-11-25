using Search.Core;

namespace Search.App;

/// <summary>What every window shares: one history, one set of bookmarks, one set of settings, one list of downloads.</summary>
static class Shared
{
    public static readonly SpaceBook Book = SpaceBook.Load();
    static readonly Dictionary<Guid, History> histories = [];
    static readonly Dictionary<Guid, Bookmarks> bookmarks = [];
    static readonly Dictionary<Guid, Archive> archives = [];
    public static Archive ArchiveOf(Guid profile) =>
        archives.TryGetValue(profile, out var a) ? a : archives[profile] = Archive.Load(Archive.FileFor(profile));

    /// <summary>A profile's own history and bookmarks; Personal's are the files there have always been.</summary>
    public static History HistoryOf(Guid profile) =>
        histories.TryGetValue(profile, out var h) ? h : histories[profile] = History.Load(SpaceBook.HistoryFile(profile));
    public static Bookmarks BookmarksOf(Guid profile) =>
        bookmarks.TryGetValue(profile, out var b) ? b : bookmarks[profile] = Bookmarks.Load(SpaceBook.BookmarksFile(profile));
    public static readonly Prefs Prefs = Prefs.Load();
    public static readonly Downloads Downloads = new();
    public static readonly SitePermissions Perms = SitePermissions.Load();
    public static readonly SiteStyles Styles = SiteStyles.Load();
    public static readonly PasswordVault Vault = new(Store.File("vault.json"), new DpapiProtector());
}
