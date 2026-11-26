using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
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
    enum UpdateStage { None, Fetching, Ready }
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
        if (stage != UpdateStage.None) { if (byHand) { ShowUpdateState(); report?.Invoke(stage == UpdateStage.Ready ? $"Search {found?.Version} is ready. Restart to update." : "Downloading an update…"); } return; }
        try
        {
            found = await Updates.Check(http, Version.Number);
            if (found is null)
            {
                if (byHand) Say($"Search {Version.Current} is the newest version.", MessageBoxImage.Information);
                return;
            }
            stage = UpdateStage.Fetching;
            var path = Path.Combine(Path.GetTempPath(), $"Search-Setup-{found.Version}.exe");
            using (var response = await http.GetAsync(found.InstallerUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                await using var file = File.Create(path);
                await response.Content.CopyToAsync(file);
            }
            installer = path;
            stage = UpdateStage.Ready;
            ShowUpdateState();
            report?.Invoke($"Search {found.Version} is ready. Restart to update.");
        }
        catch (Exception e)
        {
            stage = UpdateStage.None;
            App.Log(e);
            if (byHand) Say("Search couldn't check for updates. Check your connection and try again.", MessageBoxImage.Warning);
        }
    }

    void ShowUpdateState()
    {
        FootUpdateDoor.Visibility = TopUpdateDoor.Visibility = stage == UpdateStage.Ready ? Visibility.Visible : Visibility.Collapsed;
        if (found is not null) FootUpdateDoor.ToolTip = TopUpdateDoor.ToolTip = $"Restart to update to Search {found.Version}";
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
                if (stage == UpdateStage.Ready) InstallUpdate(); else _ = CheckForUpdates(byHand: true);
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
