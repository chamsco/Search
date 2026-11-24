namespace Search.Core;

/// <summary>Settings › Privacy › Block trackers: requests to these hosts (and their subdomains) are not sent.</summary>
public static class Trackers
{
    static readonly HashSet<string> Hosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "doubleclick.net", "googlesyndication.com", "googleadservices.com", "google-analytics.com", "googletagmanager.com", "googletagservices.com",
        "adservice.google.com", "facebook.net", "connect.facebook.net", "pixel.facebook.com", "analytics.twitter.com", "ads-twitter.com",
        "ads.linkedin.com", "px.ads.linkedin.com", "bat.bing.com", "clarity.ms", "hotjar.com", "static.hotjar.com", "mixpanel.com", "api.mixpanel.com",
        "segment.io", "segment.com", "cdn.segment.com", "amplitude.com", "api.amplitude.com", "fullstory.com", "mouseflow.com", "crazyegg.com",
        "scorecardresearch.com", "quantserve.com", "outbrain.com", "taboola.com", "criteo.com", "criteo.net", "adnxs.com", "rubiconproject.com",
        "pubmatic.com", "openx.net", "casalemedia.com", "moatads.com", "adsrvr.org", "demdex.net", "omtrdc.net", "everesttech.net", "bluekai.com",
        "krxd.net", "chartbeat.com", "chartbeat.net", "newrelic.com", "nr-data.net", "branch.io", "app.link", "appsflyer.com", "adjust.com",
        "analytics.tiktok.com", "snap.licdn.com", "sentry-cdn.com", "optimizely.com", "heapanalytics.com", "intercom.io",
    };

    public static IEnumerable<string> All => Hosts;

    /// <summary>Is this address one of them? Matches the host or any parent of it ("a.b.doubleclick.net" → "doubleclick.net").</summary>
    public static bool IsTracker(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var u) || u.Scheme is not ("http" or "https" or "ws" or "wss")) return false;
        var host = u.Host.ToLowerInvariant();
        for (var h = host; h.Length > 0;)
        {
            if (Hosts.Contains(h)) return true;
            var dot = h.IndexOf('.');
            if (dot < 0 || h.IndexOf('.', dot + 1) < 0) break; // keep at least two labels
            h = h[(dot + 1)..];
        }
        return false;
    }
}
