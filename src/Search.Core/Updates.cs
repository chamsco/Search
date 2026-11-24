using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Search.Core;

/// <summary>A newer build, and where its installer is.</summary>
public sealed record Release(System.Version Version, string Tag, string InstallerUrl, string Page);

/// <summary>
/// Knowing when there is a newer one. Port of the idea in Updater.swift: one
/// small request at launch and every hour; nothing relaunches on its own.
/// The feed is the repository's GitHub releases, newest first.
/// </summary>
public static class Updates
{
    public const string Repository = "chamsco/Search";

    /// <summary>v1.0.19.12 or v1.0.19-win.12 → 1.0.19.12.</summary>
    public static System.Version? VersionOf(string tag)
    {
        var t = tag.TrimStart('v', 'V').Replace("-win.", ".");
        return System.Version.TryParse(t, out var v) ? v : null;
    }

    /// <summary>The newest release with an installer that is newer than this build, from the API's JSON.</summary>
    public static Release? Newer(string json, System.Version current)
    {
        Release? best = null;
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array) return null;
        foreach (var r in doc.RootElement.EnumerateArray())
        {
            if (r.TryGetProperty("draft", out var d) && d.GetBoolean()) continue;
            var tag = r.GetProperty("tag_name").GetString() ?? "";
            if (VersionOf(tag) is not { } v || v <= current) continue;
            var asset = r.GetProperty("assets").EnumerateArray()
                .FirstOrDefault(a => (a.GetProperty("name").GetString() ?? "").StartsWith("Search-Setup-", StringComparison.Ordinal)
                                     && (a.GetProperty("name").GetString() ?? "").EndsWith(".exe", StringComparison.Ordinal));
            if (asset.ValueKind != JsonValueKind.Object) continue;
            if (best is null || v > best.Version)
                best = new Release(v, tag, asset.GetProperty("browser_download_url").GetString()!, r.GetProperty("html_url").GetString() ?? "");
        }
        return best;
    }

    public static async Task<Release?> Check(HttpClient http, System.Version current, CancellationToken cancel = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{Repository}/releases?per_page=10");
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Search-for-Windows", current.ToString()));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        using var response = await http.SendAsync(request, cancel);
        if (!response.IsSuccessStatusCode) return null;
        return Newer(await response.Content.ReadAsStringAsync(cancel), current);
    }
}
