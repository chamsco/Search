namespace Search.Core;

/// <summary>Site styles: a bit of CSS you wrote for a site, applied whenever the page opens (Settings › Site styles).</summary>
public sealed class SiteStyles
{
    const string FileName = "styles.json";
    public Dictionary<string, string> Hosts { get; set; } = [];
    public event Action? Changed;

    public static SiteStyles Load() => Store.Read<SiteStyles>(FileName) ?? new();

    public static string HostOf(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var u) && u.Scheme is "http" or "https" ? u.Host.ToLowerInvariant().Replace("www.", "") : "";

    /// <summary>The CSS for the host of this address (a style for example.com covers www.example.com and its subdomains).</summary>
    public string For(string url)
    {
        var host = HostOf(url);
        if (host.Length == 0) return "";
        var parts = new List<string>();
        for (var h = host; h.Length > 0;)
        {
            if (Hosts.TryGetValue(h, out var css) && css.Trim().Length > 0) parts.Insert(0, css);
            var dot = h.IndexOf('.');
            if (dot < 0 || h.IndexOf('.', dot + 1) < 0) break;
            h = h[(dot + 1)..];
        }
        return string.Join("\n", parts);
    }

    public void Set(string host, string css)
    {
        host = host.Trim().ToLowerInvariant().Replace("www.", "");
        if (host.Length == 0) return;
        if (css.Trim().Length == 0) Hosts.Remove(host); else Hosts[host] = css;
        Store.Write(FileName, this);
        Changed?.Invoke();
    }
}
