namespace Search.Core;

/// <summary>What pressing Enter in the address field does with the text.</summary>
public static class Omnibox
{
    public static string Resolve(string typed, Engine engine, string customEngine = "")
    {
        var text = typed.Trim();
        var url = Address.Url(text);
        if (url is not null) return Address.Reachable(url) ?? url;
        return engine.SearchUrl(text, customEngine);
    }

    /// <summary>The rows under the field: history first, then the search for the words.</summary>
    public static List<Suggestion> Offers(string typed, History history, Engine engine, IEnumerable<(Guid id, string url, string title)>? open = null, int limit = 5)
    {
        var text = typed.Trim();
        if (text.Length == 0) return [];
        var rows = new List<Suggestion>();
        if (open is not null)
        {
            var words = Fuzzy.Words(text);
            foreach (var (id, url, title) in open)
            {
                var hay = (History.KeyOf(url) + " " + title).ToLowerInvariant();
                if (words.All(w => hay.Contains(w, StringComparison.Ordinal)))
                    rows.Add(new Suggestion(History.KeyOf(url), title, url, SuggestionKind.Open) { Tab = id });
                if (rows.Count >= 2) break;
            }
        }
        foreach (var s in history.Suggestions(text, limit))
            if (rows.All(r => History.Identity(r.Url) != History.Identity(s.Url))) rows.Add(s);
        rows = rows.Take(limit).ToList();
        if (Address.Url(text) is null)
            rows.Add(new Suggestion(text, $"Search {engine.Title()}", engine.SearchUrl(text), SuggestionKind.Search));
        return rows;
    }
}
