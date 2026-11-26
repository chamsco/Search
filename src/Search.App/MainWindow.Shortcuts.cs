using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using Search.Core;

namespace Search.App;

/// <summary>
/// Keys (Shortcuts.swift): every command has a name, a section and a key you can change in Settings › Keyboard.
/// A pressed key becomes a KeyCombo, which names a command, which RunCommand does. Keys Windows or the browser
/// engine own regardless (F5, F6, Alt+D, F12, Ctrl+PgUp/PgDn, Ctrl+1–9, the browser keys) stay as they were.
/// </summary>
public partial class MainWindow
{
    /// <summary>The key's name as KeyCombo writes it, or null for a key no command uses.</summary>
    static string? KeyName(Key k) => k switch
    {
        >= Key.A and <= Key.Z => k.ToString(),
        >= Key.D0 and <= Key.D9 => ((int)k - (int)Key.D0).ToString(),
        >= Key.NumPad0 and <= Key.NumPad9 => ((int)k - (int)Key.NumPad0).ToString(),
        >= Key.F1 and <= Key.F12 => k.ToString(),
        Key.OemComma => ",", Key.OemPeriod => ".", Key.OemPlus or Key.Add => "=", Key.OemMinus or Key.Subtract => "-",
        Key.OemOpenBrackets => "[", Key.OemCloseBrackets => "]", Key.OemSemicolon => ";", Key.OemQuotes => "'",
        Key.OemQuestion => "/", Key.OemPipe => "\\", Key.OemTilde => "`",
        Key.Tab => "Tab", Key.Left => "Left", Key.Right => "Right", Key.Up => "Up", Key.Down => "Down",
        Key.Delete => "Delete", Key.Escape => "Escape", Key.Enter => "Enter", Key.Space => "Space", Key.Back => "Backspace",
        Key.PageUp => "PageUp", Key.PageDown => "PageDown", Key.Home => "Home", Key.End => "End",
        _ => null,
    };

    bool capturingKey;

    /// <summary>Next or previous space, stopping at the ends.</summary>
    void StepSpace(int by)
    {
        var mine = Shared.Book.In(profileId).ToList();
        var here = mine.FindIndex(x => x.Id == space.Id);
        if (here >= 0) SwitchSpaceBy(Math.Clamp(here + by, 0, mine.Count - 1));
    }

    /// <summary>Does what the command says; false when it has nothing to do just now, so the key goes on to the page.</summary>
    bool RunCommand(string id)
    {
        var core = live?.View?.CoreWebView2;
        switch (id)
        {
            case "app.settings": OpenPage("search://settings"); break;
            case "app.quit": App.Quitting = true; Application.Current.Shutdown(); break;
            case "app.library": ToggleLibrary(); break;
            case "app.extensions": ToggleLibrary("extensions"); break;
            case "app.passwords": OpenPage("search://settings/passwords"); break;

            case "file.newWindow": NewWindow(); break;
            case "file.newTab": NewTab(); break;
            case "file.reopen": Reopen(); break;
            case "file.openAddress": BeginEditing(); break;
            case "file.openFile": OpenFile(); break;
            case "file.closeTab": if (live is null) return false; Close(live); break;
            case "file.closeWindow": Close(); break;
            case "file.print": Print(); break;
            case "file.share": Share_Click(this, new RoutedEventArgs()); break;
            case "file.import": OpenPage("search://settings/import"); break;

            case "edit.find": OpenFind(); break;
            case "edit.findNext": FindStep(1); break;
            case "edit.findPrevious": FindStep(-1); break;
            case "edit.copyCleanLink": CopyCleanLink(); break;

            case "view.fold": ToggleSide(); break;
            case "view.reload": core?.Reload(); break;
            case "view.reloadOrigin": core?.CallDevToolsProtocolMethodAsync("Page.reload", "{\"ignoreCache\":true}"); break;
            case "view.reader": ToggleReader(); break;
            case "view.float": PictureInPicture(); break;
            case "view.zoomIn": Zoom(1); break;
            case "view.zoomOut": Zoom(-1); break;
            case "view.actualSize": Zoom(0); break;
            case "view.inspector": ToggleDevTools(); break;
            case "view.fullScreen": ToggleFullScreen(); break;
            case "view.screenshot": Shot_Click(this, new RoutedEventArgs()); break;
            case "view.style": OpenPage("search://settings/sites"); break;

            case "tabs.back": if (core?.CanGoBack == true) core.GoBack(); break;
            case "tabs.forward": if (core?.CanGoForward == true) core.GoForward(); break;
            case "tabs.next": Cycle(1); break;
            case "tabs.previous": Cycle(-1); break;
            case "tabs.split": SplitKey(); break;
            case "tabs.focusLeftPane": FocusPane(-1); break;
            case "tabs.focusRightPane": FocusPane(1); break;
            case "tabs.pin": if (live is null) return false; if (live.IsPin) Unpin(live); else Pin(live); break;
            case "tabs.duplicate": if (live is null) return false; DuplicateTab(); break;
            case "tabs.copyAddress": if (live is not { IsBlank: false }) return false; Clipboard.SetText(live.Url); break;
            case "tabs.pasteAndGo": PasteAndGo(); break;
            case "tabs.rename": if (live is null) return false; BeginRenaming(live); break;
            case "tabs.copyMarkdown": if (live is not { IsBlank: false }) return false; Clipboard.SetText($"[{live.Label}]({live.Url})"); break;
            case "tabs.closeOthers": if (live is null) return false; foreach (var o in tabs.Where(o => o != live).ToList()) Close(o); break;

            case "bookmarks.add": AddBookmark(); break;
            case "bookmarks.bar": Shared.Prefs.Update(d => d.BookmarksBar = !d.BookmarksBar); break;

            case "history.show": OpenPage("search://history"); break;
            case "history.downloads": OpenPage("search://downloads"); break;
            case "history.clearData": ClearBrowsingDataPage(); break;

            case "spaces.next": StepSpace(1); break;
            case "spaces.previous": StepSpace(-1); break;

            default:
                if (id.StartsWith("spaces.") && int.TryParse(id[7..], out var n)) { SwitchSpaceBy(n - 1); break; }
                return false;
        }
        return true;
    }

    // MARK: - Settings › Keyboard

    object ShortcutsMessage() => new
    {
        type = "shortcuts",
        spacesOn = Shared.Prefs.Data.SpacesOn,
        items = Commands.All.Select(c => new
        {
            id = c.Id, title = c.Title, section = c.Section.ToString(),
            key = Shared.Shortcuts.KeyFor(c.Id)?.ToString() ?? "",
            own = c.Default?.ToString() ?? "",
            changed = Shared.Shortcuts.IsChanged(c.Id),
        }),
    };

    /// <summary>A key pressed in the Settings page's "press a key" box arrives as the combo's text; says what happened.</summary>
    void ShortcutsMessageHandled(string type, JsonElement m, CoreWebView2 core)
    {
        string S(string k) => m.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
        string note = "";
        switch (type)
        {
            case "shortcuts.get": break;
            case "shortcuts.capture": capturingKey = m.TryGetProperty("on", out var on) && on.ValueKind == JsonValueKind.True; return;
            case "shortcuts.set":
            {
                var combo = KeyCombo.Parse(S("key"));
                if (combo is not null && Shortcuts.Refuse(combo) is { } why) { note = why; break; }
                if (Shared.Shortcuts.Set(S("id"), combo) is { } taken) note = $"{combo} was {taken.Title}'s. That now has no key.";
                break;
            }
            case "shortcuts.reset":
                if (Shared.Shortcuts.Reset(S("id")) is { } lost) note = $"{Shared.Shortcuts.KeyFor(S("id"))} was {lost.Title}'s. That now has no key.";
                break;
            case "shortcuts.resetAll": Shared.Shortcuts.ResetAll(); break;
        }
        var msg = ShortcutsMessage();
        Send(core, msg);
        PostToPages(msg, "shortcuts");
        if (note.Length > 0) Send(core, new { type = "shortcuts.note", text = note });
    }
}
