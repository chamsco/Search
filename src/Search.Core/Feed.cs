using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Search.Core;

/// <summary>One post of a live folder: a line that opens its page.</summary>
public sealed class FeedItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string Link { get; set; } = "";
}

/// <summary>A web feed, read (LiveFolders.swift's Feed). Pure, so tests can hold it; the fetching is the app's.</summary>
public static class Feed
{
    /// <summary>A live folder keeps its feed's latest posts, no more: a column that wide wants it short.</summary>
    public const int Most = 12;
    public const int MaxBytes = 2_000_000;

    public sealed record Read(string Title, string Feed, List<FeedItem> Items);

    /// <summary>What was typed, as an address: a bare example.com/feed is https. Null when it is not one.</summary>
    public static Uri? Address(string text)
    {
        text = (text ?? "").Trim();
        if (text.Length == 0 || text.Contains(' ')) return null;
        var full = text.Contains("://") ? text : "https://" + text;
        if (!Uri.TryCreate(full, UriKind.Absolute, out var url)) return null;
        return (url.Scheme is "https" or "http") && url.Host.Length > 0 ? url : null;
    }

    /// <summary>RSS 2.0 (and 1.0) or Atom; null for anything else, a web page among them.</summary>
    public static Read? Parse(string xml, Uri url)
    {
        XDocument doc;
        try
        {
            using var reader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
            doc = XDocument.Load(reader);
        }
        catch { return null; }
        var root = doc.Root;
        if (root is null) return null;
        var kind = root.Name.LocalName.ToLowerInvariant();
        if (kind is not ("rss" or "feed" or "rdf")) return null;

        string? title = null;
        var items = new List<FeedItem>();
        var seen = new HashSet<string>();
        foreach (var e in root.DescendantsAndSelf())
        {
            var n = e.Name.LocalName.ToLowerInvariant();
            if (title is null && n == "title" && e.Parent is { } p && p.Name.LocalName.ToLowerInvariant() is "channel" or "feed")
                title = Clean(e.Value, false);
            if (n is not ("item" or "entry")) continue;
            string? t = null, link = null, guid = null; var html = false; var guidIsLink = true;
            foreach (var c in e.Elements())
            {
                var cn = c.Name.LocalName.ToLowerInvariant();
                if (cn == "title" && t is null) { t = c.Value; html = ((string?)c.Attribute("type") ?? "").ToLowerInvariant() is "html" or "xhtml"; }
                else if (cn == "link")
                {
                    if (c.Attribute("href") is { } href)
                    {
                        var rel = ((string?)c.Attribute("rel") ?? "alternate").ToLowerInvariant();
                        if (rel == "alternate" && link is null) link = href.Value;
                    }
                    else if (link is null && c.Value.Trim().Length > 0) link = c.Value.Trim();
                }
                else if (cn == "guid") { guid = c.Value.Trim(); guidIsLink = ((string?)c.Attribute("isPermaLink") ?? "true").ToLowerInvariant() != "false"; }
            }
            var raw = link ?? (guidIsLink ? guid : null);
            if (raw is null || !Uri.TryCreate(url, raw.Trim(), out var abs) || abs.Scheme is not ("http" or "https")) continue;
            if (!seen.Add(abs.AbsoluteUri)) continue;
            var said = Clean(t ?? "", html);
            if (said.Length == 0) said = abs.Host + (abs.AbsolutePath == "/" ? "" : abs.AbsolutePath);
            items.Add(new FeedItem { Title = said.Length > 300 ? said[..300] : said, Link = abs.AbsoluteUri });
            if (items.Count >= Most) break;
        }
        title = string.IsNullOrEmpty(title) ? url.Host : title;
        return new Read(title.Length > 200 ? title[..200] : title, url.AbsoluteUri, items);
    }

    /// <summary>A page's own feed: the first link rel="alternate" of an RSS or Atom type in it, against the page's address.</summary>
    public static Uri? Discover(string html, Uri page)
    {
        foreach (Match tag in Regex.Matches(html, @"<link\b[^>]*>", RegexOptions.IgnoreCase))
        {
            var a = Attributes(tag.Value);
            var rel = (a.GetValueOrDefault("rel") ?? "").ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var type = (a.GetValueOrDefault("type") ?? "").ToLowerInvariant();
            if (!rel.Contains("alternate") || (type != "application/rss+xml" && type != "application/atom+xml")) continue;
            if (a.GetValueOrDefault("href") is not { } href) continue;
            if (Uri.TryCreate(page, Decode(href).Trim(), out var url) && url.Scheme is "http" or "https") return url;
        }
        return null;
    }

    static Dictionary<string, string> Attributes(string tag)
    {
        var d = new Dictionary<string, string>();
        foreach (Match m in Regex.Matches(tag, @"([A-Za-z_:][-A-Za-z0-9_:.]*)\s*=\s*(?:""([^""]*)""|'([^']*)'|([^\s""'>]+))"))
            d[m.Groups[1].Value.ToLowerInvariant()] = m.Groups[2].Success ? m.Groups[2].Value : m.Groups[3].Success ? m.Groups[3].Value : m.Groups[4].Value;
        return d;
    }

    /// <summary>A refetch's posts wearing the ids they already had, by link, so an unchanged feed changes nothing.</summary>
    public static List<FeedItem> KeepingIds(IEnumerable<FeedItem> fresh, IEnumerable<FeedItem> old)
    {
        var ids = new Dictionary<string, Guid>();
        foreach (var o in old) ids.TryAdd(o.Link, o.Id);
        var list = fresh.ToList();
        foreach (var f in list) if (ids.TryGetValue(f.Link, out var id)) f.Id = id;
        return list;
    }

    /// <summary>A title as a line: tags out of one sent as HTML, entities a feed escaped twice read, spaces made one.</summary>
    public static string Clean(string text, bool html)
    {
        if (html) text = Regex.Replace(text, "<[^>]*>", "");
        text = Decode(text);
        return string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    public static string Decode(string text) => text.Contains('&') ? System.Net.WebUtility.HtmlDecode(text) : text;
}
