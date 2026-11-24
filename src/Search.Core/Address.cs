using System.Globalization;

namespace Search.Core;

/// <summary>
/// What you type has to be a place. Port of Sources/Search/Address.swift:
/// hands back an address string, or null when the text is not a place
/// (the caller then searches for it).
/// Strings rather than <see cref="Uri"/>, because Uri normalises
/// ("https://example.com" becomes "https://example.com/") and the original
/// keeps what was typed.
/// </summary>
public static class Address
{
    static readonly HashSet<string> Ours = new(StringComparer.OrdinalIgnoreCase)
    {
        "http", "https", "file", "about", "data", InternalPages.Scheme,
    };

    public static string? Url(string typed)
    {
        var text = typed.Trim();
        if (text.Length == 0 || text.Contains(' ')) return null;

        // Written with a scheme, it is taken at its word.
        var split = text.IndexOf("://", StringComparison.Ordinal);
        if (split >= 0)
        {
            var scheme = text[..split];
            return Ours.Contains(scheme) && Uri.TryCreate(text, UriKind.Absolute, out _) ? text : null;
        }
        if (text.StartsWith("about:", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return text;

        // Everything else has to look like a host before it gets a scheme.
        var end = text.IndexOfAny(['/', '?', '#']);
        var head = end < 0 ? text : text[..end];
        if (head.Contains('@')) return null; // an email address
        if (head.StartsWith("[::1]", StringComparison.Ordinal)) return "http://" + text;
        var host = head.Split(':')[0];
        if (!LooksLikeHost(host)) return null;

        // A local server almost never has a certificate.
        var local = host == "localhost"
            || host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".local", StringComparison.OrdinalIgnoreCase)
            || host == "127.0.0.1"
            || host == "0.0.0.0"
            || PrivateRange(host);
        return (local ? "http://" : "https://") + text;
    }

    /// <summary>Dev servers print 0.0.0.0; open it as localhost, the same server.</summary>
    public static string? Reachable(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var u)) return null;
        if (u.Host != "0.0.0.0" || (u.Scheme != "http" && u.Scheme != "https")) return null;
        var b = new UriBuilder(u) { Host = "localhost" };
        if (u.IsDefaultPort) b.Port = -1;
        return b.Uri.ToString();
    }

    static bool PrivateRange(string host)
    {
        var parts = host.Split('.');
        if (parts.Length != 4) return false;
        var o = new byte[4];
        for (var i = 0; i < 4; i++)
            if (!byte.TryParse(parts[i], NumberStyles.None, CultureInfo.InvariantCulture, out o[i])) return false;
        return o[0] == 10
            || (o[0] == 192 && o[1] == 168)
            || (o[0] == 172 && o[1] >= 16 && o[1] <= 31);
    }

    static bool LooksLikeHost(string host)
    {
        if (host == "localhost") return true;
        var labels = host.Split('.');
        if (labels.Length == 4 && labels.All(l => byte.TryParse(l, NumberStyles.None, CultureInfo.InvariantCulture, out _)))
            return true;
        if (labels.Length < 2) return false;
        foreach (var label in labels)
        {
            if (label.Length == 0 || label.StartsWith('-') || label.EndsWith('-')) return false;
            if (!label.All(c => char.IsLetterOrDigit(c) || c == '-')) return false;
        }
        // A dotted thing ending in letters is a domain; ending in digits, a version number.
        var tld = labels[^1];
        return tld.Length >= 2 && tld.All(char.IsLetter);
    }

    /// <summary>A page's address as the field shows it for editing.</summary>
    public static string Editable(string full)
    {
        var sep = full.IndexOf("://", StringComparison.Ordinal);
        if (sep < 0) return full;
        var shortForm = full[(sep + 3)..];
        var candidates = new List<string> { shortForm };
        if (shortForm.EndsWith('/') && shortForm.IndexOf('/') == shortForm.Length - 1)
            candidates.Insert(0, shortForm[..^1]);
        foreach (var c in candidates)
        {
            var back = Url(c);
            if (back is null) continue;
            if (back == full || back + "/" == full) return c;
        }
        return full;
    }

    public static string WithoutWww(string address) =>
        address.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? address[4..] : address;

    /// <summary>What the tab says before the page has told us its title.</summary>
    public static string Pretty(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var u) || string.IsNullOrEmpty(u.Host)) return url;
        var bare = WithoutWww(u.Host);
        var path = u.AbsolutePath;
        return string.IsNullOrEmpty(path) || path == "/" ? bare : bare + path;
    }

    /// <summary>Host without "www.", for rows and monograms.</summary>
    public static string? Host(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var u) && !string.IsNullOrEmpty(u.Host) ? WithoutWww(u.Host) : null;
}

public static class InternalPages
{
    public const string Scheme = "search";
    public const string NewTab = "";
}
