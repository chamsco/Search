using System.Text.Json.Serialization;

namespace Search.Core;

/// <summary>
/// A key with its modifiers, written the way Windows menus write it: Ctrl+Alt+Shift+K.
/// The key part is a plain name (a letter, a digit, F5, Tab, Left, ",", "=").
/// </summary>
public sealed record KeyCombo(string Key, bool Ctrl = false, bool Alt = false, bool Shift = false)
{
    public override string ToString() => (Ctrl ? "Ctrl+" : "") + (Alt ? "Alt+" : "") + (Shift ? "Shift+" : "") + Key;

    public static KeyCombo? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        bool ctrl = false, alt = false, shift = false;
        var rest = text.Trim();
        while (true)
        {
            if (rest.StartsWith("Ctrl+", StringComparison.OrdinalIgnoreCase)) { ctrl = true; rest = rest[5..]; }
            else if (rest.StartsWith("Alt+", StringComparison.OrdinalIgnoreCase)) { alt = true; rest = rest[4..]; }
            else if (rest.StartsWith("Shift+", StringComparison.OrdinalIgnoreCase)) { shift = true; rest = rest[6..]; }
            else break;
        }
        if (rest.Length == 0) return null;
        return new KeyCombo(rest.Length == 1 ? rest.ToUpperInvariant() : rest, ctrl, alt, shift);
    }

    /// <summary>Keys that type something: with no Ctrl or Alt they belong to the page, not to a command.</summary>
    public bool Types => !Ctrl && !Alt && !(Key.Length > 1 && (Key.StartsWith('F') && Key.Length <= 3 || Key is "Escape" or "Delete" or "PageUp" or "PageDown" or "Home" or "End"));

    /// <summary>The editing keys the page keeps for itself.</summary>
    public bool Reserved => Ctrl && !Alt && !Shift && Key is "C" or "V" or "X" or "A" or "Z";
}

public enum CommandSection { App, File, Edit, View, Tabs, Bookmarks, History, Spaces }

/// <summary>One thing a key can do (Shortcuts.swift's Command): an id, a name, a section and the key it starts with, if any.</summary>
public sealed record Command(string Id, string Title, CommandSection Section, KeyCombo? Default);

public static class Commands
{
    static KeyCombo K(string key, bool ctrl = true, bool alt = false, bool shift = false) => new(key, ctrl, alt, shift);

    /// <summary>Every command, in the order of the Mac's menus. Keys are the Mac's with Ctrl for ⌘ where Windows has not taken the key already.</summary>
    public static readonly IReadOnlyList<Command> All = new List<Command>
    {
        new("app.settings", "Settings…", CommandSection.App, K(",")),
        new("app.quit", "Quit Search", CommandSection.App, K("Q", shift: true)),
        new("app.library", "Library", CommandSection.App, K("L", shift: true)),
        new("app.extensions", "Extensions", CommandSection.App, K("E", shift: true)),

        new("file.newWindow", "New Window", CommandSection.File, K("N")),
        new("file.newTab", "New Tab", CommandSection.File, K("T")),
        new("file.reopen", "Reopen Closed Tab", CommandSection.File, K("T", shift: true)),
        new("file.openAddress", "Open Address…", CommandSection.File, K("L")),
        new("file.openFile", "Open File…", CommandSection.File, K("O")),
        new("file.closeTab", "Close Tab", CommandSection.File, K("W")),
        new("file.closeWindow", "Close Window", CommandSection.File, K("W", shift: true)),
        new("file.print", "Print…", CommandSection.File, K("P")),

        new("edit.find", "Find on Page…", CommandSection.Edit, K("F")),
        new("edit.findNext", "Find Next", CommandSection.Edit, K("G")),
        new("edit.findPrevious", "Find Previous", CommandSection.Edit, K("G", shift: true)),
        new("edit.copyCleanLink", "Copy Clean Link", CommandSection.Edit, K("C", alt: true, shift: true)),

        new("view.fold", "Hide Sidebar or Tab Bar", CommandSection.View, K("S", shift: true)),
        new("view.reload", "Reload Page", CommandSection.View, K("R")),
        new("view.reloadOrigin", "Reload Page From Origin", CommandSection.View, K("R", alt: true)),
        new("view.reader", "Reading Mode", CommandSection.View, K("R", alt: true, shift: true)),
        new("view.float", "Float Video", CommandSection.View, K("P", shift: true)),
        new("view.zoomIn", "Zoom In", CommandSection.View, K("=")),
        new("view.zoomOut", "Zoom Out", CommandSection.View, K("-")),
        new("view.actualSize", "Actual Size", CommandSection.View, K("0")),
        new("view.inspector", "Web Inspector", CommandSection.View, K("I", alt: true)),
        new("view.fullScreen", "Full Screen", CommandSection.View, K("F11", ctrl: false)),

        new("tabs.back", "Back", CommandSection.Tabs, K("Left", ctrl: false, alt: true)),
        new("tabs.forward", "Forward", CommandSection.Tabs, K("Right", ctrl: false, alt: true)),
        new("tabs.next", "Next Tab", CommandSection.Tabs, K("Tab")),
        new("tabs.previous", "Previous Tab", CommandSection.Tabs, K("Tab", shift: true)),
        new("tabs.split", "Split Current Page", CommandSection.Tabs, K("N", alt: true)),
        new("tabs.focusLeftPane", "Focus Left Page", CommandSection.Tabs, K("Left", alt: true)),
        new("tabs.focusRightPane", "Focus Right Page", CommandSection.Tabs, K("Right", alt: true)),
        new("tabs.pin", "Pin Tab", CommandSection.Tabs, K("D", shift: true)),
        new("tabs.duplicate", "Duplicate Tab", CommandSection.Tabs, K("D")),
        new("tabs.copyAddress", "Copy Address", CommandSection.Tabs, K("C", shift: true)),
        new("tabs.pasteAndGo", "Paste and Go", CommandSection.Tabs, K("V", shift: true)),

        new("bookmarks.add", "Add This Page", CommandSection.Bookmarks, K("B", shift: true)),
        new("bookmarks.bar", "Show Bookmarks Bar", CommandSection.Bookmarks, K("B", alt: true)),

        new("history.show", "Show History…", CommandSection.History, K("Y")),
        new("history.downloads", "Downloads…", CommandSection.History, K("J", shift: true)),
        new("history.clearData", "Clear Browsing Data…", CommandSection.History, K("Delete", shift: true)),
    }.Concat(Enumerable.Range(1, 9).Select(n =>
        new Command($"spaces.{n}", $"Space {n}", CommandSection.Spaces, K(n.ToString(), alt: true)))).ToList();

    public static Command? Find(string id) => All.FirstOrDefault(c => c.Id == id);
}

/// <summary>
/// The keys as they are: each command's own, with what you changed in Settings › Keyboard on top.
/// A command can be given another key, or none; the key it took is taken from whoever had it.
/// Kept in shortcuts.json as { "id": "Ctrl+Shift+K" } with "" for none.
/// </summary>
public sealed class Shortcuts
{
    const string FileName = "shortcuts.json";
    Dictionary<string, string> changed = new();

    public event Action? Changed;

    public static Shortcuts Load() => new() { changed = Store.Read<Dictionary<string, string>>(FileName) ?? new() };

    public KeyCombo? KeyFor(string id) =>
        changed.TryGetValue(id, out var text) ? KeyCombo.Parse(text) : Commands.Find(id)?.Default;

    public bool IsChanged(string id) => changed.ContainsKey(id);

    /// <summary>Which command a key belongs to, if any.</summary>
    public Command? CommandFor(KeyCombo combo) => Commands.All.FirstOrDefault(c => KeyFor(c.Id) == combo);

    /// <summary>Why a key cannot be used, or null.</summary>
    public static string? Refuse(KeyCombo combo)
    {
        if (combo.Types) return "A key on its own types into the page: add Ctrl or Alt.";
        if (combo.Reserved) return $"{combo} copies, pastes or edits in the page.";
        return null;
    }

    /// <summary>Gives a command a key (or none, with null). Returns the command that had to give the key up, if one did.</summary>
    public Command? Set(string id, KeyCombo? combo)
    {
        if (Commands.Find(id) is null) return null;
        Command? taken = null;
        if (combo is not null && CommandFor(combo) is { } other && other.Id != id)
        {
            taken = other;
            changed[other.Id] = "";
        }
        changed[id] = combo?.ToString() ?? "";
        if (changed[id] == (Commands.Find(id)?.Default?.ToString() ?? "")) changed.Remove(id);
        Save();
        return taken;
    }

    /// <summary>The command's own key again. If another command has taken it since, that one goes without.</summary>
    public Command? Reset(string id)
    {
        var own = Commands.Find(id)?.Default;
        changed.Remove(id);
        Command? taken = null;
        if (own is not null && CommandFor(own) is { } other && other.Id != id) { taken = other; changed[other.Id] = ""; }
        Save();
        return taken;
    }

    public void ResetAll() { changed.Clear(); Save(); }

    void Save() { Store.Write(FileName, changed); Changed?.Invoke(); }
}
