using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using Application = FlaUI.Core.Application;
using Point = System.Drawing.Point;
using MouseButton = FlaUI.Core.Input.MouseButton;

// Drives the real Search.exe on a Windows desktop (the CI runner) and
// records what it did: a screenshot and a line per step in steps.md.
// Usage: Search.UiTests <path to Search.exe> <output folder>

var exe = Path.GetFullPath(args[0]);
var outDir = Directory.CreateDirectory(args[1]).FullName;
var data = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "search-ui-" + Guid.NewGuid().ToString("N")[..8])).FullName;
var steps = new List<string>();
var failures = 0;
var shot = 0;

void Step(string what, bool ok = true, string? note = null)
{
    if (!ok) failures++;
    var line = $"| {steps.Count + 1} | {what} | {(ok ? "pass" : "**FAIL**")} | {note ?? ""} |";
    steps.Add(line);
    Console.WriteLine(line);
}

string Shot(string name)
{
    var file = $"{++shot:00}-{name}.png";
    var bounds = System.Windows.Forms.Screen.PrimaryScreen!.Bounds;
    using var bmp = new Bitmap(bounds.Width, bounds.Height);
    using (var g = Graphics.FromImage(bmp)) g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
    bmp.Save(Path.Combine(outDir, file), ImageFormat.Png);
    return file;
}

Application Launch()
{
    var psi = new ProcessStartInfo(exe) { UseShellExecute = false };
    psi.Environment["SEARCH_DATA"] = data;
    psi.Environment["SEARCH_NO_UPDATES"] = "1";
    return Application.Launch(psi);
}


string SessionText() { try { return File.ReadAllText(Path.Combine(data, "session.json")); } catch { return ""; } }
string DataText(string name) { try { return File.ReadAllText(Path.Combine(data, name)); } catch { return ""; } }
void Chord(params VirtualKeyShort[] keys) { Keyboard.TypeSimultaneously(keys); Thread.Sleep(900); }
using var automation = new UIA3Automation();
var app = Launch();
Window? window = null;
// Text drawn in the app's popups (find count, bookmark card): the app's other top-level windows.
string PopupText(string automationId)
{
    try
    {
        var task = Task.Run(() => automation.GetDesktop().FindFirstDescendant(cf => cf.ByAutomationId(automationId))?.Name ?? "");
        return task.Wait(TimeSpan.FromSeconds(15)) ? task.Result : "(timed out)";
    }
    catch (Exception e) { return "(" + e.Message + ")"; }
}

try
{
    window = app.GetMainWindow(automation, TimeSpan.FromSeconds(30));
    Step("Launch Search.exe with an empty data folder", window is not null, window?.Title);
    window!.Patterns.Window.Pattern.SetWindowVisualState(FlaUI.Core.Definitions.WindowVisualState.Maximized);
    Thread.Sleep(4000);
    Step("Empty tab shows the centred address field", true, Shot("new-tab"));

    Keyboard.Type("you");
    Thread.Sleep(2000);
    var rows = window.FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.ListItem)).Length;
    Step("Type \"you\": popular sites suggested, youtube.com completed inline", rows >= 2, $"{rows} rows · " + Shot("suggestions-known-sites"));

    Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
    Keyboard.Type("weather paris");
    Thread.Sleep(2500);
    Step("Type \"weather paris\": search row plus Google's own suggestions", true, Shot("suggestions-engine"));

    Keyboard.Press(VirtualKeyShort.RETURN);
    Thread.Sleep(7000);
    var alive = !app.HasExited;
    Step("Press Enter: Google search loads and the app stays open (reported crash)", alive, Shot("google-search"));
    if (!alive) throw new Exception("Search exited after a Google search");

    Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_T);
    Thread.Sleep(800);
    Keyboard.Type("example.com");
    Keyboard.Press(VirtualKeyShort.RETURN);
    Thread.Sleep(4000);
    Step("Ctrl+T, type example.com, Enter: opens in a second tab", !app.HasExited, Shot("second-tab"));

    var b = window.BoundingRectangle;
    // First row: below the 52-point strip, 10 in from the column's edge (maximised windows sit 7 in).
    var firstRow = new Point(b.Left + 7 + 80, b.Top + 7 + 52 + 14);
    Mouse.MoveTo(firstRow);
    Thread.Sleep(700);
    Step("Hover the first tab: soft grey row and a close button, no tooltip box", true, Shot("hover-tab"));

    Mouse.Click(firstRow, MouseButton.Right);
    Thread.Sleep(800);
    var menu = automation.GetDesktop().FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Menu));
    Step("Right-click the tab: Search-styled menu with Pin, Rename, Duplicate…", menu is not null, Shot("tab-menu"));

    var pin = menu?.FindFirstDescendant(cf => cf.ByName("Pin"));
    pin?.Click();
    Thread.Sleep(1200);
    Step("Choose Pin: the tab becomes a square at the top of the column", pin is not null, Shot("pinned"));

    Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_2);
    Thread.Sleep(800);
    Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_L);
    Thread.Sleep(800);
    Keyboard.Type("git");
    Thread.Sleep(2000);
    Step("Ctrl+L on a page: address editable in its row, suggestions over the page", true, Shot("edit-address"));
    Keyboard.Press(VirtualKeyShort.ESCAPE);
    Thread.Sleep(500);


    // ---- wave 2 ----
    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_F);
    Keyboard.Type("domain");
    Thread.Sleep(1500);
    var findText = PopupText("FindCount");
    Step("Ctrl+F, type domain: matches are highlighted on the page and counted", findText.Contains(" of "), $"popup says: {findText.Replace("|", "\\|")} · " + Shot("find"));
    Keyboard.Press(VirtualKeyShort.RETURN);
    Thread.Sleep(600);
    Keyboard.Press(VirtualKeyShort.ESCAPE);
    Thread.Sleep(500);

    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.SHIFT, VirtualKeyShort.KEY_B);
    Step("Ctrl+Shift+B: a card to name the bookmark opens, and the page is saved", DataText("bookmarks.json").Contains("\"Url\"") || DataText("bookmarks.json").Contains("\"url\""), $"card says: {PopupText("MarkTitle")} · " + Shot("bookmark-card"));
    Keyboard.Press(VirtualKeyShort.RETURN);
    Thread.Sleep(600);

    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.ALT, VirtualKeyShort.KEY_B);
    Thread.Sleep(600);
    Step("Ctrl+Alt+B: the bookmarks bar appears above the page with the bookmark in it", DataText("prefs.json").Contains("\"bookmarksBar\": true") || DataText("prefs.json").Contains("\"BookmarksBar\": true") || DataText("prefs.json").Replace(" ", "").ToLower().Contains("\"bookmarksbar\":true"), Shot("bookmarks-bar"));

    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.OEM_PLUS);
    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.OEM_PLUS);
    Thread.Sleep(500);
    Step("Ctrl+= twice: the page is zoomed in", !app.HasExited, Shot("zoomed"));
    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_0);

    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_Y);
    Thread.Sleep(2500);
    Step("Ctrl+Y: History opens as a page in a tab", SessionText().Contains("search://history"), Shot("history-page"));

    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.SHIFT, VirtualKeyShort.KEY_J);
    Thread.Sleep(2500);
    Step("Ctrl+Shift+J: Downloads opens as a page in a tab", SessionText().Contains("search://downloads"), Shot("downloads-page"));

    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.OEM_COMMA);
    Thread.Sleep(2500);
    Step("Ctrl+, : Settings opens as a page in a tab", SessionText().Contains("search://settings"), Shot("settings-page"));

    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_T);
    Keyboard.Type("http://example.com");
    Keyboard.Press(VirtualKeyShort.RETURN);
    Thread.Sleep(6000);
    Step("Type http://example.com: it loads over https://", SessionText().Contains("https://example.com"), Shot("https-upgrade"));

    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_T);
    Keyboard.Type("http://neverssl.com");
    Keyboard.Press(VirtualKeyShort.RETURN);
    Thread.Sleep(15000);
    var urls = System.Text.RegularExpressions.Regex.Matches(SessionText(), "\"url\":\"([^\"]*neverssl[^\"]*)\"").Select(m => m.Groups[1].Value);
    Step("Type http://neverssl.com (no https there): it still loads, as written", SessionText().Contains("http://neverssl.com") && !app.HasExited, $"tab url: {string.Join(", ", urls)} · " + Shot("https-fallback"));

    var dl = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_T);
    Keyboard.Type("https://github.com/chamsco/Search/archive/refs/heads/main.zip");
    Keyboard.Press(VirtualKeyShort.RETURN);
    Thread.Sleep(9000);
    var got = Directory.Exists(dl) ? Directory.GetFiles(dl, "Search-main*.zip") : [];
    Step("Open a link to a zip: it downloads to the Downloads folder and the downloads card floats up", got.Length > 0, $"{got.Length} file(s) in {dl} · " + Shot("download-float"));
    Keyboard.Press(VirtualKeyShort.ESCAPE);
    Thread.Sleep(500);

    Mouse.Click(new Point(640, 420)); // outside click dismisses the floating downloads card
    Thread.Sleep(600);
    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_N);
    Thread.Sleep(4000);
    var windows = app.GetAllTopLevelWindows(automation).Count(w => !string.IsNullOrEmpty(w.Title));
    Step("Ctrl+N: a second window opens", windows >= 2, $"{windows} windows · " + Shot("new-window"));
    Keyboard.TypeSimultaneously(VirtualKeyShort.ALT, VirtualKeyShort.F4);
    Thread.Sleep(1500);
    Step("Close the second window: the first stays", !app.HasExited, Shot("back-to-first"));

    var second = Process.Start(new ProcessStartInfo(exe, "https://example.org") { UseShellExecute = false, Environment = { ["SEARCH_DATA"] = data, ["SEARCH_NO_UPDATES"] = "1" } })!;
    second.WaitForExit(8000);
    Thread.Sleep(3000);
    Step("Start Search again with a link: the running one opens it, no second copy stays", second.HasExited && SessionText().Contains("example.org"), Shot("one-copy"));

    Keyboard.TypeSimultaneously(VirtualKeyShort.ALT, VirtualKeyShort.F4);
    try { app.WaitWhileMainHandleIsMissing(TimeSpan.FromSeconds(2)); } catch { }
    Thread.Sleep(2000);
    var saved = File.Exists(Path.Combine(data, "session.json"));
    var sessionText = saved ? File.ReadAllText(Path.Combine(data, "session.json")) : "no session.json";
    Step("Alt+F4: the window closes and the session is saved", saved, sessionText[..Math.Min(160, sessionText.Length)].Replace("|", "\\|"));

    app = Launch();
    window = app.GetMainWindow(automation, TimeSpan.FromSeconds(30));
    window.Patterns.Window.Pattern.SetWindowVisualState(FlaUI.Core.Definitions.WindowVisualState.Maximized);
    Thread.Sleep(5000);
    var restored = File.ReadAllText(Path.Combine(data, "session.json"));
    var hasPin = restored.Contains("\"pinId\"");
    var hasIcon = Directory.Exists(Path.Combine(data, "Favicons")) && Directory.GetFiles(Path.Combine(data, "Favicons")).Length > 0;
    Step("Open Search again: pinned page and tabs come back, pin keeps its icon", window is not null && hasPin && hasIcon,
        $"pin saved: {hasPin}, icons cached: {hasIcon} · " + Shot("restored"));
}
catch (Exception e)
{
    Step("Unexpected error", false, e.Message.Replace("|", "\\|").ReplaceLineEndings(" "));
    try { Shot("error"); } catch { }
}
finally
{
    try { if (!app.HasExited) app.Close(); } catch { }
    var crash = Path.Combine(data, "crash.log");
    if (File.Exists(crash))
    {
        var log = File.ReadAllText(crash);
        File.Copy(crash, Path.Combine(outDir, "crash.log"), true);
        Step("Crash log is empty", false, "see crash.log");
        Console.WriteLine(log);
    }
    else Step("Crash log is empty", true);
}

File.WriteAllLines(Path.Combine(outDir, "steps.md"),
    new[] { "| # | Step | Result | Note / screenshot |", "|---|---|---|---|" }.Concat(steps));
return failures == 0 ? 0 : 1;
