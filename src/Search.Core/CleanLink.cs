using System.Text;

namespace Search.Core;

/// <summary>Copy Clean Link: the address without the tracking that rides along in it.</summary>
public static class CleanLink
{
    static readonly string[] Exact =
    [
        "fbclid", "gclid", "gclsrc", "dclid", "msclkid", "yclid", "twclid", "ttclid", "li_fat_id", "igshid", "igsh",
        "mc_cid", "mc_eid", "_hsenc", "_hsmi", "hsctatracking", "mkt_tok", "vero_id", "oly_enc_id", "oly_anon_id",
        "wickedid", "s_cid", "_ga", "_gl", "ref_src", "ref_url", "spm", "si", "feature", "pp",
    ];

    static bool IsTracking(string name)
    {
        var n = name.ToLowerInvariant();
        return n.StartsWith("utm_", StringComparison.Ordinal) || n.StartsWith("pk_", StringComparison.Ordinal) || n.StartsWith("mtm_", StringComparison.Ordinal)
            || n.StartsWith("hsa_", StringComparison.Ordinal) || Exact.Contains(n);
    }

    public static string Strip(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var u) || u.Scheme is not ("http" or "https")) return url;
        var kept = new List<string>();
        foreach (var part in u.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var name = Uri.UnescapeDataString(part.Split('=', 2)[0]);
            // A few sites need their own "si" or "feature"; only YouTube-style share trackers are dropped there.
            if (IsTracking(name) && !(name is "si" or "feature" or "pp" or "spm" && !SharesTrackerFor(u.Host, name))) continue;
            kept.Add(part);
        }
        var b = new StringBuilder(u.GetLeftPart(UriPartial.Path));
        if (kept.Count > 0) b.Append('?').Append(string.Join('&', kept));
        b.Append(u.Fragment);
        return b.ToString();
    }

    static bool SharesTrackerFor(string host, string name) => name switch
    {
        "si" or "feature" or "pp" => host.EndsWith("youtube.com", StringComparison.OrdinalIgnoreCase) || host.EndsWith("youtu.be", StringComparison.OrdinalIgnoreCase) || host.EndsWith("spotify.com", StringComparison.OrdinalIgnoreCase),
        "spm" => host.EndsWith("aliexpress.com", StringComparison.OrdinalIgnoreCase) || host.EndsWith("taobao.com", StringComparison.OrdinalIgnoreCase) || host.EndsWith("alibaba.com", StringComparison.OrdinalIgnoreCase),
        _ => true,
    };
}
