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
    public static List<Suggestion> Offers(string typed, History history, Engine engine, IEnumerable<(Guid id, string url, string title)>? open = null, int limit = 5, Bookmarks? bookmarks = null, string custom = "")
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
        // Commands: a word that starts one of Search's pages, or its letters in order ("dwn" for Downloads).
        var lower = text.ToLowerInvariant();
        foreach (var (name, url, key) in InternalPages.Commands)
        {
            var n = name.ToLowerInvariant();
            if (lower.Length >= 2 && (n.StartsWith(lower, StringComparison.Ordinal) || n.Split(' ').Any(w => w.StartsWith(lower, StringComparison.Ordinal)) || Fuzzy.Abbreviates(lower, n)))
                rows.Add(new Suggestion(url, name, url, SuggestionKind.Command));
        }
        if (bookmarks is not null)
            foreach (var b in bookmarks.Suggest(text))
                if (rows.All(r => History.Identity(r.Url) != History.Identity(b.Url))) rows.Add(b);
        foreach (var s in history.Suggestions(text, limit))
            if (rows.All(r => History.Identity(r.Url) != History.Identity(s.Url))) rows.Add(s);
        rows = rows.Take(limit + 2).ToList();
        if (Address.Url(text) is null)
            rows.Add(new Suggestion(text, $"Search {engine.Title()}", engine.SearchUrl(text, custom), SuggestionKind.Search));
        return rows;
    }

    /// <summary>The engine's own suggestions, added under the rest: words, not places, and none that repeats what was typed.</summary>
    public static List<Suggestion> WithEngine(List<Suggestion> rows, string typed, IEnumerable<string> words, Engine engine, string custom = "", int limit = 4)
    {
        var text = typed.Trim();
        var extra = words
            .Where(w => !string.Equals(w.Trim(), text, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .Select(w => new Suggestion(w, "", engine.SearchUrl(w, custom), SuggestionKind.Suggested));
        return rows.Concat(extra).ToList();
    }
}
