using System.Net.Http;
using System.Text.Json;

namespace Search.Core;

/// <summary>
/// Words the search engine itself suggests from what was typed so far
/// (Settings › General › Suggestions from your search engine on the Mac).
/// Every engine here answers in the OpenSearch shape: ["typed", ["one", "two", …]].
/// Never asked for an address, and never from a private tab.
/// </summary>
public sealed class EngineSuggester(HttpClient http)
{
    public async Task<List<string>> Suggest(Engine engine, string typed, CancellationToken cancel)
    {
        var text = typed.Trim();
        if (text.Length == 0 || Address.Url(text) is not null || engine.SuggestTemplate() is not { } template) return [];
        var url = template.Replace("%s", Uri.EscapeDataString(text));
        try
        {
            using var response = await http.GetAsync(url, cancel);
            if (!response.IsSuccessStatusCode) return [];
            var body = await response.Content.ReadAsStringAsync(cancel);
            return Parse(body);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException)
        {
            return [];
        }
    }

    public static List<string> Parse(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 1 && root[1].ValueKind == JsonValueKind.Array)
                return root[1].EnumerateArray().Where(e => e.ValueKind == JsonValueKind.String).Select(e => e.GetString()!).ToList();
            // DuckDuckGo without type=list, Brave's newer shape: [{ "phrase": … }]
            if (root.ValueKind == JsonValueKind.Array)
                return root.EnumerateArray()
                    .Select(e => e.ValueKind == JsonValueKind.Object && e.TryGetProperty("phrase", out var p) ? p.GetString() : null)
                    .Where(s => s is not null).Select(s => s!).ToList();
        }
        catch (JsonException) { }
        return [];
    }
}
