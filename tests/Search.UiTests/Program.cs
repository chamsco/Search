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

string? importLocal = null, importRoaming = null;
bool showUpdate = false;
Application Launch()
{
    var psi = new ProcessStartInfo(exe) { UseShellExecute = false };
    psi.Environment["SEARCH_DATA"] = data;
    psi.Environment["SEARCH_NO_UPDATES"] = "1";
    psi.Environment["SEARCH_SNAP"] = "1";
    psi.Environment["SEARCH_TEST_TRUST_LOCALHOST"] = "1";
    if (showUpdate) psi.Environment["SEARCH_TEST_SHOW_UPDATE"] = "1";
    if (importLocal is not null) psi.Environment["SEARCH_IMPORT_LOCALAPPDATA"] = importLocal;
    if (importRoaming is not null) psi.Environment["SEARCH_IMPORT_APPDATA"] = importRoaming;
    return Application.Launch(psi);
}


[System.Runtime.InteropServices.DllImport("user32.dll")] static extern IntPtr SendMessageW(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
string SessionText() { try { return File.ReadAllText(Path.Combine(data, "session.json")); } catch { return ""; } }
string SessionOf(Guid space) { try { return File.ReadAllText(Path.Combine(data, "session-" + space.ToString("N") + ".json")); } catch { return ""; } }
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
    // First row: below the 52-point strip, the address pill (38) and the page's actions (34), 10 in from the column's edge (maximised windows sit 7 in).
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
    Await(() => window!.FindAllDescendants().Any(el => { try { return el.Name == "Example Domain"; } catch { return false; } }), 20000); // the page has to have arrived before it can be searched
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
    Keyboard.Type("http://httpforever.com");
    Keyboard.Press(VirtualKeyShort.RETURN);
    Thread.Sleep(15000);
    var urls = System.Text.RegularExpressions.Regex.Matches(SessionText(), "\"url\":\"([^\"]*httpforever[^\"]*)\"").Select(m => m.Groups[1].Value);
    Step("Type http://httpforever.com (no https there): it still loads, as written", SessionText().Contains("http://httpforever.com") && !app.HasExited, $"tab url: {string.Join(", ", urls)} · " + Shot("https-fallback"));

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

    // ---- wave 3: spaces, profiles, layout ----
    try { app.Close(); } catch { }
    try { app.WaitWhileMainHandleIsMissing(TimeSpan.FromSeconds(1)); } catch { }
    Thread.Sleep(2500);
    File.WriteAllText(Path.Combine(data, "prefs.json"), "{\"spacesOn\":true}");
    var workId = Guid.NewGuid();
    File.WriteAllText(Path.Combine(data, "spaces.json"),
        "[{\"id\":\"00000000-0000-0000-0000-000000000001\",\"name\":\"Personal\"},{\"id\":\"" + workId + "\",\"name\":\"Work\",\"colour\":1,\"icon\":0,\"sharesSignIns\":false}]");
    app = Launch();
    window = app.GetMainWindow(automation, TimeSpan.FromSeconds(30));
    window.Patterns.Window.Pattern.SetWindowVisualState(FlaUI.Core.Definitions.WindowVisualState.Maximized);
    Thread.Sleep(5000);
    Step("Spaces on: a strip of space icons shows above the footer, Personal selected", window.FindFirstDescendant(cf => cf.ByAutomationId("NewSpace")) is not null, Shot("spaces-strip"));

    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.ALT, VirtualKeyShort.KEY_2);
    Thread.Sleep(2500);
    Keyboard.Type("example.org");
    Keyboard.Press(VirtualKeyShort.RETURN);
    Thread.Sleep(6000);
    Step("Ctrl+Alt+2: the Work space opens with its own empty row; a page opened there loads", File.Exists(Path.Combine(data, "session-" + workId.ToString("N") + ".json")) || SessionOf(workId).Contains("example.org"), Shot("space-work"));

    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.ALT, VirtualKeyShort.KEY_1);
    Thread.Sleep(2500);
    var personal = SessionText();
    Step("Ctrl+Alt+1: back to Personal with its own tabs (the search, the pin) while Work's file holds only its own page", personal.Contains("weather") && SessionOf(workId).Contains("example.org") && !SessionOf(workId).Contains("weather"), Shot("space-personal"));

    var plus = window.FindFirstDescendant(cf => cf.ByAutomationId("NewSpace"));
    plus?.AsButton().Invoke();
    Thread.Sleep(1200);
    Keyboard.Type("Travel");
    Keyboard.Press(VirtualKeyShort.RETURN);
    Thread.Sleep(2500);
    Step("New Space popup: name it Travel, Enter: the space is made and opened", DataText("spaces.json").Contains("Travel"), Shot("space-new"));
    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.ALT, VirtualKeyShort.KEY_1);

    // A second profile, seeded the way Settings › Tabs makes it.
    var other = Guid.NewGuid();
    try { app.Close(); } catch { }
    try { app.WaitWhileMainHandleIsMissing(TimeSpan.FromSeconds(1)); } catch { }
    Thread.Sleep(2500);
    File.WriteAllText(Path.Combine(data, "profiles.json"), "[{\"id\":\"00000000-0000-0000-0000-0000000000a1\",\"name\":\"Personal\"},{\"id\":\"" + other + "\",\"name\":\"Studio\"}]");
    var spacesNow = DataText("spaces.json").TrimEnd(']') + ",{\"id\":\"" + Guid.NewGuid() + "\",\"name\":\"Personal\",\"profile\":\"" + other + "\"}]";
    File.WriteAllText(Path.Combine(data, "spaces.json"), spacesNow);
    File.WriteAllText(Path.Combine(data, "prefs.json"), "{\"spacesOn\":true,\"layout\":1,\"pinsAsRows\":true}");
    app = Launch();
    window = app.GetMainWindow(automation, TimeSpan.FromSeconds(30));
    window.Patterns.Window.Pattern.SetWindowVisualState(FlaUI.Core.Definitions.WindowVisualState.Maximized);
    Thread.Sleep(5000);
    Step("Layout Top, pins as rows: tabs run along the top strip with the space icons, the column is gone", window.FindFirstDescendant(cf => cf.ByAutomationId("ProfileDoor")) is not null, Shot("layout-top"));

    window.FindFirstDescendant(cf => cf.ByAutomationId("ProfileDoor"))?.AsButton().Invoke();
    Thread.Sleep(1200);
    Step("The profile door lists Personal and Studio", true, Shot("profile-menu"));
    var studio = automation.GetDesktop().FindFirstDescendant(cf => cf.ByName("Studio"));
    studio?.Click();
    Thread.Sleep(5000);
    var windowsNow = app.GetAllTopLevelWindows(automation).Count(w => !string.IsNullOrEmpty(w.Title));
    Step("Choose Studio: it opens in a window of its own with its own spaces", windowsNow >= 2, $"{windowsNow} windows · " + Shot("profile-window"));

    // ---- wave 4: archive, split view, peek, little windows, snap, windows restored ----
    void Quit()
    {
        try { Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.SHIFT, VirtualKeyShort.KEY_Q); } catch { }
        try { Process.GetProcessById(app.ProcessId).WaitForExit(10000); } catch { }
        try { if (!app.HasExited) app.Kill(); } catch { }
        Thread.Sleep(2000);
    }
    Quit();
    data = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "search-ui-w4-" + Guid.NewGuid().ToString("N")[..8])).FullName;
    File.WriteAllText(Path.Combine(data, "prefs.json"), "{\"closeIdleDays\":1,\"altSplit\":true,\"littleLinks\":true}");
    File.WriteAllText(Path.Combine(data, "session.json"),
        "{\"tabs\":[{\"url\":\"https://example.net/\",\"title\":\"Old page\",\"lastSeen\":\"2020-01-01T00:00:00Z\"},{\"url\":\"https://example.org/\",\"title\":\"Example Domain\"}],\"active\":1,\"splits\":[]}");
    app = Launch();
    window = app.GetMainWindow(automation, TimeSpan.FromSeconds(30));
    window.Patterns.Window.Pattern.SetWindowVisualState(FlaUI.Core.Definitions.WindowVisualState.Maximized);
    Thread.Sleep(7000);
    Step("A tab left alone for years, with Close tabs left alone set to a day: it is closed into the archive, the other stays",
        DataText("archive.json").Contains("example.net") && !SessionText().Contains("example.net") && SessionText().Contains("example.org"),
        "archive.json holds: " + (DataText("archive.json").Contains("example.net") ? "example.net" : "nothing") + " · " + Shot("archived-on-start"));

    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_T);
    Keyboard.Type("search://archive");
    Keyboard.Press(VirtualKeyShort.RETURN);
    Thread.Sleep(3500);
    Step("search://archive: the Archived Tabs page lists it, with Open and Remove", SessionText().Contains("search://archive"), Shot("archive-page"));
    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_W);
    Thread.Sleep(1500);

    int[] SplitCounts()
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(SessionText());
            return doc.RootElement.GetProperty("splits").EnumerateArray().Select(x => x.GetProperty("tabs").GetArrayLength()).ToArray();
        }
        catch { return []; }
    }
    double[] FirstSizes()
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(SessionText());
            return doc.RootElement.GetProperty("splits")[0].GetProperty("sizes").EnumerateArray().Select(x => x.GetDouble()).ToArray();
        }
        catch { return []; }
    }
    Point CentreOf(AutomationElement e) { var r = e.BoundingRectangle; return new Point(r.Left + r.Width / 2, r.Top + r.Height / 2); }
    List<AutomationElement> Rows() => window!.FindAllDescendants(cf => cf.ByName("Example Domain")).Where(e => e.BoundingRectangle.Left < window!.BoundingRectangle.Left + 260).OrderBy(x => x.BoundingRectangle.Top).ThenBy(x => x.BoundingRectangle.Left).ToList();
    // One point a line, in the column: the row's border and its text both answer to the name, so lines are told apart by height.
    List<Point> RowPoints()
    {
        var left = window!.BoundingRectangle.Left;
        var ys = window.FindAllDescendants(cf => cf.ByName("Example Domain")).Where(e => e.BoundingRectangle.Right < left + 300)
            .Select(e => e.BoundingRectangle.Top + e.BoundingRectangle.Height / 2.0).OrderBy(y => y).ToList();
        var lines = new List<double>();
        foreach (var y in ys) if (lines.Count == 0 || y - lines[^1] > 12) lines.Add(y);
        return lines.Select(y => new Point(left + 7 + 110, (int)y)).ToList();
    }

    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.ALT, VirtualKeyShort.KEY_N);
    Thread.Sleep(1500);
    Keyboard.Type("example.com");
    Keyboard.Press(VirtualKeyShort.RETURN);
    Thread.Sleep(6000);
    Step("Ctrl+Alt+N on a lone page: an empty page opens beside it, the address field ready; example.com loads in it", SplitCounts().SequenceEqual([2]), $"splits: [{string.Join(",", SplitCounts())}] · " + Shot("split-two"));

    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.ALT, VirtualKeyShort.LEFT);
    Thread.Sleep(1500);
    Step("Ctrl+Alt+Left: the left page takes the focus ring", !app.HasExited, Shot("split-focus-left"));

    var line = window.FindFirstDescendant(cf => cf.ByAutomationId("SplitLine0"));
    var before = FirstSizes();
    if (line is not null) { var c = CentreOf(line); Mouse.Drag(c, new Point(c.X - 200, c.Y)); }
    Thread.Sleep(3500);
    var after = FirstSizes();
    Step("Drag the line between the pages: the left one gets narrower, the right one wider", line is not null && after.Length == 2 && before.Length == 2 && after[0] < before[0] - 0.05,
        $"before {string.Join("/", before.Select(x => x.ToString("0.00")))}, after {string.Join("/", after.Select(x => x.ToString("0.00")))} · " + Shot("split-resized"));

    for (var n = 0; n < 2; n++)
    {
        Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_T);
        Keyboard.Type("example.org/?p" + n);
        Keyboard.Press(VirtualKeyShort.RETURN);
        Thread.Sleep(4500);
        Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_1);
        Thread.Sleep(1200);
        var row = Rows().LastOrDefault();
        if (row is not null)
        {
            Keyboard.Press(VirtualKeyShort.ALT);
            Mouse.Click(CentreOf(row));
            Keyboard.Release(VirtualKeyShort.ALT);
        }
        Thread.Sleep(5000);
    }
    Step("Alt+click two more tabs: they join the split; four pages share the room", SplitCounts().SequenceEqual([4]), $"splits: [{string.Join(",", SplitCounts())}] · " + Shot("split-four"));

    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.ALT, VirtualKeyShort.KEY_N);
    Thread.Sleep(2000);
    Step("Ctrl+Alt+N inside a split: the pages come apart and stay as tabs", SplitCounts().Length == 0 && !app.HasExited, Shot("split-separated"));

    // Peek: a page with a link, Shift+click on it.
    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_L);
    Keyboard.Type("example.org");
    Keyboard.Press(VirtualKeyShort.RETURN);
    Thread.Sleep(5000);
    // The screen is small and the page tall: zoom out so the link is well above the taskbar (a click down there starts the browser pinned to it).
    for (var z = 0; z < 4; z++) Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.SUBTRACT);
    Thread.Sleep(1500);
    AutomationElement? OnScreenLink()
    {
        var l = window!.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Hyperlink));
        return l is not null && CentreOf(l).Y < 650 && CentreOf(l).Y > 60 ? l : null;
    }
    var link = OnScreenLink();
    // Owned windows hang off their owner in the UI tree, so they are looked for by what is on them.
    bool Shows(string name) { try { var t = Task.Run(() => automation.GetDesktop().FindFirstDescendant(cf => cf.ByName(name)) is not null); return t.Wait(TimeSpan.FromSeconds(15)) && t.Result; } catch { return false; } }
    var windowsBefore = 0;
    if (link is not null)
    {
        Keyboard.Press(VirtualKeyShort.SHIFT);
        Mouse.Click(CentreOf(link));
        Keyboard.Release(VirtualKeyShort.SHIFT);
    }
    Thread.Sleep(6000);
    string EdgeLaunch()
    {
        try
        {
            var ps = Process.Start(new ProcessStartInfo("powershell", "-NoProfile -Command \"Get-CimInstance Win32_Process -Filter \\\"Name='msedge.exe'\\\" | Where-Object { $_.CommandLine -notmatch 'webview2|--type=' } | ForEach-Object { $_.ProcessId.ToString() + ' parent ' + $_.ParentProcessId + ' ' + $_.CommandLine } | Select-Object -First 3\"") { RedirectStandardOutput = true, UseShellExecute = false })!;
            var o = ps.StandardOutput.ReadToEnd(); ps.WaitForExit(15000);
            return o.Replace("|", "/").ReplaceLineEndings(" ");
        }
        catch (Exception e) { return e.Message; }
    }
    var edgeNote = EdgeLaunch();
    Step("Shift+click a link: its page opens over this one in a panel, the page behind dimmed", link is not null && Shows("Open as tab"), $"link found: {link is not null}, panel with Open as tab: {Shows("Open as tab")}, browser started: {edgeNote} · " + Shot("peek"));
    Keyboard.Press(VirtualKeyShort.ESCAPE);
    Thread.Sleep(1500);
    Step("Esc: the peek goes away", !Shows("Open as tab"), Shot("peek-closed"));

    var splitsBefore = SplitCounts().Length;
    link = OnScreenLink();
    if (link is not null)
    {
        Keyboard.Press(VirtualKeyShort.ALT);
        Mouse.Click(CentreOf(link));
        Keyboard.Release(VirtualKeyShort.ALT);
    }
    Thread.Sleep(7000);
    Step("Alt+click a link (Settings › Alt+click opens in Split View): the link loads beside the page", SplitCounts().SequenceEqual([2]), $"splits: [{string.Join(",", SplitCounts())}] · " + Shot("split-link"));

    // A link handed in from another app, with little windows on.
    var little = Process.Start(new ProcessStartInfo(exe, "https://example.com/?little") { UseShellExecute = false, Environment = { ["SEARCH_DATA"] = data, ["SEARCH_NO_UPDATES"] = "1" } })!;
    little.WaitForExit(8000);
    Thread.Sleep(6000);
    Step("A link from another app, with Little windows on: it opens in a small window, not a tab", little.HasExited && Shows("Open in Search") && !SessionText().Contains("?little"), $"small window with Open in Search: {Shows("Open in Search")} · " + Shot("little-window"));
    try { automation.GetDesktop().FindFirstDescendant(cf => cf.ByName("\u2715"))?.AsButton().Invoke(); } catch { }
    Thread.Sleep(1500);
    try { window.Focus(); } catch { }
    Thread.Sleep(500);

    // Snap layouts: the system asks the window what is under the pointer; over the maximise button the answer is HTMAXBUTTON.
    var max = window.FindFirstDescendant(cf => cf.ByAutomationId("MaxDoor"));
    if (max is not null)
    {
        var c = CentreOf(max);
        var answer = SendMessageW(new IntPtr(window.Properties.NativeWindowHandle.Value), 0x0084, IntPtr.Zero, (IntPtr)((c.Y << 16) | (c.X & 0xFFFF))).ToInt64();
        var elsewhere = SendMessageW(new IntPtr(window.Properties.NativeWindowHandle.Value), 0x0084, IntPtr.Zero, (IntPtr)(((c.Y + 400) << 16) | ((c.X - 100) & 0xFFFF))).ToInt64();
        Step("Snap layouts: asked what is under the pointer over the maximise button, the window says HTMAXBUTTON (9)", answer == 9 && elsewhere != 9, $"over the button: {answer}, elsewhere: {elsewhere}");
    }
    else Step("Snap layouts: the maximise button is found", false, "MaxDoor not found");

    // Several windows, quit, open again.
    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_N);
    Thread.Sleep(4000);
    Keyboard.Type("example.com/?second-window");
    Keyboard.Press(VirtualKeyShort.RETURN);
    Thread.Sleep(6000);
    Quit();
    var listed = DataText("windows.json").Contains("\"id\"");
    app = Launch();
    window = app.GetMainWindow(automation, TimeSpan.FromSeconds(30));
    Thread.Sleep(9000);
    var back = app.GetAllTopLevelWindows(automation).Count(w => !string.IsNullOrEmpty(w.Title));
    Step("Quit with two windows (Ctrl+Shift+Q), open Search again: both windows come back, each with its own tabs", listed && back >= 2 && Directory.GetFiles(data, "session-w*.json").Length >= 1, $"windows.json lists one: {listed}, windows open: {back}, window session files: {Directory.GetFiles(data, "session-w*.json").Length} · " + Shot("windows-restored"));


    // ---- wave 5: tab groups, pin folders, motion ----
    Quit();
    data = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "search-ui-w5-" + Guid.NewGuid().ToString("N")[..8])).FullName;
    var work5 = Guid.NewGuid();
    File.WriteAllText(Path.Combine(data, "prefs.json"), "{\"pinsAsRows\":true,\"spacesOn\":true}");
    File.WriteAllText(Path.Combine(data, "spaces.json"),
        "[{\"id\":\"00000000-0000-0000-0000-000000000001\",\"name\":\"Personal\"},{\"id\":\"" + work5 + "\",\"name\":\"Work\",\"colour\":1,\"icon\":0,\"sharesSignIns\":false}]");
    File.WriteAllText(Path.Combine(data, "session.json"),
        "{\"tabs\":[{\"url\":\"https://example.org/\",\"title\":\"Example Domain\"},{\"url\":\"https://example.com/\",\"title\":\"Example Domain\"},{\"url\":\"https://example.net/\",\"title\":\"Example Domain\"}],\"active\":0}");
    app = Launch();
    window = app.GetMainWindow(automation, TimeSpan.FromSeconds(30));
    window.Patterns.Window.Pattern.SetWindowVisualState(FlaUI.Core.Definitions.WindowVisualState.Maximized);
    Thread.Sleep(7000);

    System.Text.Json.JsonElement Session5() { using var d = System.Text.Json.JsonDocument.Parse(SessionText()); return d.RootElement.Clone(); }
    AutomationElement? MenuEntry(string name)
    {
        try
        {
            return automation.GetDesktop().FindAllDescendants(cf => cf.ByName(name)).FirstOrDefault(e => e.ControlType == FlaUI.Core.Definitions.ControlType.MenuItem);
        }
        catch { return null; }
    }
    AutomationElement? Heading(string name) => window!.FindAllDescendants(cf => cf.ByName(name)).FirstOrDefault(e => e.ControlType != FlaUI.Core.Definitions.ControlType.MenuItem);
    void Choose(Point at, params string[] path)
    {
        Mouse.Click(at, MouseButton.Right);
        Thread.Sleep(900);
        foreach (var name in path)
        {
            var item = MenuEntry(name);
            item?.Click();
            Thread.Sleep(900);
        }
    }

    Choose(RowPoints()[1], "Add to Group", "New Group");
    Keyboard.Type("Research");
    Keyboard.Press(VirtualKeyShort.RETURN);
    Thread.Sleep(2500);
    string GroupsNote() { try { return System.Text.RegularExpressions.Regex.Replace(Session5().GetProperty("groups").ToString(), "\\s+", "").Replace("|", "/"); } catch { return "none"; } }
    Step("Tab menu › Add to Group › New Group, type Research: a heading appears above the tab, the group is saved",
        GroupsNote().Contains("Research") && Heading("Research") is not null, $"groups: {GroupsNote()} · " + Shot("group-new"));

    Choose(RowPoints()[2], "Add to Group", "Research");
    Thread.Sleep(2000);
    int Members() { try { var root = Session5(); var gid = root.GetProperty("groups")[0].GetProperty("id").GetString(); return root.GetProperty("tabs").EnumerateArray().Count(t => t.TryGetProperty("group", out var g) && g.GetString() == gid); } catch { return -1; } }
    Step("Add the third tab to Research from its menu: two tabs sit under the heading", Members() == 2, $"members: {Members()} · " + Shot("group-two"));

    var head = Heading("Research");
    if (head is not null) Mouse.Click(CentreOf(head));
    Thread.Sleep(2500);
    var folded = GroupsNote().Contains("\"collapsed\":true");
    var rowsWhileFolded = Rows().Count;
    Step("Click the heading: the group folds to its name and a count", folded, $"collapsed saved: {folded}, tab rows left: {rowsWhileFolded} · " + Shot("group-folded"));
    if (head is not null) Mouse.Click(CentreOf(head));
    Thread.Sleep(2000);

    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_T);
    Keyboard.Type("example.org/?loose");
    Keyboard.Press(VirtualKeyShort.RETURN);
    Thread.Sleep(5000);
    var midShot = "";
    var looseRows = RowPoints();
    head = Heading("Research");
    if (looseRows.Count > 0 && head is not null)
    {
        // A drag the system will take as one: press, move a little at a time, release.
        Keyboard.Release(VirtualKeyShort.ESCAPE); Keyboard.Release(VirtualKeyShort.CONTROL); Keyboard.Release(VirtualKeyShort.SHIFT); Keyboard.Release(VirtualKeyShort.ALT); Thread.Sleep(200);
        var from = looseRows[0]; var to = CentreOf(head); Console.WriteLine($"drag from {from} to {to}");
        Mouse.MoveTo(from); Thread.Sleep(300);
        Mouse.Down(MouseButton.Left); Thread.Sleep(200);
        // Once the drag is on, a 'Drop here to pin' strip slides in above and pushes the column down: aim where the heading is now.
        for (var k = 1; k <= 6; k++) { Mouse.MoveTo(new Point(from.X + k, from.Y + k * 2)); Thread.Sleep(80); }
        Thread.Sleep(700);
        var nowHead = Heading("Research"); if (nowHead is not null) to = CentreOf(nowHead);
        var here = new Point(from.X + 6, from.Y + 12);
        for (var k = 1; k <= 10; k++) { Mouse.MoveTo(new Point(here.X + (to.X - here.X) * k / 10, here.Y + (to.Y - here.Y) * k / 10)); Thread.Sleep(80); }
        for (var k = 0; k < 4; k++) { Mouse.MoveTo(new Point(to.X + k, to.Y + 2 + k)); Thread.Sleep(150); }
        Thread.Sleep(300);
        midShot = Shot("group-dragging");
        Mouse.Up(MouseButton.Left);
    }
    Thread.Sleep(3000);
    string DragTrace() { try { var lines = File.ReadAllLines(Path.Combine(data, "trace.log")).Where(l => (l.Contains("drag") || l.Contains("drop") || l.Contains("row ")) && (!l.Contains("preview")) && !l.Contains("drag over row")).Select(l => l[(l.IndexOf(']') + 2)..]).ToList(); var outp = new List<string>(); foreach (var l in lines) if (outp.Count == 0 || outp[^1] != l) outp.Add(l); return string.Join(" / ", outp.TakeLast(16)); } catch (Exception e) { return "no trace: " + e.Message; } }
    Step("Drag a loose tab onto the Research heading: it joins the group", Members() == 3, $"members: {Members()} · from {looseRows.FirstOrDefault()} to {(head is null ? "none" : CentreOf(head).ToString())} trace: {DragTrace()} · {midShot} · " + Shot("group-drop"));

    // Pin a tab, then folders: pinned rows are on, so the pin is a row.
    Choose(RowPoints()[0], "Pin");
    Thread.Sleep(2000);
    Choose(RowPoints()[0], "Move to Folder", "New Folder");
    Keyboard.Type("Work");
    Keyboard.Press(VirtualKeyShort.RETURN);
    Thread.Sleep(2500);
    string FoldersNote() { try { return System.Text.RegularExpressions.Regex.Replace(Session5().GetProperty("folders").ToString(), "\\s+", "").Replace("|", "/"); } catch { return "none"; } }
    Step("Pin a tab, its menu › Move to Folder › New Folder, type Work: a folder heading holds the pin",
        FoldersNote().Contains("Work") && SessionText().Contains("\"folder\""), $"folders: {FoldersNote()} · " + Shot("folder-new"));

    var wh = Heading("Work");
    if (wh is not null) { Mouse.Click(CentreOf(wh), MouseButton.Right); Thread.Sleep(900); MenuEntry("New Folder Inside")?.Click(); Thread.Sleep(1200); Keyboard.Type("Deep"); Keyboard.Press(VirtualKeyShort.RETURN); }
    Thread.Sleep(2500);
    var nested = false;
    try { var fs = Session5().GetProperty("folders"); nested = fs.GetArrayLength() == 2 && fs.EnumerateArray().Any(f => f.TryGetProperty("parent", out var p) && p.ValueKind == System.Text.Json.JsonValueKind.String); } catch { }
    Step("Right-click the folder › New Folder Inside, type Deep: a folder inside a folder, set further in", nested, $"folders: {FoldersNote()} · " + Shot("folder-nested"));

    Quit();
    app = Launch();
    window = app.GetMainWindow(automation, TimeSpan.FromSeconds(30));
    window.Patterns.Window.Pattern.SetWindowVisualState(FlaUI.Core.Definitions.WindowVisualState.Maximized);
    Thread.Sleep(8000);
    Step("Quit and open again: the group, the folder and the folder inside it are still there", Heading("Research") is not null && Heading("Work") is not null && Heading("Deep") is not null,
        $"headings found: Research {Heading("Research") is not null}, Work {Heading("Work") is not null}, Deep {Heading("Deep") is not null} · " + Shot("groups-restored"));

    // Motion: a still can only catch it part of the way, so each is shot straight away and again once settled.
    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.SHIFT, VirtualKeyShort.KEY_S);
    var midFold = Shot("column-folding");
    Thread.Sleep(1200);
    var folded2 = window.FindFirstDescendant(cf => cf.ByName("New tab")) is null;
    Step("Ctrl+Shift+S: the column folds away (a still shot straight after, then settled)", folded2, $"column gone: {folded2} · {midFold} · " + Shot("column-folded"));
    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.SHIFT, VirtualKeyShort.KEY_S);
    Thread.Sleep(1200);
    Step("Ctrl+Shift+S again: the column comes back", window.FindFirstDescendant(cf => cf.ByName("New tab")) is not null, Shot("column-back"));

    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.ALT, VirtualKeyShort.KEY_2);
    var midSwipe = Shot("space-swiping");
    Thread.Sleep(1500);
    Step("Ctrl+Alt+2: the rows of the next space slide in from the side (a still straight after, then settled)", !app.HasExited, $"{midSwipe} · " + Shot("space-swiped"));
    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.ALT, VirtualKeyShort.KEY_1);
    Thread.Sleep(1500);
    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_2);
    var midSwitch = Shot("tab-switching");
    Thread.Sleep(800);
    Step("Ctrl+2: the page dips as the next tab comes up, its row takes the wash (a still straight after, then settled)", !app.HasExited, $"{midSwitch} · " + Shot("tab-switched"));

    // ---- wave 6: page features and privacy ----
    Quit();
    // A site of our own on localhost (a secure origin for the engine), so permissions, notifications and the rest can be asked for.
    var port = 18930 + Random.Shared.Next(0, 50);
    var server = new System.Net.HttpListener();
    server.Prefixes.Add($"http://localhost:{port}/");
    server.Start();
    const string indexPage = "<!doctype html><title>Wave six</title><body style='font:16px sans-serif'><h1>Wave six</h1>" +
        "<button onclick=\"navigator.geolocation.getCurrentPosition(()=>{document.title='geo ok'},()=>{document.title='geo denied'})\">Ask location</button> " +
        "<button onclick=\"Notification.requestPermission().then(p=>{document.title='note '+p; if(p==='granted') new Notification('Hello from a page',{body:'It works'})})\">Ask notifications</button> " +
        "<button onclick=\"fetch('https://doubleclick.net/x',{mode:'no-cors'}).then(()=>document.title='probe done',()=>document.title='probe failed')\">Probe a tracker</button>";
    const string articlePage = "<!doctype html><title>The Article</title><body><nav><a href='/'>Home</a> <a href='/x'>News</a> <a href='/y'>Sport</a></nav><aside><a href='/z'>Related story one</a> <a href='/w'>Related story two</a></aside>" +
        "<article><h1>A long walk home</h1>" +
        "<p>The evening came down slowly over the harbour, and the lamps along the quay were lit one by one, each with its small halo of mist, so that the whole waterfront seemed to be breathing in the cold.</p>" +
        "<p>She had walked this way every night for eleven years and had never once noticed how the boats leaned toward each other at the turn of the tide, as if they were sharing a piece of news they could not say aloud.</p>" +
        "<p>At the corner where the bakery used to be there was now a shop that sold only umbrellas, and in the window, among the black and the navy, hung a single yellow one, open, as though it had been waiting for her.</p></article>" +
        "<footer><a href='/terms'>Terms</a> <a href='/privacy'>Privacy</a></footer>";
    const string styledPage = "<!doctype html><title>Styled</title><body style='background:white'><h1>Styled</h1><script>setTimeout(()=>{document.title='bg='+getComputedStyle(document.body).backgroundColor},2000)</script>";
    const string videoPage = "<!doctype html><title>Video</title><video id=v muted autoplay playsinline width=320 height=180></video><canvas id=c width=320 height=180 style='display:none'></canvas>" +
        "<script>const c=document.getElementById('c'),x=c.getContext('2d');setInterval(()=>{x.fillStyle='hsl('+(Date.now()/10%360)+',70%,50%)';x.fillRect(0,0,320,180)},50);const v=document.getElementById('v');v.srcObject=c.captureStream(20);v.play();</script>";
    const string loginPage = "<!doctype html><title>Sign in</title><body style='font:16px sans-serif'><form action='/welcome' method='post'><input id=e aria-label='Email address' autocomplete=username> <input id=p type=password aria-label='Password field' autocomplete=current-password> <button type=submit>Sign in</button></form>" +
        "<script>const t=()=>{document.title='f:'+e.value+':'+p.value.length};e.oninput=p.oninput=t</script>";
    const string signupPage = "<!doctype html><title>Sign up</title><body style='font:16px sans-serif'><form action='/welcome' method='post'><input id=e aria-label='New email' autocomplete=username> <input id=p type=password aria-label='Choose a password' autocomplete=new-password> <input id=q type=password aria-label='Repeat password' autocomplete=new-password></form>" +
        "<script>const t=()=>{document.title='n:'+p.value.length+':'+(p.value===q.value)};p.oninput=q.oninput=t</script>";
    const string contactPage = "<!doctype html><title>Contact</title><body style='font:16px sans-serif'><form action='/welcome' method='post'><input id=g aria-label='Given name' autocomplete=given-name> <input id=f aria-label='Family name' autocomplete=family-name> <input id=m aria-label='Contact email' autocomplete=email> <input id=z aria-label='Postal code' autocomplete=postal-code> <input id=o aria-label='Already typed' autocomplete=organization value='Kept'></form>" +
        "<script>const t=()=>{document.title='c:'+g.value+'|'+f.value+'|'+m.value+'|'+z.value+'|'+o.value};for(const i of document.querySelectorAll('input'))i.oninput=t</script>";
    const string feedXml = "<?xml version=\"1.0\"?><rss version=\"2.0\"><channel><title>Harbour News</title>" +
        "<item><title>Post one</title><link>/post1</link></item><item><title>Post two &amp; more</title><link>/post2</link></item></channel></rss>";
    byte[] linkedCrx;
    {
        var ms = new MemoryStream();
        using (var z = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Create, true))
        {
            using (var w = new StreamWriter(z.CreateEntry("manifest.json").Open())) w.Write("{\"manifest_version\":3,\"name\":\"Linked Ext\",\"version\":\"1.0\",\"description\":\"Came from an address\"}");
        }
        var zip = ms.ToArray();
        var crx = new List<byte>("Cr24"u8.ToArray());
        crx.AddRange(BitConverter.GetBytes(3u)); crx.AddRange(BitConverter.GetBytes(4u)); crx.AddRange(new byte[] { 1, 2, 3, 4 }); crx.AddRange(zip);
        linkedCrx = [.. crx];
    }
    var serving = Task.Run(async () =>
    {
        while (server.IsListening)
        {
            try
            {
                var ctx = await server.GetContextAsync();
                if (ctx.Request.HasEntityBody) ctx.Request.InputStream.CopyTo(Stream.Null);
                var path = ctx.Request.Url!.AbsolutePath;
                if (path == "/linked.crx") { ctx.Response.ContentType = "application/x-chrome-extension"; ctx.Response.OutputStream.Write(linkedCrx, 0, linkedCrx.Length); ctx.Response.Close(); continue; }
                var html = path switch { "/article" => articlePage, "/styled" => styledPage, "/video" => videoPage, "/feed.xml" => feedXml, "/blog" => "<!doctype html><title>A blog</title><link rel='alternate' type='application/rss+xml' href='/feed.xml'>", "/login" => loginPage, "/signup" => signupPage, "/contact" => contactPage, "/welcome" => "<!doctype html><title>Welcome</title><h1>Welcome</h1>", _ => indexPage };
                var bytes = System.Text.Encoding.UTF8.GetBytes(html);
                ctx.Response.ContentType = path == "/feed.xml" ? "application/rss+xml; charset=utf-8" : "text/html; charset=utf-8";
                ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
                ctx.Response.Close();
            }
            catch { }
        }
    });
    var origin6 = $"http://localhost:{port}";
    var site6 = $"localhost:{port}";

    Quit();
    data = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "search-ui-w6-" + Guid.NewGuid().ToString("N")[..8])).FullName;
    File.WriteAllText(Path.Combine(data, "prefs.json"), "{\"httpsUpgrade\":false,\"blockTrackers\":true,\"siteStylesOn\":true}");
    File.WriteAllText(Path.Combine(data, "styles.json"), "{\"hosts\":{\"localhost\":\"body{background:rgb(1,2,3)!important}\"}}");
    File.WriteAllText(Path.Combine(data, "session.json"), "{\"tabs\":[{\"url\":\"" + origin6 + "/\",\"title\":\"Wave six\"}],\"active\":0}");
    app = Launch();
    window = app.GetMainWindow(automation, TimeSpan.FromSeconds(30));
    window.Patterns.Window.Pattern.SetWindowVisualState(FlaUI.Core.Definitions.WindowVisualState.Maximized);
    Thread.Sleep(9000);

    Point RowPoints6() => new Point(window!.BoundingRectangle.Left + 117, 72);
    AutomationElement? Anywhere(string name)
    {
        // The app's own top-level windows (popups are separate windows of this process); bounded so a slow tree never stalls the step.
        var task = Task.Run(() =>
        {
            try
            {
                foreach (var top in automation.GetDesktop().FindAllChildren())
                {
                    int pid; try { pid = top.Properties.ProcessId.ValueOrDefault; } catch { continue; }
                    if (pid != app!.ProcessId) continue;
                    var hit = top.FindFirstDescendant(cf => cf.ByName(name));
                    if (hit is not null) return hit;
                }
            }
            catch { }
            return null;
        });
        try { return task.Wait(TimeSpan.FromSeconds(6)) ? task.Result : null; } catch { return null; }
    }
    bool Await(Func<bool> what, int ms = 8000) { var until = DateTime.UtcNow.AddMilliseconds(ms); while (DateTime.UtcNow < until) { if (what()) return true; Thread.Sleep(300); } return what(); }
    AutomationElement? OnPage(string name) { try { return window!.FindFirstDescendant(cf => cf.ByName(name)); } catch { return null; } }
    string Trace6() { try { using var fs = new FileStream(Path.Combine(data, "trace.log"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite); using var r = new StreamReader(fs); return r.ReadToEnd(); } catch { return ""; } }
    void ClickPage(string name) { if (OnPage(name) is { } el) { Mouse.Click(CentreOf(el)); } Thread.Sleep(1200); }
    void ClickCardAway(string name, string title) { for (var i = 0; i < 3; i++) { ClickCard(name, 6000); if (Await(() => Anywhere(title) is null, 2500)) return; } }
    bool ClickCard(string name, int ms = 10000) { AutomationElement? el = null; Await(() => (el = Anywhere(name)) is not null, ms); if (el is not null) Mouse.Click(CentreOf(el)); Thread.Sleep(1200); return el is not null; }

    // Permissions
    bool AskUntil(string button, string title) { for (var i = 0; i < 4; i++) { ClickPage(button); if (Await(() => Anywhere(title) is not null, 7000)) return true; } return false; }
    var asked = AskUntil("Ask location", $"{site6} wants to know your location");
    var askedShot = Shot("permission-card");
    ClickCard("Allow");
    var kept = Await(() => DataText("permissions.json").Contains("\"Location\":\"Allow\"") || DataText("permissions.json").Contains("\"Location\":0"), 5000);
    Step("A page asks for your location: a card over the page asks, Allow is kept for that site", asked && kept, $"card shown: {asked}, kept: {DataText("permissions.json")} · {askedShot}");
    ClickPage("geo denied"); ClickPage("geo ok"); ClickPage("Ask location");
    Thread.Sleep(1500);
    Step("Asked again: no card, the kept answer is used", Anywhere($"{site6} wants to know your location") is null, Shot("permission-remembered"));

    // Notifications
    var askedNote = AskUntil("Ask notifications", $"{site6} wants to show notifications");
    ClickCard("Allow");
    var shown = Await(() => Anywhere($"{site6}: Hello from a page") is not null, 8000);
    Step("A page asks to show notifications, then sends one: the card names the site and the message", askedNote && shown, $"asked: {askedNote}, shown: {shown} · " + Shot("notification"));
    ClickCard("Dismiss");

    // Trackers
    ClickPage("Probe a tracker");
    Thread.Sleep(2500);
    var blocked = Trace6().Contains("tracker blocked https://doubleclick.net");
    Step("Block trackers on: a page asking for a doubleclick.net address is never sent it", blocked, $"trace: {(blocked ? "tracker blocked https://doubleclick.net/x" : "no line")}");

    // Site Information
    Mouse.Click(RowPoints6(), MouseButton.Right);
    Thread.Sleep(900);
    MenuEntry("Site Information")?.Click();
    var info = Await(() => Anywhere("The connection is not secure: what you send can be read on the way.") is not null, 5000);
    Step("Tab menu › Site Information: the site, how it is connected and what it may use", info && Anywhere(site6) is not null, Shot("site-information"));
    ClickCard("Done");

    // Settings › Sites
    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.OEM_COMMA);
    Thread.Sleep(3500);
    var listed6 = false;
    for (var i = 0; i < 4 && !listed6; i++)
    {
        if (i < 2 && OnPage("Sites") is { } nav) Mouse.Click(CentreOf(nav));
        else Mouse.Click(new Point(window!.BoundingRectangle.Left + 293, window.BoundingRectangle.Top + 257)); // the nav entry in the 1024x768 layout
        Thread.Sleep(1200);
        listed6 = Await(() => OnPage("Location: allowed") is not null || OnPage(site6) is not null, 3000);
    }
    Step("Settings › Sites lists what the site was allowed", listed6, Shot("settings-sites"));
    for (var i = 0; i < 4 && DataText("permissions.json").Contains(origin6); i++) // one row per thing the site was allowed
    {
        if (OnPage("Forget") is { } forget) Mouse.Click(CentreOf(forget));
        Thread.Sleep(1500);
    }
    Step("Forget takes the answer back", !DataText("permissions.json").Contains(origin6), $"permissions.json: {DataText("permissions.json")}");
    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_W);
    Thread.Sleep(800);

    // Copy Clean Link
    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_T);
    Keyboard.Type($"{origin6}/?utm_source=news&id=7&fbclid=abc");
    Keyboard.Press(VirtualKeyShort.RETURN);
    Thread.Sleep(4000);
    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.ALT, VirtualKeyShort.SHIFT, VirtualKeyShort.KEY_C);
    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_T);
    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_V);
    Keyboard.Press(VirtualKeyShort.RETURN);
    Thread.Sleep(4500);
    var cleanSaved = Await(() => SessionText().Contains(origin6 + "/?id=7\"") || SessionText().Contains(origin6 + "/?id=7\\u0022"), 6000);
    Step("Ctrl+Alt+Shift+C on a page with utm_ and fbclid in its address, pasted into a new tab: only id=7 is left", cleanSaved, "session: " + System.Text.RegularExpressions.Regex.Match(SessionText(), "[^\"]*\\?id=7[^\"]*").Value + " · " + Shot("clean-link"));

    // Site search with Tab
    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_T);
    Keyboard.Type("youtube");
    Thread.Sleep(600);
    Keyboard.Press(VirtualKeyShort.TAB);
    Thread.Sleep(800);
    var chipShot = Shot("site-search-chip");
    Keyboard.Type("cats");
    Keyboard.Press(VirtualKeyShort.RETURN);
    Thread.Sleep(6000);
    var searched = Await(() => SessionText().Contains("youtube.com/results?search_query=cats"), 8000);
    Step("Type youtube, Tab, cats, Enter: YouTube's own search for cats opens", searched, chipShot + " · " + Shot("site-search-result"));

    // Site style
    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_T);
    Keyboard.Type($"{origin6}/styled");
    Keyboard.Press(VirtualKeyShort.RETURN);
    var styled = Await(() => OnPage("bg=rgb(1, 2, 3)") is not null, 12000);
    Step("A style kept for localhost (Settings › Sites) is on its pages: the background reads rgb(1, 2, 3)", styled, Shot("site-style"));

    // Reading mode
    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_T);
    Keyboard.Type($"{origin6}/article");
    Keyboard.Press(VirtualKeyShort.RETURN);
    Thread.Sleep(5000);
    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.ALT, VirtualKeyShort.SHIFT, VirtualKeyShort.KEY_R);
    Thread.Sleep(1500);
    var readerLine = System.Text.RegularExpressions.Regex.Match(Trace6(), "reader \"opened (\\d+)\"").Groups[1].Value;
    Step("Ctrl+Alt+Shift+R on an article: reading mode opens with its three paragraphs, the links around it left out", readerLine == "3", $"paragraphs: {readerLine} · " + Shot("reading-mode"));
    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.ALT, VirtualKeyShort.SHIFT, VirtualKeyShort.KEY_R);

    // Picture in picture
    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_T);
    Keyboard.Type($"{origin6}/video");
    Keyboard.Press(VirtualKeyShort.RETURN);
    Thread.Sleep(5000);
    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.SHIFT, VirtualKeyShort.KEY_P);
    Thread.Sleep(2500);
    var pipTrace = Trace6().Split('\n').LastOrDefault(l => l.Contains("pip ")) ?? "";
    Step("Ctrl+Shift+P on a page with a playing video: it floats in a picture-in-picture window", pipTrace.Contains("opened"), pipTrace.Trim().Replace("|", "/") + " · " + Shot("picture-in-picture"));
    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.SHIFT, VirtualKeyShort.KEY_P);

    // Live folders
    Quit();
    data = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "search-ui-w6-live-" + Guid.NewGuid().ToString("N")[..8])).FullName;
    File.WriteAllText(Path.Combine(data, "prefs.json"), "{\"httpsUpgrade\":false,\"pinsAsRows\":true}");
    File.WriteAllText(Path.Combine(data, "session.json"), "{\"tabs\":[{\"url\":\"" + origin6 + "/\",\"title\":\"Wave six\"}],\"active\":0,\"folders\":[{\"id\":\"6f1b2c3d-0000-4000-8000-000000000001\",\"name\":\"Seeded live\",\"feed\":\"" + origin6 + "/feed.xml\"}]}");
    app = Launch();
    window = app.GetMainWindow(automation, TimeSpan.FromSeconds(30));
    window.Patterns.Window.Pattern.SetWindowVisualState(FlaUI.Core.Definitions.WindowVisualState.Maximized);
    var postOne = Await(() => OnPage("Post one") is not null || window!.FindFirstDescendant(cf => cf.ByName("Post one")) is not null, 25000);
    Step("A live folder (pin folder with a feed address): its latest posts fill it a few seconds after launch", postOne && window.FindFirstDescendant(cf => cf.ByName("Post two & more")) is not null, "feed: " + origin6 + "/feed.xml · " + Shot("live-folder"));
    if (window.FindFirstDescendant(cf => cf.ByName("Post one")) is { } post) Mouse.Click(CentreOf(post));
    var opened = Await(() => SessionText().Contains("/post1"), 8000);
    Step("Click a post: its page opens in a new tab", opened, "session has /post1: " + opened + " · " + Shot("live-folder-post"));
    if (window.FindFirstDescendant(cf => cf.ByName("Seeded live")) is { } seeded) { Mouse.Click(CentreOf(seeded), MouseButton.Right); Thread.Sleep(900); MenuEntry("New Live Folder Inside…")?.Click(); }
    var addressBox = Await(() => Anywhere("Feed address") is not null, 8000);
    if (addressBox) { Keyboard.Type(origin6 + "/blog"); Thread.Sleep(300); Keyboard.Press(VirtualKeyShort.RETURN); }
    var made = Await(() => window!.FindFirstDescendant(cf => cf.ByName("Harbour News")) is not null, 15000);
    Step("Heading menu › New Live Folder Inside…, a site's address (not the feed's): the feed it points to is found and a folder named after it is made", addressBox && made, $"address box: {addressBox}, folder made: {made} · " + Shot("live-folder-new"));

    // Passwords
    Quit();
    data = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "search-ui-w7-" + Guid.NewGuid().ToString("N")[..8])).FullName;
    File.WriteAllText(Path.Combine(data, "prefs.json"), "{\"httpsUpgrade\":false,\"fillFromCard\":true}");
    File.WriteAllText(Path.Combine(data, "mecard.json"), "{\"given\":\"Ana\",\"family\":\"Diaz\",\"organization\":\"Acme\",\"emails\":[\"ana@example.com\"],\"phones\":[\"+33 6 12 34 56 78\"],\"addresses\":[{\"street\":\"1 Rue Haute\",\"city\":\"Lyon\",\"postal\":\"69001\",\"country\":\"FR\"}]}");
    File.WriteAllText(Path.Combine(data, "session.json"), "{\"tabs\":[{\"url\":\"" + origin6 + "/login\",\"title\":\"\"}],\"active\":0}");
    app = Launch();
    window = app.GetMainWindow(automation, TimeSpan.FromSeconds(30));
    window.Patterns.Window.Pattern.SetWindowVisualState(FlaUI.Core.Definitions.WindowVisualState.Maximized);
    Thread.Sleep(7000);
    string VaultText() => DataText("vault.json");
    string FormTrace() { try { using var fs = new FileStream(Path.Combine(data, "trace.log"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite); using var r = new StreamReader(fs); return string.Join(" / ", r.ReadToEnd().Split('\n').Where(l => l.Contains("form ") || l.Contains("password") || l.Contains("card fill")).TakeLast(8).Select(l => l[(l.IndexOf(']') + 2)..])); } catch { return "no trace"; } }
    bool TypeInto(string field, string text) { for (var i = 0; i < 4; i++) { if (OnPage(field) is { } el) { Mouse.Click(CentreOf(el)); Thread.Sleep(500); Keyboard.Type(text); Thread.Sleep(400); return true; } Thread.Sleep(1000); } return false; }
    var typedUser = TypeInto("Email address", "ana@example.com");
    var typedPw = TypeInto("Password field", "correct horse 42");
    Keyboard.Press(VirtualKeyShort.RETURN);
    if (!Await(() => Anywhere("Save the password for ana@example.com at localhost?") is not null, 5000) && OnPage("Sign in") is { } signInBtn) Mouse.Click(CentreOf(signInBtn)); // the Enter can be lost to the first click
    var offerSave = Await(() => Anywhere("Save the password for ana@example.com at localhost?") is not null, 20000);
    var saveShot = Shot("password-save-card");
    ClickCardAway("Save", "Save the password for ana@example.com at localhost?");
    var vaultHas = Await(() => VaultText().Contains("ana@example.com"), 6000);
    Step("A sign-in sent and settled: a card offers to keep the password; Save puts it in the vault, protected, never in the file as written", typedUser && typedPw && offerSave && vaultHas && !VaultText().Contains("correct horse"), $"card: {offerSave}, kept: {vaultHas}, file shows the password: {VaultText().Contains("correct horse")}, trace: {FormTrace()} · {saveShot}");

    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_T);
    Keyboard.Type(origin6 + "/login");
    Keyboard.Press(VirtualKeyShort.RETURN);
    Thread.Sleep(4500);
    if (OnPage("Email address") is { } emailBox) { Mouse.Click(CentreOf(emailBox)); }
    var offerFill = Await(() => Anywhere("Sign in to localhost") is not null, 15000);
    var fillShot = Shot("password-fill-card");
    ClickCard("Fill");
    var filled = Await(() => OnPage("f:ana@example.com:16") is not null || window!.FindAllDescendants().Any(el => { try { return el.Name == "f:ana@example.com:16"; } catch { return false; } }), 8000);
    Step("Back on the sign-in: the caret in the box offers the kept account; Fill puts the name and password in through the field's own setter", offerFill && filled, $"card: {offerFill}, page title shows both filled: {filled} · {fillShot}");

    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.OEM_COMMA);
    Thread.Sleep(3500);
    var listedPw = false;
    for (var i = 0; i < 4 && !listedPw; i++)
    {
        if (i < 2 && OnPage("Passwords") is { } nav) Mouse.Click(CentreOf(nav));
        else Mouse.Click(new Point(window!.BoundingRectangle.Left + 293, window.BoundingRectangle.Top + 289));
        Thread.Sleep(1200);
        listedPw = Await(() => window!.FindAllDescendants().Any(el => { try { return el.Name?.Contains("ana@example.com") == true; } catch { return false; } }), 3000);
    }
    if (OnPage("Show") is { } showBtn) { Mouse.Click(CentreOf(showBtn)); Thread.Sleep(2500); }
    var revealed = window.FindAllDescendants().Any(el => { try { return el.Name?.Contains("correct horse 42") == true; } catch { return false; } });
    Step("Settings › Passwords: the account is listed; Show (Windows Hello, or straight through where none is set up) reveals the password", listedPw && revealed, $"listed: {listedPw}, shown: {revealed} · " + Shot("settings-passwords"));
    string passkeyLine = "";
    Await(() => { passkeyLine = window!.FindAllDescendants().Select(el => { try { return el.Name ?? ""; } catch { return ""; } }).FirstOrDefault(n => n.StartsWith("Ready: Windows Hello") || n.StartsWith("Windows Hello is not set up") || n.StartsWith("Not available in this web engine") || n.StartsWith("Could not check")) ?? ""; return passkeyLine.Length > 0; }, 5000);
    Step("Settings › Passwords › Passkeys reports whether this PC can hold passkeys (the web engine's WebAuthn answers; the signing itself needs a person at Windows Hello)", passkeyLine.Length > 0 && !passkeyLine.StartsWith("Not available") && !passkeyLine.StartsWith("Could not"), "status: " + passkeyLine.Replace("|", "/"));
    if (window.FindFirstDescendant(cf => cf.ByName("Forget the password for ana@example.com")) is { } forgetBtn) { Mouse.Click(CentreOf(forgetBtn)); Thread.Sleep(1500); }
    Step("Forget removes it from the vault", !VaultText().Contains("ana@example.com"), "vault.json: " + VaultText().Replace("\n", " ").Replace("|", "/")[..Math.Min(120, VaultText().Length)]);
    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_W);

    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_T);
    Keyboard.Type(origin6 + "/signup");
    Keyboard.Press(VirtualKeyShort.RETURN);
    Thread.Sleep(4500);
    var offerStrong = false;
    for (var i = 0; i < 3 && !offerStrong; i++)
    {
        if (OnPage("Choose a password") is { } newBox) Mouse.Click(CentreOf(newBox));
        offerStrong = Await(() => Anywhere("Use a strong password for localhost?") is not null, 6000);
    }
    ClickCardAway("Use", "Use a strong password for localhost?");
    var strong = Await(() => window!.FindAllDescendants().Any(el => { try { return el.Name == "n:20:true"; } catch { return false; } }), 8000);
    Step("A sign-up's new-password box: a strong password is offered and fills both boxes", offerStrong && strong, $"card: {offerStrong}, both boxes hold the same 20 characters: {strong}, trace: {FormTrace()} · " + Shot("password-strong"));

    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_T);
    Keyboard.Type(origin6 + "/contact");
    Keyboard.Press(VirtualKeyShort.RETURN);
    Thread.Sleep(4500);
    var offerCard = false;
    for (var i = 0; i < 3 && !offerCard; i++)
    {
        if (OnPage("Contact email") is { } mailBox) Mouse.Click(CentreOf(mailBox));
        offerCard = Await(() => Anywhere("Fill in from your card at localhost?") is not null, 6000);
    }
    var cardShot = Shot("card-offer");
    ClickCardAway("Fill", "Fill in from your card at localhost?");
    var cardFilled = Await(() => window!.FindAllDescendants().Any(el => { try { return el.Name == "c:Ana|Diaz|ana@example.com|69001|Kept"; } catch { return false; } }), 8000);
    Step("A contact form's email box offers your card (Settings › Passwords › My card); Fill puts name, email and postal code in the empty boxes and leaves the one already typed alone", offerCard && cardFilled, $"card: {offerCard}, boxes: {cardFilled} · {cardShot} · " + Shot("card-filled"));

    // Import from another browser
    Quit();
    var fakeRoot = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "search-ui-import-" + Guid.NewGuid().ToString("N")[..8])).FullName;
    importLocal = Path.Combine(fakeRoot, "Local"); importRoaming = Path.Combine(fakeRoot, "Roaming");
    var chromeUser = Directory.CreateDirectory(Path.Combine(importLocal, "Google", "Chrome", "User Data")).FullName;
    var chromeDefault = Directory.CreateDirectory(Path.Combine(chromeUser, "Default")).FullName;
    var chromeKey = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
    var wrapped = System.Security.Cryptography.ProtectedData.Protect(chromeKey, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
    File.WriteAllText(Path.Combine(chromeUser, "Local State"), "{\"os_crypt\":{\"encrypted_key\":\"" + Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes("DPAPI").Concat(wrapped).ToArray()) + "\"},\"profile\":{\"info_cache\":{\"Default\":{\"name\":\"Ana\"}}}}");
    File.WriteAllText(Path.Combine(chromeDefault, "Bookmarks"), "{\"roots\":{\"bookmark_bar\":{\"type\":\"folder\",\"name\":\"Bookmarks bar\",\"children\":[{\"type\":\"url\",\"name\":\"Fake Chrome Bookmark\",\"url\":\"https://chrome-mark.example/\"},{\"type\":\"folder\",\"name\":\"Reading list\",\"children\":[{\"type\":\"url\",\"name\":\"Deep One\",\"url\":\"https://deep.example/\"}]}]}}}");
    void Lite(string file, params (string Sql, (string, object)[] Args)[] statements)
    {
        using var c = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={file};Pooling=false"); c.Open();
        foreach (var (sql, args) in statements) { using var cmd = c.CreateCommand(); cmd.CommandText = sql; foreach (var (k, v) in args) cmd.Parameters.AddWithValue(k, v); cmd.ExecuteNonQuery(); }
    }
    var chromeNow = (long)((DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds + 11644473600L) * 1000000;
    Lite(Path.Combine(chromeDefault, "History"), ("create table urls (id integer primary key, url text, title text, visit_count integer, last_visit_time integer)", []),
        ("insert into urls (url, title, visit_count, last_visit_time) values ('https://history.example/page', 'A history page', 3, " + chromeNow + ")", []));
    var nonce = System.Security.Cryptography.RandomNumberGenerator.GetBytes(12);
    var plainPw = System.Text.Encoding.UTF8.GetBytes("imported-secret-9");
    var cipherPw = new byte[plainPw.Length]; var tagPw = new byte[16];
    using (var gcm = new System.Security.Cryptography.AesGcm(chromeKey, 16)) gcm.Encrypt(nonce, plainPw, cipherPw, tagPw);
    Lite(Path.Combine(chromeDefault, "Login Data"), ("create table logins (origin_url text, username_value text, password_value blob, blacklisted_by_user integer)", []),
        ("insert into logins values ('https://login.fake.example/', 'chrome-user', $p, 0)", [("$p", System.Text.Encoding.ASCII.GetBytes("v10").Concat(nonce).Concat(cipherPw).Concat(tagPw).ToArray())]));
    var zenProfile = Directory.CreateDirectory(Path.Combine(importRoaming, "zen", "Profiles", "abcd1234.Default (release)")).FullName;
    Lite(Path.Combine(zenProfile, "places.sqlite"), ("create table moz_places (id integer primary key, url text, title text, visit_count integer, last_visit_date integer)", []),
        ("create table moz_bookmarks (id integer primary key, type integer, fk integer, parent integer, position integer, title text)", []),
        ("insert into moz_places values (1, 'https://zen-mark.example/', 'Zen page', 2, 1740830400000000)", []),
        ("insert into moz_bookmarks values (1, 2, null, 0, 0, ''), (2, 2, null, 1, 0, 'toolbar'), (3, 1, 1, 2, 0, 'Fake Zen Bookmark')", []));
    {
        // Zen keeps its spaces and pins in zen-sessions.jsonlz4: a mozLz40 header around one LZ4 block (here all literals).
        var zenJson = System.Text.Encoding.UTF8.GetBytes("{\"spaces\":[{\"uuid\":\"{aaa}\",\"name\":\"Zen Garden\",\"icon\":\"🌱\"}],\"folders\":[{\"id\":\"f1\",\"name\":\"Reading\"}],\"tabs\":[{\"pinned\":true,\"zenWorkspace\":\"{aaa}\",\"groupId\":\"f1\",\"entries\":[{\"url\":\"https://zen-pin.example/read\",\"title\":\"Zen Pinned Page\"}],\"index\":1}]}");
        var block = new List<byte> { 0xF0 };
        var rest = zenJson.Length - 15; while (rest >= 255) { block.Add(255); rest -= 255; }
        block.Add((byte)rest); block.AddRange(zenJson);
        var packed = new List<byte>(System.Text.Encoding.ASCII.GetBytes("mozLz40\0"));
        packed.AddRange(BitConverter.GetBytes(zenJson.Length)); packed.AddRange(block);
        File.WriteAllBytes(Path.Combine(zenProfile, "zen-sessions.jsonlz4"), packed.ToArray());
    }
    data = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "search-ui-w7i-" + Guid.NewGuid().ToString("N")[..8])).FullName;
    File.WriteAllText(Path.Combine(data, "prefs.json"), "{\"httpsUpgrade\":false}");
    app = Launch();
    window = app.GetMainWindow(automation, TimeSpan.FromSeconds(30));
    window.Patterns.Window.Pattern.SetWindowVisualState(FlaUI.Core.Definitions.WindowVisualState.Maximized);
    Thread.Sleep(5000);
    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.OEM_COMMA);
    Thread.Sleep(3500);
    var importGo = false;
    for (var i = 0; i < 4 && !importGo; i++)
    {
        if (i < 2 && OnPage("Import") is { } nav) Mouse.Click(CentreOf(nav));
        else Mouse.Click(new Point(window.BoundingRectangle.Left + 293, window.BoundingRectangle.Top + 321));
        Thread.Sleep(1200);
        importGo = Await(() => OnPage("Import from Chrome · Ana") is not null, 3000);
    }
    var importShot = Shot("import-sources");
    if (OnPage("Import from Chrome · Ana") is { } goChrome) Mouse.Click(CentreOf(goChrome));
    var chromeIn = Await(() => DataText("bookmarks.json").Contains("Fake Chrome Bookmark") && DataText("history.json").Contains("history.example") && DataText("vault.json").Contains("login.fake.example"), 15000);
    Step("Settings › Import lists Chrome; Import brings over its bookmarks (a folder keeps its name), history and saved passwords; the vault holds the password protected, not as written",
        importGo && chromeIn && DataText("bookmarks.json").Contains("Reading list") && !DataText("vault.json").Contains("imported-secret-9"),
        $"sources listed: {importGo}, imported: {chromeIn}, folder kept: {DataText("bookmarks.json").Contains("Reading list")}, file shows the password: {DataText("vault.json").Contains("imported-secret-9")} · {importShot} · " + Shot("import-chrome"));
    if (OnPage("Import from Zen · Default (release)") is { } goZen) Mouse.Click(CentreOf(goZen));
    var zenIn = Await(() => DataText("bookmarks.json").Contains("Fake Zen Bookmark"), 15000);
    Step("The same for Zen (a Firefox-family profile in the roaming folder): its bookmarks come over", zenIn, "bookmark kept: " + zenIn + " · " + Shot("import-zen"));
    var zenSpaces = Await(() => DataText("spaces.json").Contains("Zen Garden"), 8000);
    var zenPins = Directory.EnumerateFiles(data, "session-*.json").Select(File.ReadAllText).Any(t => t.Contains("zen-pin.example/read") && t.Contains("Reading"));
    Step("Zen's sidebar comes too: its space is a space here, and the page pinned inside its folder is pinned in that space, in a folder of the same name", zenSpaces && zenPins, $"space: {zenSpaces}, pin in folder: {zenPins} · " + Shot("import-zen-spaces"));
    importLocal = null; importRoaming = null;

    // Folder sync
    Quit();
    var syncSpace = "";
    var syncShared = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "search-ui-sync-" + Guid.NewGuid().ToString("N")[..8])).FullName;
    {
        var otherPc = "{\"format\":1,\"device\":\"otherpc\",\"name\":\"Other PC\",\"scope\":\"SCOPE\",\"written\":100,\"items\":{ITEMS}}";
        string E(string v) => "{\"v\":" + System.Text.Json.JsonSerializer.Serialize(v) + ",\"t\":100,\"d\":\"otherpc\"}";
        var spaceId = "5a5a5a5a5a5a5a5a5a5a5a5a5a5a5a5a"; var pinId = "6b6b6b6b6b6b6b6b6b6b6b6b6b6b6b6b"; var markId = "7c7c7c7c7c7c7c7c7c7c7c7c7c7c7c7c";
        var syncPersonal = Path.Combine(syncShared, "Search Sync", "profile-000000000000000000000000000000a1");
        Directory.CreateDirectory(syncPersonal); Directory.CreateDirectory(Path.Combine(syncShared, "Search Sync", "app"));
        File.WriteAllText(Path.Combine(syncPersonal, "otherpc.json"), otherPc.Replace("SCOPE", "profile-000000000000000000000000000000a1").Replace("ITEMS", string.Join(",", new[] {
            "\"bm:" + markId + "\":" + E("{\"Title\":\"Synced Mark\",\"Url\":\"https://synced.example/\",\"Folder\":null}"),
            "\"space:" + spaceId + "\":" + E("{\"Name\":\"Synced Space\",\"Colour\":2,\"Colour2\":0,\"Icon\":null,\"Shares\":true,\"Strength\":0}"),
            "\"pin:" + spaceId + ":" + pinId + "\":" + E("{\"Url\":\"https://synced-pin.example/\",\"Title\":\"Synced Pin\",\"Name\":null,\"Folder\":null,\"Muted\":false}"),
            "\"pinorder:" + spaceId + "\":" + E("[\"" + pinId + "\"]") }.Select(x => x))));
        File.WriteAllText(Path.Combine(syncShared, "Search Sync", "app", "otherpc.json"), otherPc.Replace("SCOPE", "app").Replace("ITEMS", "\"set:search.suggest\":" + E("false")));
        syncSpace = spaceId;
    }
    data = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "search-ui-sync-d-" + Guid.NewGuid().ToString("N")[..8])).FullName;
    File.WriteAllText(Path.Combine(data, "prefs.json"), "{\"httpsUpgrade\":false,\"spacesOn\":true}");
    File.WriteAllText(Path.Combine(data, "sync.json"), System.Text.Json.JsonSerializer.Serialize(new { on = true, folder = syncShared }));
    app = Launch();
    window = app.GetMainWindow(automation, TimeSpan.FromSeconds(30));
    window.Patterns.Window.Pattern.SetWindowVisualState(FlaUI.Core.Definitions.WindowVisualState.Maximized);
    Thread.Sleep(6000);
    var syncIn = Await(() => DataText("bookmarks.json").Contains("Synced Mark") && DataText("spaces.json").Contains("Synced Space") && DataText("prefs.json").Contains("\"engineSuggests\":false"), 15000);
    var syncPin = File.Exists(Path.Combine(data, "session-" + syncSpace + ".json")) && DataText("session-" + syncSpace + ".json").Contains("synced-pin.example");
    var syncWrote = Directory.EnumerateFiles(Path.Combine(syncShared, "Search Sync", "app"), "*.json").Count() == 2 && Directory.EnumerateFiles(Path.Combine(syncShared, "Search Sync", "profile-000000000000000000000000000000a1"), "*.json").Count() == 2;
    Step("Sync through a folder: at launch another PC's bookmark, space, pin and setting are here, and this PC's own file is written beside it", syncIn && syncPin && syncWrote, $"bookmark/space/setting: {syncIn}, pin in the space's session: {syncPin}, own files written: {syncWrote} · " + Shot("sync-applied"));
    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.OEM_COMMA);
    Thread.Sleep(3500);
    var syncText = Await(() => window!.FindAllDescendants().Any(el => { try { return el.Name?.Contains("Synced with 1 other PC") == true; } catch { return false; } }), 6000);
    Step("Settings › General says who it synced with", syncText, "status shown: " + syncText + " · " + Shot("sync-settings"));

    // The column: address pill, page actions, hide, edge, foot
    Quit();
    data = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "search-ui-side-" + Guid.NewGuid().ToString("N")[..8])).FullName;
    File.WriteAllText(Path.Combine(data, "prefs.json"), "{\"httpsUpgrade\":false}");
    File.WriteAllText(Path.Combine(data, "session.json"), "{\"tabs\":[{\"url\":\"" + origin6 + "/login\",\"title\":\"\"}],\"active\":0}");
    showUpdate = true;
    app = Launch();
    window = app.GetMainWindow(automation, TimeSpan.FromSeconds(30));
    window.Patterns.Window.Pattern.SetWindowVisualState(FlaUI.Core.Definitions.WindowVisualState.Maximized);
    Thread.Sleep(7000);
    AutomationElement? ById(string id) { try { return window!.FindFirstDescendant(cf => cf.ByAutomationId(id)); } catch { return null; } }
    // The page's actions live in the tab's own menu; the row itself is the address.
    Mouse.MoveTo(RowPoints6()); Thread.Sleep(500);
    var copyOnRow = OnPage("Copy link");
    Step("Hover the tab: a copy-link button sits at the far right of the row, no second address bar above the pages", copyOnRow is not null && ById("AddressPill") is null && ById("AddressText") is null, $"copy button on row: {copyOnRow is not null} · " + Shot("column-address"));
    if (copyOnRow is not null) Mouse.Click(CentreOf(copyOnRow));
    var copied = Await(() => Trace6().Contains("link copied"), 5000);
    Mouse.MoveTo(new Point(window.BoundingRectangle.Left + 700, window.BoundingRectangle.Top + 400)); Thread.Sleep(300);
    // A plain click on the tab you are on edits its address where it is.
    Mouse.Click(new Point(RowPoints6().X + 20, RowPoints6().Y));
    Thread.Sleep(900);
    Keyboard.Type(origin6 + "/signup"); Keyboard.Press(VirtualKeyShort.RETURN);
    var navigated = Await(() => window!.FindAllDescendants().Any(el => { try { return el.Name == "Sign up"; } catch { return false; } }), 12000);
    Step("Clicking the tab you are on changes its address right there, no new tab; Copy link copied it", navigated && copied, $"tab titled Sign up: {navigated}, copied: {copied} · " + Shot("column-address-edit"));
    Mouse.Click(RowPoints6(), MouseButton.Right);
    Thread.Sleep(900);
    var menuHas = new[] { "Bookmark This Page", "Take Screenshot", "Share", "Style This Site" }.All(n => MenuEntry(n) is not null);
    var menuShot = Shot("column-actions");
    MenuEntry("Bookmark This Page")?.Click();
    var starred = Await(() => DataText("bookmarks.json").Contains("localhost"), 8000);
    Keyboard.Press(VirtualKeyShort.ESCAPE); Thread.Sleep(500);
    Step("The tab's right-click menu has Bookmark, Screenshot, Share and Style This Site; Bookmark saves the page", menuHas && starred, $"items: {menuHas}, bookmarked: {starred} · " + menuShot);
    Mouse.Click(RowPoints6(), MouseButton.Right);
    Thread.Sleep(900);
    MenuEntry("Take Screenshot")?.Click();
    var shotCard = Await(() => Anywhere("Screenshot saved") is not null, 10000);
    var shotFile = Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Screenshots")) && Directory.EnumerateFiles(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Screenshots"), "Search *.png").Any(f => (DateTime.Now - File.GetLastWriteTime(f)).TotalMinutes < 5);
    Step("Take Screenshot from the tab menu: the page is saved to Pictures › Screenshots and a card says so", shotCard && shotFile, $"card: {shotCard}, file: {shotFile} · " + Shot("column-screenshot"));
    ClickCardAway("Done", "Screenshot saved");

    // The column never gets narrower than its top row.
    var hideBefore = ById("CollapseSidebar")?.BoundingRectangle.Right ?? 0;
    var edgeAt = new Point(hideBefore + 7, window.BoundingRectangle.Top + 400); // the left half of the edge's grip: the right half is under the page
    Mouse.MoveTo(edgeAt); Thread.Sleep(300);
    Mouse.Down(MouseButton.Left);
    for (var dx = 0; dx <= 150; dx += 10) { Mouse.MoveTo(new Point(edgeAt.X - dx, edgeAt.Y)); Thread.Sleep(30); }
    Mouse.Up(MouseButton.Left);
    Thread.Sleep(1000);
    var reload = ById("ReloadDoor")?.BoundingRectangle; var hideBtn = ById("CollapseSidebar")?.BoundingRectangle;
    var gap = hideBtn is null || reload is null ? -999 : hideBtn.Value.Left - reload.Value.Right;
    Step("Dragging the column's edge as narrow as it goes: the reload button stays whole with a little space before the next one", gap >= 0 && gap < 40 && reload is not null && reload.Value.Width > 20 && (hideBtn?.Right ?? hideBefore) < hideBefore - 2, $"column's right button moved from {hideBefore} to {hideBtn?.Right}, gap after reload: {gap}px · " + Shot("column-narrowest"));

    if (ById("FootSettings") is { } footSet && ById("FootUpdate") is { } footUp)
        Step("The foot has Settings at the right, and Update beside it when one is ready", footUp.BoundingRectangle.Right <= footSet.BoundingRectangle.Left + 4 && Math.Abs(footUp.BoundingRectangle.Top - footSet.BoundingRectangle.Top) < 6, $"update at {footUp.BoundingRectangle.Left}, settings at {footSet.BoundingRectangle.Left} · " + Shot("column-foot"));
    else Step("The foot has Settings at the right, and Update beside it when one is ready", false, "buttons not found");
    var settingsOpened = false;
    for (var i = 0; i < 3 && !settingsOpened; i++)
    {
        // At this screen size the window's foot is behind the taskbar, out of a click's reach: the button is pressed the way assistive tools do.
        if (ById("FootSettings") is { } footSet2) { try { footSet2.Patterns.Invoke.Pattern.Invoke(); } catch { Mouse.Click(CentreOf(footSet2)); } }
        settingsOpened = Await(() => window!.FindAllDescendants().Count(el => { try { return el.Name == "Settings"; } catch { return false; } }) >= 2 || OnPage("Passwords") is not null, 5000);
    }
    Step("Settings at the foot opens Settings", settingsOpened, "Settings page shown: " + settingsOpened);
    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_W);

    // The Library: the column widens, a rail of sections, the section beside it
    var railBefore = ById("ReloadDoor")?.BoundingRectangle.Left ?? 0;
    var doorNote = "";
    for (var i = 0; i < 3 && ById("LibraryTitle") is null; i++)
    {
        var libDoor = ById("LibraryDoor") ?? OnPage("Library");
        if (libDoor is null)
        {
            var seen = window.FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button)).Select(b => { try { return b.AutomationId + "|" + b.Name + "@" + b.BoundingRectangle.Left + "," + b.BoundingRectangle.Top; } catch { return "?"; } }).Where(x => !x.StartsWith("|") || x.Length > 1).Take(40);
            doorNote += "door not found among: " + string.Join(" ", seen) + "; "; Thread.Sleep(1000); continue;
        }
        try { libDoor.Patterns.Invoke.Pattern.Invoke(); doorNote += "invoked at " + libDoor.BoundingRectangle + "; "; }
        catch (Exception ex) { doorNote += "invoke failed: " + ex.Message + "; "; }
        Await(() => ById("LibraryTitle") is not null, 5000);
    }
    doorNote += "trace: " + string.Join(" / ", Trace6().Split('\n').Where(l => l.Contains("library")).TakeLast(3));
    var railCount = new[] { "spaces", "history", "downloads", "bookmarks", "sites", "archive" }.Count(id => ById("Lib-" + id) is not null);
    var cardShown = Await(() => OnPage("Personal") is not null, 8000);
    var wide = (ById("Lib-spaces")?.BoundingRectangle.Right ?? 0) > 0 && (window.FindFirstDescendant(cf => cf.ByName("Spaces").And(cf.ByControlType(FlaUI.Core.Definitions.ControlType.Text))) is not null || cardShown);
    Step("The Library door in the foot widens the column: a rail of six sections down its left, Spaces first with the space as a card", railCount == 6 && cardShown && wide, $"rail buttons: {railCount}, space card: {cardShown}, {doorNote} · " + Shot("library-spaces"));
    if (ById("Lib-history") is { } hBtn) { try { hBtn.Patterns.Invoke.Pattern.Invoke(); } catch { Mouse.Click(CentreOf(hBtn)); } }
    var histShown = Await(() => OnPage("Search history") is not null, 8000);
    Step("History in the rail: the history list shows inside the column, no tab opened", histShown && ById("LibraryTitle")?.Name == "History", "search box shown: " + histShown + " · " + Shot("library-history"));
    if (ById("Lib-downloads") is { } dBtn) { try { dBtn.Patterns.Invoke.Pattern.Invoke(); } catch { Mouse.Click(CentreOf(dBtn)); } }
    var downShown = Await(() => OnPage("Search downloads") is not null, 8000);
    Step("Downloads in the rail: the downloads list shows inside the column", downShown, "search box shown: " + downShown + " · " + Shot("library-downloads"));
    if (ById("Lib-bookmarks") is { } bBtn) { try { bBtn.Patterns.Invoke.Pattern.Invoke(); } catch { Mouse.Click(CentreOf(bBtn)); } }
    var markShown = Await(() => OnPage("Search bookmarks") is not null, 8000);
    Step("Bookmarks in the rail: the saved page is listed inside the column", markShown, "search box shown: " + markShown + " · " + Shot("library-bookmarks"));
    if (ById("LibraryBack") is { } backBtn) { try { backBtn.Patterns.Invoke.Pattern.Invoke(); } catch { Mouse.Click(CentreOf(backBtn)); } }
    var closed = Await(() => ById("LibraryTitle") is null && ById("Lib-history") is null, 5000);
    var reloadAfter = ById("ReloadDoor")?.BoundingRectangle.Left ?? 0;
    Step("The back arrow closes the Library: the column narrows to what it was and the tabs are back", closed && Math.Abs(reloadAfter - railBefore) < 3, $"closed: {closed}, reload at {railBefore} then {reloadAfter} · " + Shot("library-closed"));
    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.SHIFT, VirtualKeyShort.KEY_L);
    var viaKey = Await(() => ById("LibraryTitle") is not null, 5000);
    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.SHIFT, VirtualKeyShort.KEY_L);
    var keyClosed = Await(() => ById("LibraryTitle") is null, 5000);
    Step("Ctrl+Shift+L opens the Library at the section it was left on, and again closes it", viaKey && keyClosed, $"opened: {viaKey}, closed: {keyClosed}");

    // Hide, edge, keep
    if (ById("CollapseSidebar") is { } hide) Mouse.Click(CentreOf(hide));
    var hidden = Await(() => ById("CollapseSidebar") is null, 4000);
    var hiddenShot = Shot("column-hidden");
    Mouse.MoveTo(new Point(window.BoundingRectangle.Left + 700, window.BoundingRectangle.Top + 400)); Thread.Sleep(300);
    Mouse.MoveTo(new Point(window.BoundingRectangle.Left + 3, window.BoundingRectangle.Top + 400));
    var peeked = Await(() => ById("CollapseSidebar") is not null, 4000);
    var peekShot = Shot("column-edge");
    Mouse.MoveTo(new Point(window.BoundingRectangle.Left + 800, window.BoundingRectangle.Top + 400));
    var gone = Await(() => ById("CollapseSidebar") is null, 4000);
    Step("The hide button takes the column away; the pointer at the window's edge brings it back and it leaves again with the pointer", hidden && peeked && gone, $"hidden: {hidden}, shown from the edge: {peeked}, left again: {gone} · {hiddenShot} · {peekShot}");
    Mouse.MoveTo(new Point(window.BoundingRectangle.Left + 3, window.BoundingRectangle.Top + 400));
    Await(() => ById("CollapseSidebar") is not null, 4000);
    if (ById("CollapseSidebar") is { } keep) Mouse.Click(CentreOf(keep));
    Mouse.MoveTo(new Point(window.BoundingRectangle.Left + 800, window.BoundingRectangle.Top + 400)); Thread.Sleep(2000);
    var stayed = ById("CollapseSidebar") is not null;
    Step("Pressing the button while the column is out from the edge keeps it open", stayed, "still there after the pointer left: " + stayed + " · " + Shot("column-kept"));
    showUpdate = false;

    // Extensions
    Quit();
    data = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "search-ui-ext-" + Guid.NewGuid().ToString("N")[..8])).FullName;
    File.WriteAllText(Path.Combine(data, "prefs.json"), "{\"httpsUpgrade\":false}");
    File.WriteAllText(Path.Combine(data, "session.json"), "{\"tabs\":[{\"url\":\"" + origin6 + "/login\",\"title\":\"\"}],\"active\":0}");
    var extDir = Directory.CreateDirectory(Path.Combine(data, "Extensions", "seeded0001")).FullName;
    File.WriteAllText(Path.Combine(extDir, "manifest.json"), "{\"manifest_version\":3,\"name\":\"Test Ext\",\"version\":\"1.0\",\"description\":\"Marks pages\",\"action\":{\"default_popup\":\"popup.html\"},\"content_scripts\":[{\"matches\":[\"http://localhost/*\"],\"js\":[\"c.js\"],\"run_at\":\"document_idle\"}]}");
    File.WriteAllText(Path.Combine(extDir, "c.js"), "document.title = document.title + ' [ext]';");
    File.WriteAllText(Path.Combine(extDir, "popup.html"), "<!doctype html><title>Popup</title><body style='font:16px sans-serif;padding:20px'><h2>Popup of Test Ext</h2>");
    File.WriteAllText(Path.Combine(data, "extensions.json"), "{\"items\":[{\"key\":\"seeded0001\",\"name\":\"Test Ext\",\"version\":\"1.0\",\"description\":\"Marks pages\",\"popup\":\"popup.html\",\"enabled\":true,\"permissions\":[],\"sites\":[\"http://localhost/*\"]}]}");
    app = Launch();
    window = app.GetMainWindow(automation, TimeSpan.FromSeconds(30));
    window.Patterns.Window.Pattern.SetWindowVisualState(FlaUI.Core.Definitions.WindowVisualState.Maximized);
    Thread.Sleep(7000);
    AutomationElement? ExtById(string id) { try { return window!.FindFirstDescendant(cf => cf.ByAutomationId(id)); } catch { return null; } }
    var marked = Await(() => window!.FindAllDescendants().Any(el => { try { return el.Name.Contains("[ext]"); } catch { return false; } }), 25000);
    Step("An extension kept in Search's folder is loaded: its content script has marked the page's title", marked, "tab shows [ext]: " + marked + " · " + Shot("extension-content-script"));
    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.SHIFT, VirtualKeyShort.KEY_E);
    var hubOpen = Await(() => ExtById("LibraryTitle")?.Name == "Extensions", 6000);
    var card = Await(() => OnPage("Test Ext on or off") is not null, 8000);
    Step("Ctrl+Shift+E opens the Library at Extensions: the extension is listed with its switch", hubOpen && card, $"library at Extensions: {hubOpen}, listed: {card} · " + Shot("extension-hub"));
    if (window.FindAllDescendants(cf => cf.ByName("Open")).FirstOrDefault(el => el.ControlType == FlaUI.Core.Definitions.ControlType.Button) is { } openBtn) Mouse.Click(CentreOf(openBtn));
    var popupShown = Await(() => Anywhere("Popup of Test Ext") is not null, 15000);
    Step("Open: the extension's popup appears in a small window", popupShown, "popup text shown: " + popupShown + " · " + Shot("extension-popup"));
    ClickCardAway("Close small window", "Popup of Test Ext");
    Thread.Sleep(500);
    if (OnPage("Chrome Web Store link or id") is { } linkBox) { Mouse.Click(CentreOf(linkBox)); Thread.Sleep(300); Keyboard.Type(origin6 + "/linked.crx"); }
    if (OnPage("Add extension") is { } addBtn) Mouse.Click(CentreOf(addBtn));
    var linked = Await(() => OnPage("Linked Ext on or off") is not null, 15000);
    Step("An address to a .crx in the box, Add: the package is downloaded, unpacked and listed", linked && DataText("extensions.json").Contains("Linked Ext"), $"listed: {linked} · " + Shot("extension-added"));
    // The real Chrome Web Store: uBlock Origin Lite, by its id
    if (OnPage("Chrome Web Store link or id") is { } storeBox) { Mouse.Click(CentreOf(storeBox)); Thread.Sleep(300); Keyboard.Press(VirtualKeyShort.CONTROL); Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A); Keyboard.Release(VirtualKeyShort.CONTROL); Keyboard.Type("https://chromewebstore.google.com/detail/ublock-origin-lite/ddkjiahejlhfcafbddmgiahcphecmpfh"); }
    if (OnPage("Add extension") is { } addStore) Mouse.Click(CentreOf(addStore));
    var storeAdded = Await(() => window!.FindAllDescendants().Any(el => { try { return el.Name.Contains("uBlock") && el.Name.EndsWith("on or off"); } catch { return false; } }), 60000);
    var storeNote = storeAdded ? "listed" : (OnPage("Added") is null ? "not listed; page says: " + string.Join(" | ", window.FindAllDescendants(cf => cf.ByAutomationId("status")).Select(el => el.Name)) : "not listed");
    Step("A Chrome Web Store link in the box, Add: the real store's package is downloaded, unpacked and listed", storeAdded, storeNote + " · " + Shot("extension-store"));
    if (OnPage("Test Ext on or off") is { } sw) Mouse.Click(CentreOf(sw));
    var off = Await(() => DataText("extensions.json").Contains("\"enabled\":false"), 6000);
    Step("The switch turns an extension off", off, "kept as off: " + off + " · " + Shot("extension-off"));
    for (var i = 0; i < 2; i++)
    {
        var sizeBefore = DataText("extensions.json").Length;
        if (window.FindAllDescendants(cf => cf.ByName("Remove")).Where(el => el.ControlType == FlaUI.Core.Definitions.ControlType.Button).LastOrDefault() is { } rm) Mouse.Click(CentreOf(rm));
        Await(() => DataText("extensions.json").Length != sizeBefore, 6000);
    }
    var removed = Await(() => !DataText("extensions.json").Contains("Linked Ext") && !DataText("extensions.json").Contains("uBlock") && !Directory.EnumerateDirectories(Path.Combine(data, "Extensions")).Skip(1).Any(), 8000);
    Step("Remove takes the extension and its folder away", removed, $"gone: {removed} · " + Shot("extension-removed"));

    // Proxy
    Quit();
    var proxyPort = port + 100;
    var proxySaw = new List<string>();
    var proxy = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, proxyPort);
    proxy.Start();
    var proxying = Task.Run(async () =>
    {
        while (true)
        {
            try
            {
                var client = await proxy.AcceptTcpClientAsync();
                _ = Task.Run(() =>
                {
                    using var c = client;
                    var stream = c.GetStream();
                    var buf = new byte[8192];
                    var n = stream.Read(buf, 0, buf.Length);
                    var head = System.Text.Encoding.ASCII.GetString(buf, 0, n);
                    var first = head.Split('\n')[0].Trim();
                    lock (proxySaw) proxySaw.Add(first);
                    if (!first.StartsWith("GET http://", StringComparison.Ordinal)) return;
                    var body = "<!doctype html><title>Via the proxy</title><h1>Via the proxy</h1>";
                    var reply = $"HTTP/1.1 200 OK\r\nContent-Type: text/html\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n{body}";
                    var bytes = System.Text.Encoding.ASCII.GetBytes(reply);
                    stream.Write(bytes, 0, bytes.Length);
                });
            }
            catch { return; }
        }
    });
    data = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "search-ui-w6p-" + Guid.NewGuid().ToString("N")[..8])).FullName;
    File.WriteAllText(Path.Combine(data, "prefs.json"), "{\"httpsUpgrade\":false,\"proxyMode\":2,\"proxyServer\":\"127.0.0.1:" + proxyPort + "\"}");
    File.WriteAllText(Path.Combine(data, "session.json"), "{\"tabs\":[{\"url\":\"http://proxy-probe.test/\",\"title\":\"\"}],\"active\":0}");
    app = Launch();
    window = app.GetMainWindow(automation, TimeSpan.FromSeconds(30));
    window.Patterns.Window.Pattern.SetWindowVisualState(FlaUI.Core.Definitions.WindowVisualState.Maximized);
    var viaProxy = Await(() => OnPage("Via the proxy") is not null, 20000);
    string saw; lock (proxySaw) saw = string.Join(" / ", proxySaw.Where(l => l.Contains("proxy-probe")).Take(2));
    Step("Settings › Privacy › Proxy set to a server of its own: a page for a name that exists nowhere loads through it", viaProxy && saw.Length > 0, $"the proxy saw: {saw} · " + Shot("proxy"));
    proxy.Stop();

    // Proxy that asks who you are
    Quit();
    var authPort = port + 200;
    var authSaw = new List<string>();
    var authProxy = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, authPort);
    authProxy.Start();
    var expected = "Proxy-Authorization: Basic " + Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes("alice:s3cret"));
    var authing = Task.Run(async () =>
    {
        while (true)
        {
            try
            {
                var client = await authProxy.AcceptTcpClientAsync();
                _ = Task.Run(() =>
                {
                    using var c = client;
                    var stream = c.GetStream();
                    var buf = new byte[8192];
                    var n = stream.Read(buf, 0, buf.Length);
                    var head = System.Text.Encoding.ASCII.GetString(buf, 0, n);
                    var first = head.Split('\n')[0].Trim();
                    if (!first.StartsWith("GET http://proxy-auth.test", StringComparison.Ordinal)) return;
                    var signedIn = head.Contains(expected, StringComparison.OrdinalIgnoreCase);
                    lock (authSaw) authSaw.Add(signedIn ? "signed in" : "asked who");
                    string reply;
                    if (signedIn) { var body = "<!doctype html><title>Signed in at the proxy</title><h1>Signed in at the proxy</h1>"; reply = $"HTTP/1.1 200 OK\r\nContent-Type: text/html\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n{body}"; }
                    else reply = "HTTP/1.1 407 Proxy Authentication Required\r\nProxy-Authenticate: Basic realm=\"test\"\r\nContent-Length: 0\r\nConnection: close\r\n\r\n";
                    var bytes = System.Text.Encoding.ASCII.GetBytes(reply);
                    stream.Write(bytes, 0, bytes.Length);
                });
            }
            catch { return; }
        }
    });
    data = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "search-ui-w6a-" + Guid.NewGuid().ToString("N")[..8])).FullName;
    File.WriteAllText(Path.Combine(data, "prefs.json"), "{\"httpsUpgrade\":false,\"proxyMode\":2,\"proxyServer\":\"127.0.0.1:" + authPort + "\"}");
    app = Launch();
    window = app.GetMainWindow(automation, TimeSpan.FromSeconds(30));
    window.Patterns.Window.Pattern.SetWindowVisualState(FlaUI.Core.Definitions.WindowVisualState.Maximized);
    Thread.Sleep(5000);
    Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.OEM_COMMA);
    Thread.Sleep(3500);
    var userBox = false;
    for (var i = 0; i < 4 && !userBox; i++)
    {
        Mouse.Click(new Point(window.BoundingRectangle.Left + 293, window.BoundingRectangle.Top + 225)); // Privacy in the nav
        Thread.Sleep(1200);
        userBox = Await(() => OnPage("Proxy user name") is not null, 3000);
    }
    if (OnPage("Proxy user name") is { } userField) { Mouse.Click(CentreOf(userField)); Thread.Sleep(300); Keyboard.Type("alice"); Keyboard.Press(VirtualKeyShort.TAB); Thread.Sleep(900); }
    if (OnPage("Proxy password") is { } pwField) { Mouse.Click(CentreOf(pwField)); Thread.Sleep(300); Keyboard.Type("s3cret"); Keyboard.Press(VirtualKeyShort.TAB); Thread.Sleep(1500); }
    var vault = Path.Combine(data, "proxy-secret.bin");
    var vaulted = File.Exists(vault) && !System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(vault)).Contains("s3cret") && !DataText("prefs.json").Contains("s3cret");
    Step("Settings › Privacy › Proxy › User name and Password typed: the name is vaulted in prefs, the password protected in its own file and nowhere else", userBox && vaulted && DataText("prefs.json").Contains("alice"), $"settings has the fields: {userBox}, protected file: {File.Exists(vault)} · " + Shot("proxy-credentials"));
    Quit();
    File.WriteAllText(Path.Combine(data, "session.json"), "{\"tabs\":[{\"url\":\"http://proxy-auth.test/\",\"title\":\"\"}],\"active\":0}");
    app = Launch();
    window = app.GetMainWindow(automation, TimeSpan.FromSeconds(30));
    window.Patterns.Window.Pattern.SetWindowVisualState(FlaUI.Core.Definitions.WindowVisualState.Maximized);
    var signed = Await(() => OnPage("Signed in at the proxy") is not null, 25000);
    string sawAuth; lock (authSaw) sawAuth = string.Join(" / ", authSaw);
    Step("A proxy that answers 407: Search signs in with the kept name and password, and the page loads", signed, $"the proxy saw: {sawAuth} · " + Shot("proxy-signed-in"));
    authProxy.Stop();

    // A site that asks for a certificate (mutual TLS: openssl's own test server, a client certificate in this account's store)
    Quit();
    var certDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "search-ui-certs-" + Guid.NewGuid().ToString("N")[..8])).FullName;
    using var caKey = System.Security.Cryptography.RSA.Create(2048);
    var caReq = new System.Security.Cryptography.X509Certificates.CertificateRequest("CN=Search Test CA", caKey, System.Security.Cryptography.HashAlgorithmName.SHA256, System.Security.Cryptography.RSASignaturePadding.Pkcs1);
    caReq.CertificateExtensions.Add(new System.Security.Cryptography.X509Certificates.X509BasicConstraintsExtension(true, false, 0, true));
    using var ca = caReq.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(2));
    using var srvKey = System.Security.Cryptography.RSA.Create(2048);
    var srvReq = new System.Security.Cryptography.X509Certificates.CertificateRequest("CN=localhost", srvKey, System.Security.Cryptography.HashAlgorithmName.SHA256, System.Security.Cryptography.RSASignaturePadding.Pkcs1);
    var san = new System.Security.Cryptography.X509Certificates.SubjectAlternativeNameBuilder(); san.AddDnsName("localhost");
    srvReq.CertificateExtensions.Add(san.Build());
    using var srvCert = srvReq.Create(ca, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1), Guid.NewGuid().ToByteArray());
    using var cliKey = System.Security.Cryptography.RSA.Create(2048);
    var cliReq = new System.Security.Cryptography.X509Certificates.CertificateRequest("CN=Test Client", cliKey, System.Security.Cryptography.HashAlgorithmName.SHA256, System.Security.Cryptography.RSASignaturePadding.Pkcs1);
    cliReq.CertificateExtensions.Add(new System.Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension(new System.Security.Cryptography.OidCollection { new System.Security.Cryptography.Oid("1.3.6.1.5.5.7.3.2") }, false));
    using var cliCert = cliReq.Create(ca, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1), Guid.NewGuid().ToByteArray());
    File.WriteAllText(Path.Combine(certDir, "ca.pem"), ca.ExportCertificatePem());
    File.WriteAllText(Path.Combine(certDir, "srv.pem"), srvCert.ExportCertificatePem());
    File.WriteAllText(Path.Combine(certDir, "srv.key"), srvKey.ExportPkcs8PrivateKeyPem());
    var cliPfx = new System.Security.Cryptography.X509Certificates.X509Certificate2(System.Security.Cryptography.X509Certificates.RSACertificateExtensions.CopyWithPrivateKey(cliCert, cliKey).Export(System.Security.Cryptography.X509Certificates.X509ContentType.Pfx, "x"), "x",
        System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.PersistKeySet | System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.UserKeySet);
    using (var my = new System.Security.Cryptography.X509Certificates.X509Store(System.Security.Cryptography.X509Certificates.StoreName.My, System.Security.Cryptography.X509Certificates.StoreLocation.CurrentUser)) { my.Open(System.Security.Cryptography.X509Certificates.OpenFlags.ReadWrite); my.Add(cliPfx); }
    var tlsPort = port + 300;
    var openssl = File.Exists(@"C:\Program Files\Git\usr\bin\openssl.exe") ? @"C:\Program Files\Git\usr\bin\openssl.exe" : "openssl";
    var tlsOut = new System.Text.StringBuilder();
    Process? tls = null;
    try
    {
        tls = Process.Start(new ProcessStartInfo(openssl, $"s_server -accept {tlsPort} -tls1_2 -cert srv.pem -key srv.key -Verify 1 -CAfile ca.pem -www") { WorkingDirectory = certDir, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true });
        tls!.OutputDataReceived += (_, e) => { lock (tlsOut) tlsOut.AppendLine(e.Data); };
        tls.ErrorDataReceived += (_, e) => { lock (tlsOut) tlsOut.AppendLine(e.Data); };
        tls.BeginOutputReadLine(); tls.BeginErrorReadLine();
    }
    catch (Exception e) { tlsOut.AppendLine("could not start openssl: " + e.Message); }
    Thread.Sleep(1500);
    data = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "search-ui-w6c-" + Guid.NewGuid().ToString("N")[..8])).FullName;
    File.WriteAllText(Path.Combine(data, "prefs.json"), "{\"httpsUpgrade\":false}");
    File.WriteAllText(Path.Combine(data, "session.json"), "{\"tabs\":[{\"url\":\"https://localhost:" + tlsPort + "/\",\"title\":\"\"}],\"active\":0}");
    app = Launch();
    window = app.GetMainWindow(automation, TimeSpan.FromSeconds(30));
    window.Patterns.Window.Pattern.SetWindowVisualState(FlaUI.Core.Definitions.WindowVisualState.Maximized);
    var certAsked = Await(() => Anywhere($"localhost:{tlsPort} asks who you are") is not null, 25000);
    var certShot = Shot("client-certificate-card");
    ClickCard("Continue");
    Thread.Sleep(5000);
    string said; lock (tlsOut) said = tlsOut.ToString();
    var sent = said.Contains("Test Client");
    var shownOnPage = window.FindAllDescendants().Any(el => { try { return el.Name?.Contains("Test Client") == true; } catch { return false; } });
    Step("A site that asks for a certificate: a card lists yours, Continue sends it, and the site sees it", certAsked && (sent || shownOnPage), $"card: {certAsked}, server saw the certificate: {sent}, page shows it: {shownOnPage} · {certShot} · " + Shot("client-certificate-sent"));
    try { tls?.Kill(true); } catch { }
    try { using var my = new System.Security.Cryptography.X509Certificates.X509Store(System.Security.Cryptography.X509Certificates.StoreName.My, System.Security.Cryptography.X509Certificates.StoreLocation.CurrentUser); my.Open(System.Security.Cryptography.X509Certificates.OpenFlags.ReadWrite); my.Remove(cliPfx); } catch { }
    server.Stop();
}
catch (Exception e)
{
    Step("Unexpected error", false, e.Message.Replace("|", "\\|").ReplaceLineEndings(" "));
    try { Shot("error"); } catch { }
}
finally
{
    try { if (!app.HasExited) app.Close(); } catch { }
    try { var tr = Path.Combine(data, "trace.log"); if (File.Exists(tr)) File.Copy(tr, Path.Combine(outDir, "trace.log"), true); } catch { }
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
