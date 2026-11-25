namespace Search.Core;

/// <summary>
/// A named section of ordinary tabs in one space's column (TabGroup.swift). The tabs stay ordinary tabs; each one
/// says which group it is in, and the members of a group sit side by side.
/// </summary>
public sealed class TabGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public bool Collapsed { get; set; }
    /// <summary>One of <see cref="Spaces.ColourNames"/>, or none.</summary>
    public int? Colour { get; set; }
}

public static class Groups
{
    /// <summary>Every group is one run: a member found after the run broke is let go of; a group with no members is dropped.</summary>
    public static List<Guid?> Normalize(IReadOnlyList<Guid?> members, IReadOnlyCollection<Guid> known)
    {
        var result = new List<Guid?>(members.Count);
        var done = new HashSet<Guid>();
        Guid? current = null;
        foreach (var g in members)
        {
            if (g is not Guid id || !known.Contains(id)) { current = null; result.Add(null); continue; }
            if (current == id) { result.Add(id); continue; }
            if (done.Contains(id)) { current = null; result.Add(null); continue; }
            done.Add(id); current = id; result.Add(id);
        }
        return result;
    }

    /// <summary>The group a tab belongs to once it is set down between BEFORE and AFTER: a tab dropped inside a group joins it, one dropped beside the end of its own group stays, any other is in none.</summary>
    public static Guid? Adopt(Guid? before, Guid? after, Guid? own)
    {
        if (before is not null && before == after) return before;
        if (own is not null && (before == own || after == own)) return own;
        return null;
    }

    /// <summary>The groups that still hold a tab, in the order they first appear.</summary>
    public static List<TabGroup> Kept(IEnumerable<TabGroup> groups, IEnumerable<Guid?> members)
    {
        var used = members.Where(m => m is not null).Select(m => m!.Value).ToHashSet();
        return groups.Where(g => used.Contains(g.Id)).ToList();
    }
}

/// <summary>A folder among the pinned rows: it holds pinned rows and other folders, and folds (PinFolders.swift).</summary>
public sealed class PinFolder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    /// <summary>The folder that holds this one; none at the top of the rows.</summary>
    public Guid? Parent { get; set; }
    public bool Collapsed { get; set; }
    /// <summary>A live folder is filled from a web feed (LiveFolders.swift): the feed's address, and its latest posts.</summary>
    public string? Feed { get; set; }
    public List<FeedItem>? Items { get; set; }
    [System.Text.Json.Serialization.JsonIgnore] public bool IsLive => !string.IsNullOrEmpty(Feed);
}

/// <summary>The shape of the folders, worked out: what is drawn, what may go where. Pure, so tests can hold it.</summary>
public static class PinTree
{
    /// <summary>A folder in a folder in a folder, and no deeper: the column has not the room.</summary>
    public const int MaxDepth = 3;

    public readonly record struct Line(PinFolder? Folder, Guid? Pin, int Depth);

    /// <summary>The folders as they can be trusted: a parent that is gone, a folder that would hold itself, or a cycle puts the folder at the top; one too deep goes into the deepest folder above it that may hold it.</summary>
    public static List<PinFolder> Sanitized(IEnumerable<PinFolder> folders)
    {
        var seen = new HashSet<Guid>();
        var list = folders.Where(f => seen.Add(f.Id)).ToList();
        var ids = list.Select(f => f.Id).ToHashSet();
        foreach (var f in list)
            if (f.Parent is Guid p && (!ids.Contains(p) || p == f.Id)) f.Parent = null;
        foreach (var f in list)
        {
            var visited = new HashSet<Guid> { f.Id };
            var current = f.Parent;
            while (current is Guid id && list.FirstOrDefault(x => x.Id == id) is { } up)
            {
                if (!visited.Add(id)) { f.Parent = null; break; }
                current = up.Parent;
            }
        }
        foreach (var f in list)
        {
            var chain = Ancestors(f.Id, list);
            while (chain.Count >= MaxDepth) { chain.RemoveAt(0); f.Parent = chain.Count > 0 ? chain[0] : null; }
        }
        return list;
    }

    /// <summary>The folders above one, nearest first.</summary>
    public static List<Guid> Ancestors(Guid id, IReadOnlyList<PinFolder> folders)
    {
        var chain = new List<Guid>();
        var current = folders.FirstOrDefault(f => f.Id == id)?.Parent;
        while (current is Guid p && !chain.Contains(p) && folders.FirstOrDefault(f => f.Id == p) is { } found)
        {
            chain.Add(p);
            current = found.Parent;
        }
        return chain;
    }

    /// <summary>1 for a folder at the top, 2 for one inside it, and so on.</summary>
    public static int Depth(Guid id, IReadOnlyList<PinFolder> folders) => Ancestors(id, folders).Count + 1;

    /// <summary>How many levels a folder and what it holds take: 1 for one holding no folder.</summary>
    public static int Height(Guid id, IReadOnlyList<PinFolder> folders)
    {
        var best = 1;
        var stack = new Stack<(Guid, int)>();
        stack.Push((id, 1));
        var seen = new HashSet<Guid>();
        while (stack.Count > 0)
        {
            var (current, level) = stack.Pop();
            if (!seen.Add(current)) continue;
            best = Math.Max(best, level);
            foreach (var c in folders.Where(f => f.Parent == current)) stack.Push((c.Id, level + 1));
        }
        return best;
    }

    /// <summary>May FOLDER go into PARENT: not into itself or what it holds, and not so that it would run deeper than <see cref="MaxDepth"/>.</summary>
    public static bool CanHold(Guid? parent, Guid folder, IReadOnlyList<PinFolder> folders)
    {
        if (parent is not Guid p) return true;
        if (p == folder || Ancestors(p, folders).Contains(folder)) return false;
        return Depth(p, folders) + Height(folder, folders) <= MaxDepth;
    }

    /// <summary>Inside a folder, or at the top, the folders come first, in their order, then the pins, in theirs. A folded folder shows its heading alone.</summary>
    public static List<Line> Lines(IReadOnlyList<PinFolder> folders, IReadOnlyList<(Guid Id, Guid? Folder)> pins)
    {
        var ok = folders.Select(f => f.Id).ToHashSet();
        var lines = new List<Line>();
        void Walk(Guid? parent, int depth)
        {
            foreach (var f in folders.Where(x => x.Parent == parent))
            {
                lines.Add(new Line(f, null, depth));
                if (!f.Collapsed) Walk(f.Id, depth + 1);
            }
            foreach (var p in pins.Where(x => (x.Folder is Guid g && ok.Contains(g) ? x.Folder : null) == parent))
                lines.Add(new Line(null, p.Id, depth));
        }
        Walk(null, 0);
        return lines;
    }
}
