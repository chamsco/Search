namespace Search.Core;

public enum ProxyMode { System, Off, Custom }

/// <summary>Settings › Privacy › Proxy. The engine takes its proxy when it starts, so a change waits for the next start.</summary>
public static class Proxy
{
    /// <summary>What a person typed, made a server the engine understands: "host:port", "http://host:port", "socks5://host:port". Empty if it is not one.</summary>
    public static string Normalise(string typed)
    {
        var t = typed.Trim();
        if (t.Length == 0 || t.Contains(' ')) return "";
        var scheme = "";
        var rest = t;
        var i = t.IndexOf("://", StringComparison.Ordinal);
        if (i > 0) { scheme = t[..i].ToLowerInvariant(); rest = t[(i + 3)..]; if (scheme is not ("http" or "https" or "socks4" or "socks5")) return ""; }
        rest = rest.TrimEnd('/');
        var colon = rest.LastIndexOf(':');
        if (colon <= 0 || !int.TryParse(rest[(colon + 1)..], out var port) || port is < 1 or > 65535) return "";
        var host = rest[..colon];
        if (host.Length == 0 || host.Contains('/') || host.Contains('@')) return "";
        return (scheme.Length > 0 ? scheme + "://" : "") + host + ":" + port;
    }

    /// <summary>The engine switch for this setting; empty when the engine should do what Windows says.</summary>
    public static string Argument(ProxyMode mode, string server) => mode switch
    {
        ProxyMode.Off => "--no-proxy-server",
        ProxyMode.Custom when Normalise(server) is { Length: > 0 } s => $"--proxy-server={s}",
        _ => "",
    };
}
