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

using var automation = new UIA3Automation();
var app = Launch();
Window? window = null;
try
{
    window = app.GetMainWindow(automation, TimeSpan.FromSeconds(30));
    Step("Launch Search.exe with an empty data folder", window is not null, window?.Title);
    window!.Patterns.Window.Pattern.SetWindowVisualState(FlaUI.Core.Definitions.WindowVisualState.Maximized);
    Thread.Sleep(4000);
    Step("Empty tab shows the centred address field", true, Shot("new-tab"));

    Keyboard.Type("you");
    Thread.Sleep(2000);
    Step("Type \"you\": popular sites suggested, youtube.com completed inline", true, Shot("suggestions-known-sites"));

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

    Keyboard.TypeSimultaneously(VirtualKeyShort.ALT, VirtualKeyShort.F4);
    app.WaitWhileMainHandleIsMissing(TimeSpan.FromSeconds(2));
    Thread.Sleep(2000);
    var saved = File.Exists(Path.Combine(data, "session.json"));
    var sessionText = saved ? File.ReadAllText(Path.Combine(data, "session.json")) : "no session.json";
    Step("Alt+F4: the window closes and the session is saved", saved, sessionText[..Math.Min(160, sessionText.Length)].Replace("|", "\\|"));

    app = Launch();
    window = app.GetMainWindow(automation, TimeSpan.FromSeconds(30));
    window.Patterns.Window.Pattern.SetWindowVisualState(FlaUI.Core.Definitions.WindowVisualState.Maximized);
    Thread.Sleep(5000);
    Step("Open Search again: pinned page and tabs come back", window is not null, Shot("restored"));
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
