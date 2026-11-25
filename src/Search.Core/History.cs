namespace Search.Core;

public enum SuggestionKind { Open, Visited, Bookmark, Known, Search, Suggested, Command }

public sealed record Suggestion(string Key, string Title, string Url, SuggestionKind Kind)
{
    public Guid? Tab { get; init; }
}

public sealed class Visit
{
    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
    public int Count { get; set; }
    public DateTime Last { get; set; }
}

/// <summary>
/// Where you have been, so the field can finish the address for you.
/// Port of the ranking in Sources/Search/History.swift:
/// score = fit × log(2 + popularity), popularity = visits × e^(−days/30).
/// </summary>
public sealed class History
{
    public const int Cap = 20_000;
    string FileName = "history.json";
    readonly Dictionary<string, Visit> visits = new();
    readonly Func<DateTime> now;

    public History(Func<DateTime>? clock = null) => now = clock ?? (() => DateTime.UtcNow);

    public IReadOnlyCollection<Visit> All => visits.Values;

    public static History Load(string fileName = "history.json")
    {
        var h = new History { FileName = fileName };
        foreach (var v in Store.Read<List<Visit>>(fileName) ?? []) h.visits[Identity(v.Url)] = v;
        return h;
    }

    public void Save() => Store.Write(FileName,
        visits.Values.OrderByDescending(v => Score(v, now())).Take(Cap).ToList());

    /// <summary>The same page however it was written: no scheme, no www, no trailing slash, no fragment.</summary>
    public static string Identity(string url)
    {
        var s = url;
        var hash = s.IndexOf('#');
        if (hash >= 0) s = s[..hash];
        var sep = s.IndexOf("://", StringComparison.Ordinal);
        if (sep >= 0) s = s[(sep + 3)..];
        s = Address.WithoutWww(s);
        if (s.EndsWith('/')) s = s[..^1];
        return s.ToLowerInvariant();
    }

    public void Record(string url, string title)
    {
        if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return;
        var key = Identity(url);
        if (!visits.TryGetValue(key, out var v)) visits[key] = v = new Visit { Url = url };
        v.Url = url;
        if (!string.IsNullOrWhiteSpace(title)) v.Title = title;
        v.Count++;
        v.Last = now();
    }

    /// <summary>A visit read from another browser: kept as it was, the later time and the sum of the counts when the page is already here.</summary>
    public bool Merge(string url, string title, int count, DateTime last)
    {
        if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return false;
        var key = Identity(url);
        if (!visits.TryGetValue(key, out var v)) { visits[key] = new Visit { Url = url, Title = title ?? "", Count = Math.Max(1, count), Last = last }; return true; }
        if (string.IsNullOrWhiteSpace(v.Title) && !string.IsNullOrWhiteSpace(title)) v.Title = title;
        v.Count += Math.Max(0, count);
        if (last > v.Last) v.Last = last;
        return false;
    }

    public void Retitle(string url, string title)
    {
        if (visits.TryGetValue(Identity(url), out var v) && !string.IsNullOrWhiteSpace(title)) v.Title = title;
    }

    public void Clear() => visits.Clear();

    public bool Remove(string url) => visits.Remove(Identity(url));

    /// <summary>Clear browsing data's time ranges: everything since the moment given.</summary>
    public int ClearSince(DateTime since) =>
        visits.Where(kv => kv.Value.Last >= since).Select(kv => kv.Key).ToList().Count(visits.Remove);

    /// <summary>The History page: newest first, filtered by words in the title or address.</summary>
    public List<Visit> Query(string typed, int limit = 500)
    {
        var words = Fuzzy.Words(typed ?? "");
        return visits.Values
            .Where(v => words.Count == 0 || words.All(w => (History.KeyOf(v.Url) + " " + v.Title).Contains(w, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(v => v.Last).Take(limit).ToList();
    }

    static double Score(Visit v, DateTime at) =>
        v.Count * Math.Exp(-Math.Max(0, (at - v.Last).TotalDays) / 30);

    /// <summary>What you could type to get here: the address without scheme or www.</summary>
    public static string KeyOf(string url)
    {
        var s = url;
        var sep = s.IndexOf("://", StringComparison.Ordinal);
        if (sep >= 0) s = s[(sep + 3)..];
        s = Address.WithoutWww(s);
        return s.EndsWith('/') && s.IndexOf('/') == s.Length - 1 ? s[..^1] : s;
    }

    /// <summary>Best matches first. An empty field proposes nothing.</summary>
    public List<Suggestion> Suggestions(string typed, int limit = 5)
    {
        var needle = typed.Trim().ToLowerInvariant();
        if (needle.Length == 0) return [];
        var words = Fuzzy.Words(needle);
        var at = now();
        var scored = new List<(Suggestion s, double score)>();
        foreach (var v in visits.Values)
        {
            var key = KeyOf(v.Url);
            var findable = (key + " " + v.Title).ToLowerInvariant();
            if (!words.All(w => findable.Contains(w, StringComparison.Ordinal))) continue;
            var fit = Fit(needle, words, key.ToLowerInvariant(), findable);
            if (fit is null) continue;
            scored.Add((new Suggestion(key, v.Title, v.Url, SuggestionKind.Visited), fit.Value * Math.Log(2 + Score(v, at))));
        }
        var visitedHosts = visits.Values.Select(v => Address.Host(v.Url)).Where(h => h is not null).ToHashSet();
        foreach (var (host, title) in Known.Sites)
        {
            if (visitedHosts.Contains(host)) continue;
            var findable = title.ToLowerInvariant() + " " + host;
            if (!words.All(w => findable.Contains(w, StringComparison.Ordinal))) continue;
            var fit = Fit(needle, words, host, findable);
            if (fit is null) continue;
            scored.Add((new Suggestion(host, title, "https://" + host, SuggestionKind.Known), fit.Value * Known.Weight));
        }
        if (scored.Count < limit && needle.Length >= 3)
        {
            foreach (var v in visits.Values)
            {
                var key = KeyOf(v.Url);
                var findable = (key + " " + v.Title).ToLowerInvariant();
                if (Fuzzy.Match(words, findable) != 0) continue;
                scored.Add((new Suggestion(key, v.Title, v.Url, SuggestionKind.Visited), 1 * Math.Log(2 + Score(v, at))));
            }
            foreach (var (host, title) in Known.Sites)
            {
                if (visitedHosts.Contains(host)) continue;
                if (Fuzzy.Match(words, title.ToLowerInvariant() + " " + host) != 0) continue;
                scored.Add((new Suggestion(host, title, "https://" + host, SuggestionKind.Known), Known.Weight));
            }
        }
        return scored
            .OrderByDescending(x => x.score).ThenBy(x => x.s.Key.Length)
            .Select(x => x.s).DistinctBy(s => Identity(s.Url)).Take(limit).ToList();
    }

    /// <summary>4 address starts with it, 3 a word of the host does, 2 every word starts a word, 1 inside a word.</summary>
    static double? Fit(string needle, List<string> words, string address, string findable)
    {
        if (address.StartsWith(needle, StringComparison.Ordinal)) return 4;
        var end = address.IndexOfAny(['/', '?', '#']);
        var host = end < 0 ? address : address[..end];
        if (StartsWord(needle, host)) return 3;
        if (needle.Length < 3) return null;
        return words.All(w => StartsWord(w, findable)) ? 2 : 1;
    }

    static bool StartsWord(string word, string text)
    {
        var from = 0;
        while (from <= text.Length - word.Length)
        {
            var hit = text.IndexOf(word, from, StringComparison.Ordinal);
            if (hit < 0) return false;
            if (hit == 0 || !char.IsLetterOrDigit(text[hit - 1])) return true;
            from = hit + 1;
        }
        return false;
    }

    /// <summary>What the field draws greyed after the caret: the rest of the best match, when it carries on from what was typed.</summary>
    public static string? Completion(string typed, IReadOnlyList<Suggestion> options)
    {
        if (typed.Length == 0 || options.Count == 0) return null;
        var single = typed.Length == 1;
        if (single && options[0].Kind != SuggestionKind.Visited) return null;
        foreach (var o in single ? options.Take(1) : options)
        {
            if (o.Kind is not (SuggestionKind.Visited or SuggestionKind.Bookmark or SuggestionKind.Open or SuggestionKind.Known)) continue;
            var key = o.Key;
            if (key.Length < typed.Length) continue;
            if (single && key.IndexOfAny(['/', '?', '#']) >= 0) continue;
            if (!key.StartsWith(typed, StringComparison.OrdinalIgnoreCase)) continue;
            var rest = key[typed.Length..];
            var completed = Address.Url(typed + rest);
            if (completed is null || Identity(completed) != Identity(o.Url)) continue;
            return rest.Length == 0 ? null : rest;
        }
        return null;
    }
}
