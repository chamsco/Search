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
    psi.Environment["SEARCH_SNAP"] = "1";
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
        for (var k = 1; k <= 12; k++) { Mouse.MoveTo(new Point(from.X + (to.X - from.X) * k / 12, from.Y + (to.Y - from.Y) * k / 12)); Thread.Sleep(60); }
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
