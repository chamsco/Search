using Search.Core;

namespace Search.App;

/// <summary>What every window shares: one history, one set of bookmarks, one set of settings, one list of downloads.</summary>
static class Shared
{
    public static readonly History History = History.Load();
    public static readonly Bookmarks Bookmarks = Bookmarks.Load();
    public static readonly Prefs Prefs = Prefs.Load();
    public static readonly Downloads Downloads = new();
}
