namespace Search.Core;

/// <summary>
/// What was open last time. Port of the essentials of Sources/Search/Session.swift
/// and Pins.swift: tabs, which one was live, and the pins.
/// </summary>
public sealed class SessionEntry
{
    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
    public Guid? PinId { get; set; }
    /// <summary>A pin's own page, the one it was pinned at.</summary>
    public string? Home { get; set; }
    public string? Name { get; set; }
    public bool Muted { get; set; }
}

public sealed class SessionShape
{
    public List<SessionEntry> Tabs { get; set; } = [];
    public int Active { get; set; }
    public double? SideWidth { get; set; }
    public WindowPlacement? Placement { get; set; }
}

public sealed class WindowPlacement
{
    public double Left { get; set; }
    public double Top { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public bool Maximized { get; set; }
}

public static class Session
{
    /// <summary>The first space of the first profile keeps the file there has always been; every other space has its own.</summary>
    public static string FileFor(Guid? space) => space is null || space == Spaces.FirstId ? "session.json" : $"session-{space:N}.json";
    public static SessionShape? Load(Guid? space = null) => Store.Read<SessionShape>(FileFor(space));
    public static void Save(SessionShape shape, Guid? space = null) => Store.Write(FileFor(space), shape);
}

/// <summary>Tabs closed, newest last, for Ctrl+Shift+T.</summary>
public sealed class ClosedTabs
{
    readonly List<(SessionEntry entry, int index)> stack = [];
    public const int Limit = 50;
    public void Push(SessionEntry e, int index)
    {
        stack.Add((e, index));
        if (stack.Count > Limit) stack.RemoveAt(0);
    }
    public (SessionEntry entry, int index)? Pop()
    {
        if (stack.Count == 0) return null;
        var last = stack[^1];
        stack.RemoveAt(stack.Count - 1);
        return last;
    }
    public int Count => stack.Count;
}
