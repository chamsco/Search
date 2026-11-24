using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace Search.App;

/// <summary>
/// Snap layouts: Windows 11 shows its flyout of layouts over a window's maximise button, which it finds by asking
/// the window what is under the pointer. Ours is drawn by the app, so it answers "the maximise button" for the
/// two places it draws one, and takes the click that follows (the system sends it as a non-client click).
/// Windows 10 has no flyout, and keeps its own hover highlight on the button, so it is left alone there.
/// </summary>
public partial class MainWindow
{
    const int WM_NCHITTEST = 0x0084, WM_NCLBUTTONDOWN = 0x00A1, WM_NCLBUTTONUP = 0x00A2, HTMAXBUTTON = 9;

    static readonly bool SnapOn = Environment.OSVersion.Version.Build >= 22000 || Environment.GetEnvironmentVariable("SEARCH_SNAP") == "1";

    IntPtr SnapHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (!SnapOn || msg is not (WM_NCHITTEST or WM_NCLBUTTONDOWN or WM_NCLBUTTONUP)) return IntPtr.Zero;
        if (msg == WM_NCHITTEST)
        {
            var l = lParam.ToInt64();
            var screen = new Point((short)(l & 0xFFFF), (short)((l >> 16) & 0xFFFF));
            if (OverMax(MaxDoor, screen) || OverMax(TopMaxDoor, screen)) { handled = true; return (IntPtr)HTMAXBUTTON; }
            return IntPtr.Zero;
        }
        if ((int)wParam != HTMAXBUTTON) return IntPtr.Zero;
        handled = true;
        if (msg == WM_NCLBUTTONUP) Maximize_Click(this, new RoutedEventArgs());
        return IntPtr.Zero;
    }

    bool OverMax(Button door, Point screen)
    {
        if (!door.IsVisible || door.ActualWidth <= 0) return false;
        try
        {
            var p = door.PointFromScreen(screen);
            return p.X >= 0 && p.Y >= 0 && p.X <= door.ActualWidth && p.Y <= door.ActualHeight;
        }
        catch (InvalidOperationException) { return false; }
    }
}
