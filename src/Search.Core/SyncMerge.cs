using System.Text.Json.Serialization;

namespace Search.Core;

/// <summary>One item as one PC last had it: its JSON (null for one deleted), when it was set (seconds since 1970) and by which device.</summary>
public sealed class SyncEntry
{
    [JsonPropertyName("v")] public string? V { get; set; }
    [JsonPropertyName("t")] public double T { get; set; }
    [JsonPropertyName("d")] public string D { get; set; } = "";
}

/// <summary>One PC's file in one scope's folder: only that PC writes it.</summary>
public sealed class SyncFile
{
    [JsonPropertyName("format")] public int Format { get; set; }
    [JsonPropertyName("device")] public string Device { get; set; } = "";
    [JsonPropertyName("name")] public string? Name { get; set; }
    /// <summary>"app", or "profile-&lt;id&gt;": a file in the wrong folder is not read.</summary>
    [JsonPropertyName("scope")] public string Scope { get; set; } = "";
    [JsonPropertyName("written")] public double Written { get; set; }
    [JsonPropertyName("items")] public Dictionary<string, SyncEntry> Items { get; set; } = [];
}

/// <summary>
/// The merge behind folder sync (SyncMerge.swift): plain values in, plain values out. Items are a flat map, key → small JSON.
/// A deletion is an entry with no value (a tombstone), so "gone" travels as surely as "changed". The latest stamp wins, then the
/// device id, then a tombstone, then the larger text: the same answer on every PC whatever order it reads them in.
/// </summary>
public static class SyncMerge
{
    public const int Format = 1;
    public const double TombstoneLife = 365d * 24 * 60 * 60;
    public const int WholeKindFloor = 5;
    /// <summary>Kinds whose vanishing all at once is a file that didn't read, not a choice: they come back from the folder.</summary>
    public static readonly string[] WholeKinds = ["bm:", "bmfolder:", "pin:", "pinfolder:", "space:"];

    public static bool Wins(SyncEntry a, SyncEntry b)
    {
        if (a.T != b.T) return a.T > b.T;
        if (a.D != b.D) return string.CompareOrdinal(a.D, b.D) > 0;
        if (a.V is null && b.V is null) return false;
        if (a.V is null) return true;
        if (b.V is null) return false;
        return string.CompareOrdinal(a.V, b.V) > 0;
    }

    public static Dictionary<string, SyncEntry> Merge(IEnumerable<Dictionary<string, SyncEntry>> maps)
    {
        var o = new Dictionary<string, SyncEntry>();
        foreach (var map in maps)
            foreach (var (key, entry) in map)
                if (!o.TryGetValue(key, out var held) || Wins(entry, held)) o[key] = entry;
        return o;
    }

    /// <summary>The items as this PC has them now, over what it last agreed. What differs is an edit made here, stamped later than anything it has seen;
    /// what it agreed on and no longer has is a tombstone. Joining a folder other PCs use stamps 0, so what is there wins where both have an item.</summary>
    public static Dictionary<string, SyncEntry> Stamp(Dictionary<string, string> current, Dictionary<string, SyncEntry> baseMap, string device, double now, bool joining = false, Func<string, string, bool>? understood = null)
    {
        var clock = joining ? 0 : Math.Max(now, (baseMap.Values.Select(e => e.T).DefaultIfEmpty(0).Max()) + 0.001);
        var o = new Dictionary<string, SyncEntry>(baseMap);
        foreach (var (key, value) in current)
            if (!baseMap.TryGetValue(key, out var was) || was.V != value) o[key] = new SyncEntry { V = value, T = clock, D = device };
        var lost = WholeKinds.Where(kind => !current.Keys.Any(k => k.StartsWith(kind, StringComparison.Ordinal))
            && baseMap.Count(b => b.Key.StartsWith(kind, StringComparison.Ordinal) && b.Value.V is not null) >= WholeKindFloor).ToHashSet();
        if (lost.Contains("pin:")) lost.Add("pinorder:");
        if (lost.Contains("space:")) lost.Add("pinfolder:");
        foreach (var (key, entry) in baseMap)
        {
            if (entry.V is null || current.ContainsKey(key) || (understood is not null && !understood(key, entry.V))) continue;
            if (lost.Any(k => key.StartsWith(k, StringComparison.Ordinal))) continue;
            o[key] = new SyncEntry { V = null, T = clock, D = device };
        }
        return o;
    }

    /// <summary>One pass as every PC runs it: this PC's items stamped over its base (null before the first pass), merged with the others', old deletions forgotten.</summary>
    public static Dictionary<string, SyncEntry> Pass(Dictionary<string, string> current, Dictionary<string, SyncEntry>? baseMap, List<Dictionary<string, SyncEntry>> theirs, string device, double now, Func<string, string, bool>? understood = null)
    {
        var mine = Stamp(current, baseMap ?? [], device, now, baseMap is null && theirs.Count > 0, understood);
        return Prune(Merge([mine, .. theirs]), now);
    }

    public static Dictionary<string, SyncEntry> Prune(Dictionary<string, SyncEntry> map, double now) =>
        map.Where(kv => kv.Value.V is not null || kv.Value.T >= now - TombstoneLife).ToDictionary(kv => kv.Key, kv => kv.Value);

    public static Dictionary<string, string> Live(Dictionary<string, SyncEntry> map) =>
        map.Where(kv => kv.Value.V is not null).ToDictionary(kv => kv.Key, kv => kv.Value.V!);

    /// <summary>Another PC's file, if it is one this build can read for this scope: damaged, foreign, other-format and wrong-folder files are null.</summary>
    public static SyncFile? Decode(string json, string scope)
    {
        try
        {
            var f = System.Text.Json.JsonSerializer.Deserialize<SyncFile>(json);
            if (f is null || f.Format != Format || f.Scope != scope || f.Device.Length == 0) return null;
            f.Items = (f.Items ?? []).Where(kv => double.IsFinite(kv.Value?.T ?? double.NaN) && kv.Value!.T >= 0 && !string.IsNullOrEmpty(kv.Value.D)).ToDictionary(kv => kv.Key, kv => kv.Value);
            return f;
        }
        catch (System.Text.Json.JsonException) { return null; }
    }

    public static string Encode(SyncFile f) => System.Text.Json.JsonSerializer.Serialize(f);

    /// <summary>A list's order applied to the ids that are there: the order's first, then the rest by id, so an item added on one PC while another reordered is kept, at the end, the same on every PC.</summary>
    public static List<string> Ordered(IEnumerable<string> ids, IEnumerable<string> order)
    {
        var have = ids.ToHashSet(StringComparer.Ordinal);
        var o = new List<string>(); var seen = new HashSet<string>();
        foreach (var id in order) if (have.Contains(id) && seen.Add(id)) o.Add(id);
        o.AddRange(have.Except(seen).Order(StringComparer.Ordinal));
        return o;
    }
}
