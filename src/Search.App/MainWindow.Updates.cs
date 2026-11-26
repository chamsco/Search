using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Ellipse = System.Windows.Shapes.Ellipse;
using Shape = System.Windows.Shapes.Shape;
using System.Windows.Threading;
using Search.Core;

namespace Search.App;

/// <summary>
/// Updates (Updater.swift): checked at launch and every hour, fetched quietly,
/// and offered as "Restart to Update" at the foot of the column. Nothing
/// restarts on its own; a page you are reading is never interrupted.
/// Also "Check for Updates…" and "About Search" in the window's system menu
/// (Alt+Space, or right-click the strip), where Windows keeps an app's own menu.
/// </summary>
public partial class MainWindow
{
    enum UpdateStage { None, Available, Fetching, Ready }
    UpdateStage stage;
    Release? found;
    string? installer;
    DispatcherTimer? hourly;

    void StartUpdates()
    {
        if (Environment.GetEnvironmentVariable("SEARCH_NO_UPDATES") == "1") return;
        _ = CheckForUpdates(byHand: false);
        hourly = new DispatcherTimer { Interval = TimeSpan.FromHours(1) };
        hourly.Tick += (_, _) => _ = CheckForUpdates(byHand: false);
        hourly.Start();
    }

    async Task CheckForUpdates(bool byHand, Action<string>? report = null)
    {
        void Say(string text, MessageBoxImage icon) { if (report is not null) report(text); else MessageBox.Show(this, text, "Search", MessageBoxButton.OK, icon); }
        if (stage != UpdateStage.None) { if (byHand) { ShowUpdateState(); report?.Invoke(stage switch { UpdateStage.Ready => $"Search {found?.Version} is ready. Restart to update.", UpdateStage.Fetching => "Downloading an update…", _ => $"Search {found?.Version} is available. Click the update button." }); } return; }
        try
        {
            found = await Updates.Check(http, Version.Number);
            if (found is null)
            {
                if (byHand) Say($"Search {Version.Current} is the newest version.", MessageBoxImage.Information);
                return;
            }
            stage = UpdateStage.Available;
            ShowUpdateState();
            report?.Invoke($"Search {found.Version} is available. Click the update button at the foot of the window.");
        }
        catch (Exception e)
        {
            stage = UpdateStage.None;
            ShowUpdateState();
            App.Log(e);
            if (byHand) Say("Search couldn't check for updates. Check your connection and try again.", MessageBoxImage.Warning);
        }
    }

    enum UpdateLook { Available, Fetching, Ready, Restarting }

    void ShowUpdateState() => PaintUpdate(stage switch { UpdateStage.Ready => UpdateLook.Ready, UpdateStage.Fetching => UpdateLook.Fetching, _ => UpdateLook.Available }, stage != UpdateStage.None);

    /// <summary>
    /// The update button is a disc in the page's ink, so it reads on a light or a dark column. Available: an outlined disc with
    /// an arrow down and a ring pinging outwards. Downloading: the outline breathes. Ready: a solid disc with a ring. Restarting: the glyph turns.
    /// </summary>
    void PaintUpdate(UpdateLook look, bool shown = true)
    {
        foreach (var b in new[] { FootUpdateDoor, TopUpdateDoor })
        {
            b.Visibility = shown ? Visibility.Visible : Visibility.Collapsed;
            b.IsEnabled = look != UpdateLook.Restarting;
            b.Content = UpdateFace(look);
        }
    }

    UIElement UpdateFace(UpdateLook look)
    {
        var grid = new Grid { Width = 22, Height = 22, RenderTransformOrigin = new Point(0.5, 0.5) };
        var disc = new Ellipse { Width = 20, Height = 20 };
        var glyph = new TextBlock { FontFamily = (FontFamily)FindResource("Glyphs"), FontSize = 10, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, RenderTransformOrigin = new Point(0.5, 0.5) };
        if (look is UpdateLook.Fetching or UpdateLook.Available)
        {
            disc.Fill = Brushes.Transparent; disc.StrokeThickness = 1.5; disc.SetResourceReference(Shape.StrokeProperty, "Ink");
            glyph.Text = "\uE896"; glyph.SetResourceReference(TextBlock.ForegroundProperty, "Ink");
            if (Moves && look == UpdateLook.Fetching) grid.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0.4, TimeSpan.FromMilliseconds(800)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever });
        }
        else
        {
            disc.SetResourceReference(Shape.FillProperty, "Ink");
            glyph.Text = "\uE895"; glyph.SetResourceReference(TextBlock.ForegroundProperty, "Ground");
        }
        grid.Children.Add(disc);
        if (look is UpdateLook.Ready or UpdateLook.Available && Moves)
        {
            var ring = new Ellipse { Width = 20, Height = 20, StrokeThickness = 1.5, Fill = Brushes.Transparent, IsHitTestVisible = false, RenderTransformOrigin = new Point(0.5, 0.5), RenderTransform = new ScaleTransform() };
            ring.SetResourceReference(Shape.StrokeProperty, "Ink");
            var cycle = new Duration(TimeSpan.FromMilliseconds(1800));
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            ((ScaleTransform)ring.RenderTransform).BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1, 1.5, cycle) { RepeatBehavior = RepeatBehavior.Forever, EasingFunction = ease });
            ((ScaleTransform)ring.RenderTransform).BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(1, 1.5, cycle) { RepeatBehavior = RepeatBehavior.Forever, EasingFunction = ease });
            ring.BeginAnimation(OpacityProperty, new DoubleAnimation(0.6, 0, cycle) { RepeatBehavior = RepeatBehavior.Forever });
            grid.Children.Add(ring);
        }
        grid.Children.Add(glyph);
        if (look == UpdateLook.Restarting)
        {
            glyph.Text = "\uE72C";
            glyph.RenderTransform = new RotateTransform();
            ((RotateTransform)glyph.RenderTransform).BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(0, 360, TimeSpan.FromMilliseconds(700)) { RepeatBehavior = RepeatBehavior.Forever });
        }
        return grid;
    }

    int updatePercent;
    bool updateFailed;
    DispatcherTimer? updateHide, updateRestart;

    /// <summary>First click downloads, with the bar filling; once it is ready a second click restarts into the new version.</summary>
    void TopUpdate_Click(object sender, RoutedEventArgs e)
    {
        switch (stage)
        {
            case UpdateStage.Available: _ = DownloadUpdate(); break;
            case UpdateStage.Ready:
                PaintUpdate(UpdateLook.Restarting);
                UpdateHint.Text = "Restarting…";
                updateRestart = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(Moves ? 700 : 50) };
                updateRestart.Tick += (_, _) => { updateRestart.Stop(); InstallUpdate(); };
                updateRestart.Start();
                break;
        }
    }

    async Task DownloadUpdate()
    {
        if (found is null || stage != UpdateStage.Available) return;
        stage = UpdateStage.Fetching; updatePercent = 0; updateFailed = false;
        ShowUpdateState(); RefreshUpdatePopup();
        try
        {
            var path = Path.Combine(Path.GetTempPath(), $"Search-Setup-{found.Version}.exe");
            using var response = await http.GetAsync(found.InstallerUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            var total = response.Content.Headers.ContentLength ?? -1;
            await using var from = await response.Content.ReadAsStreamAsync();
            await using (var file = File.Create(path))
            {
                var buffer = new byte[81920]; long done = 0; int n;
                while ((n = await from.ReadAsync(buffer)) > 0)
                {
                    await file.WriteAsync(buffer.AsMemory(0, n));
                    done += n;
                    if (total > 0 && (int)(done * 100 / total) is var pct && pct != updatePercent) { updatePercent = pct; RefreshUpdatePopup(); }
                }
            }
            installer = path; updatePercent = 100;
            stage = UpdateStage.Ready;
        }
        catch (Exception e)
        {
            App.Log(e);
            stage = UpdateStage.Available; updateFailed = true;
        }
        ShowUpdateState(); RefreshUpdatePopup();
    }

    // MARK: - the popup by the button

    void Update_Enter(object sender, MouseEventArgs e)
    {
        updateHide?.Stop();
        if (stage == UpdateStage.None || found is null) return;
        if (sender is Button b) { UpdatePopup.PlacementTarget = b; UpdatePopup.Placement = ReferenceEquals(b, TopUpdateDoor) ? System.Windows.Controls.Primitives.PlacementMode.Bottom : System.Windows.Controls.Primitives.PlacementMode.Top; }
        RefreshUpdatePopup();
        UpdatePopup.IsOpen = true;
    }

    void Update_Leave(object sender, MouseEventArgs e)
    {
        updateHide?.Stop();
        updateHide = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        updateHide.Tick += (_, _) => { updateHide!.Stop(); if (!FootUpdateDoor.IsMouseOver && !TopUpdateDoor.IsMouseOver && !(UpdatePopup.Child?.IsMouseOver ?? false)) UpdatePopup.IsOpen = false; };
        updateHide.Start();
    }

    void RefreshUpdatePopup()
    {
        if (found is null) return;
        UpdateTitle.Text = $"Search {found.Version}";
        UpdateFrom.Text = $"You have {Version.Current}";
        UpdateNotes.Children.Clear();
        var lines = Release.Lines(found.Notes);
        if (lines.Length == 0) lines = ["No notes came with this build."];
        foreach (var line in lines)
        {
            var t = new TextBlock { Text = line, FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 3) };
            t.SetResourceReference(TextBlock.ForegroundProperty, "Ink");
            UpdateNotes.Children.Add(t);
        }
        var bar = stage is UpdateStage.Fetching or UpdateStage.Ready;
        UpdateTrack.Visibility = bar ? Visibility.Visible : Visibility.Collapsed;
        UpdateFill.Width = 236 * Math.Clamp(updatePercent, 0, 100) / 100.0;
        UpdateHint.Text = stage switch
        {
            UpdateStage.Fetching => "Downloading…",
            UpdateStage.Ready => "Ready. Click to restart and update.",
            _ => updateFailed ? "The download did not finish. Click to try again." : "Click to download",
        };
    }

    /// <summary>The installer runs silently, closes this window, and opens Search again when it is done.</summary>
    void InstallUpdate()
    {
        if (installer is null || !File.Exists(installer)) return;
        SaveNow();
        history.Save();
        Process.Start(new ProcessStartInfo(installer, "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS /RELAUNCH=1") { UseShellExecute = true });
        App.Quitting = true;
        Application.Current.Shutdown();
    }

    // MARK: - the system menu

    const int CheckUpdatesId = 0x1F10, AboutId = 0x1F20;
    [DllImport("user32.dll")] static extern IntPtr GetSystemMenu(IntPtr hWnd, bool revert);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern bool AppendMenu(IntPtr menu, uint flags, uint id, string? item);

    void AddSystemMenuItems()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var menu = GetSystemMenu(hwnd, false);
        AppendMenu(menu, 0x800, 0, null); // separator
        AppendMenu(menu, 0, CheckUpdatesId, "Check for Updates…");
        AppendMenu(menu, 0, AboutId, "About Search");
        HwndSource.FromHwnd(hwnd)?.AddHook(SystemMenuHook);
        HwndSource.FromHwnd(hwnd)?.AddHook(SnapHook);
    }

    IntPtr SystemMenuHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_SYSCOMMAND = 0x0112;
        if (msg != WM_SYSCOMMAND) return IntPtr.Zero;
        switch ((int)wParam & 0xFFF0)
        {
            case CheckUpdatesId:
                handled = true;
                if (stage == UpdateStage.Ready) InstallUpdate(); else if (stage == UpdateStage.Available) _ = DownloadUpdate(); else _ = CheckForUpdates(byHand: true);
                break;
            case AboutId:
                handled = true;
                MessageBox.Show(this,
                    $"Search for Windows {Version.Current}\n\nA port of Search by Office Commun (MIT).\nEngine: Microsoft Edge WebView2 {Microsoft.Web.WebView2.Core.CoreWebView2Environment.GetAvailableBrowserVersionString()}",
                    "About Search", MessageBoxButton.OK, MessageBoxImage.Information);
                break;
        }
        return IntPtr.Zero;
    }
}
