using System.Runtime.InteropServices;

namespace Search.App;

static class Native
{
    [DllImport("gdi32.dll")] static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int w, int h);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [StructLayout(LayoutKind.Sequential)] public struct CursorAt { public int X, Y; }
    [DllImport("user32.dll")] public static extern bool GetCursorPos(out CursorAt at);
    [DllImport("user32.dll")] static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool redraw);

    /// <summary>
    /// The page's corners. A web view is a window of its own inside ours, so
    /// WPF can't clip it; a window region rounds it instead (the Mac's card has 10-point corners).
    /// </summary>
    public static void Round(IntPtr hwnd, int width, int height, int radius)
    {
        if (hwnd == IntPtr.Zero || width <= 0 || height <= 0) return;
        var rgn = CreateRoundRectRgn(0, 0, width + 1, height + 1, radius * 2, radius * 2);
        SetWindowRgn(hwnd, rgn, true); // the system owns rgn from here
    }
}
