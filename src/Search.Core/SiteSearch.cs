namespace Search.Core;

/// <summary>Site search with Tab: type "youtube", press Tab, and what you type next is searched on YouTube itself (Arc's site search).</summary>
public static class SiteSearch
{
    public sealed record Site(string Name, string Host, string Url);

    public static readonly Site[] Sites =
    [
        new("YouTube", "youtube.com", "https://www.youtube.com/results?search_query=%s"),
        new("Wikipedia", "wikipedia.org", "https://en.wikipedia.org/w/index.php?search=%s"),
        new("GitHub", "github.com", "https://github.com/search?q=%s"),
        new("Reddit", "reddit.com", "https://www.reddit.com/search/?q=%s"),
        new("Amazon", "amazon.com", "https://www.amazon.com/s?k=%s"),
        new("Stack Overflow", "stackoverflow.com", "https://stackoverflow.com/search?q=%s"),
        new("Hacker News", "news.ycombinator.com", "https://hn.algolia.com/?q=%s"),
        new("npm", "npmjs.com", "https://www.npmjs.com/search?q=%s"),
        new("X", "x.com", "https://x.com/search?q=%s"),
        new("LinkedIn", "linkedin.com", "https://www.linkedin.com/search/results/all/?keywords=%s"),
        new("Spotify", "spotify.com", "https://open.spotify.com/search/%s"),
        new("Netflix", "netflix.com", "https://www.netflix.com/search?q=%s"),
        new("Pinterest", "pinterest.com", "https://www.pinterest.com/search/pins/?q=%s"),
        new("Dribbble", "dribbble.com", "https://dribbble.com/search/%s"),
        new("Figma", "figma.com", "https://www.figma.com/community/search?resource_type=mixed&q=%s"),
        new("Google Maps", "maps.google.com", "https://www.google.com/maps/search/%s"),
        new("Google Drive", "drive.google.com", "https://drive.google.com/drive/search?q=%s"),
        new("Gmail", "mail.google.com", "https://mail.google.com/mail/u/0/#search/%s"),
        new("Notion", "notion.so", "https://www.notion.so/search?q=%s"),
        new("Claude", "claude.ai", "https://claude.ai/new?q=%s"),
        new("ChatGPT", "chatgpt.com", "https://chatgpt.com/?q=%s"),
    ];

    /// <summary>The site that what is typed names, if any: its name or the start of its host, nothing more.</summary>
    public static Site? Match(string typed)
    {
        var t = typed.Trim().ToLowerInvariant();
        if (t.Length < 2 || t.Contains(' ') ) return null;
        t = t.Replace("www.", "");
        foreach (var s in Sites)
        {
            var name = s.Name.ToLowerInvariant().Replace(" ", "");
            if (name == t || s.Host == t || s.Host.Split('.')[0] == t) return s;
        }
        foreach (var s in Sites)
        {
            var name = s.Name.ToLowerInvariant().Replace(" ", "");
            if (name.StartsWith(t, StringComparison.Ordinal) || s.Host.StartsWith(t, StringComparison.Ordinal)) return s;
        }
        return null;
    }

    public static string UrlFor(Site site, string words) => site.Url.Replace("%s", Uri.EscapeDataString(words.Trim()));
}
