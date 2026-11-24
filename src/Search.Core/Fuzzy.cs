namespace Search.Core;

/// <summary>
/// Typos in the address field: a word with a letter wrong, missing or
/// swapped still finds what it meant. Port of Sources/Search/Fuzzy.swift.
/// </summary>
public static class Fuzzy
{
    /// <summary>1 if every word is in text as typed, 0 if one needed a typo forgiven, null if one isn't there.</summary>
    public static int? Match(IReadOnlyList<string> typed, string text)
    {
        var slipped = false;
        foreach (var word in typed)
        {
            if (text.Contains(word, StringComparison.Ordinal)) continue;
            if (!Nearly(word, text)) return null;
            slipped = true;
        }
        return slipped ? 0 : 1;
    }

    /// <summary>"dwn" for downloads: the letters in order, starting with the first.</summary>
    public static bool Abbreviates(string typed, string word)
    {
        if (typed.Length == 0 || word.Length == 0 || typed[0] != word[0]) return false;
        var i = 1;
        for (var j = 1; j < word.Length && i < typed.Length; j++)
            if (word[j] == typed[i]) i++;
        return i == typed.Length;
    }

    /// <summary>Whether a word in text starts with typed, allowing one typo (two in long words). None under four letters.</summary>
    public static bool Nearly(string typed, string text)
    {
        if (typed.Length < 4) return false;
        var allowed = typed.Length >= 8 ? 2 : 1;
        for (var start = 0; start < text.Length; start++)
        {
            if (start > 0 && InWord(text[start - 1])) continue;
            var end = start;
            while (end < text.Length && InWord(text[end])) end++;
            if (end == start) continue;
            var word = text.AsSpan(start, end - start);
            if (PrefixDistance(typed, word) <= allowed) return true;
            start = end;
        }
        return false;
    }

    static bool InWord(char c) => char.IsLetterOrDigit(c) || c > 127;

    /// <summary>Damerau–Levenshtein distance between typed and the best prefix of word.</summary>
    static int PrefixDistance(string a, ReadOnlySpan<char> b)
    {
        var n = a.Length;
        var m = Math.Min(b.Length, n + 2);
        var d = new int[n + 1, m + 1];
        for (var i = 0; i <= n; i++) d[i, 0] = i;
        for (var j = 0; j <= m; j++) d[0, j] = j;
        for (var i = 1; i <= n; i++)
            for (var j = 1; j <= m; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                var v = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1]) v = Math.Min(v, d[i - 2, j - 2] + 1);
                d[i, j] = v;
            }
        var best = int.MaxValue;
        for (var j = 0; j <= m; j++) best = Math.Min(best, d[n, j]);
        return best;
    }

    public static List<string> Words(string typed) =>
        typed.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
}
