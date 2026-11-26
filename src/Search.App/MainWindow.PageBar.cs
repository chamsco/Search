using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Search.Core;

namespace Search.App;

// The top of the column and its foot (Zen's sidebar, on Windows): the address you can click to change where you are, the actions for
// the page you are on, a button that hides the column and an edge that brings it back, and Settings and Update at the foot.
public partial class MainWindow
{
    bool peeking;
    DispatcherTimer? leaving;

    /// <summary>The column is hidden or back: the card leaves a strip at the window's edge to reach for it, and the button says which it is.</summary>
    void SideHidden(bool hidden)
    {
        EdgeHot.Visibility = hidden ? Visibility.Visible : Visibility.Collapsed;
        Card.Margin = hidden ? new Thickness(8, 8, 8, 8) : new Thickness(0, 8, 8, 8);
        CollapseDoor.Content = "";
        CollapseDoor.ToolTip = "Hide sidebar (Ctrl+Shift+S)";
    }

    void CollapseDoor_Click(object sender, RoutedEventArgs e)
    {
        if (peeking) { peeking = false; return; } // shown from the edge: the button keeps it
        ToggleSide();
    }

    /// <summary>The pointer at the window's edge while the column is hidden: it comes out, and goes again when the pointer leaves it.</summary>
    void EdgeHot_MouseEnter(object sender, MouseEventArgs e)
    {
        if (Side.Visibility == Visibility.Visible || TopLayout) return;
        ToggleSide();
        peeking = true;
        CollapseDoor.ToolTip = "Keep sidebar open";
        Side.MouseLeave -= Side_PeekLeave;
        Side.MouseLeave += Side_PeekLeave;
    }

    void Side_PeekLeave(object sender, MouseEventArgs e)
    {
        if (!peeking) return;
        leaving ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(450) };
        leaving.Stop();
        leaving.Tick -= LeavingTick;
        leaving.Tick += LeavingTick;
        leaving.Start();
    }

    void LeavingTick(object? sender, EventArgs e)
    {
        leaving?.Stop();
        // Typing an address in the column or dragging a tab keeps it out.
        if (!peeking || Side.IsMouseOver || live?.Editing == true || InlinePopup.IsOpen) return;
        var keep = peeking;
        if (keep) { ToggleSide(); }
    }

    void FootSettings_Click(object sender, RoutedEventArgs e) => OpenPage("search://settings");

    // MARK: - the page's actions (in the tab's menu), and copy link on the row

    void RefreshPageBar() { }

    void CopyRowLink_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if ((sender as FrameworkElement)?.DataContext is not Tab { IsBlank: false, IsInternal: false } t) return;
        try { Clipboard.SetText(t.Url); } catch { return; }
        if (sender is Button b)
        {
            b.Content = "\uE73E"; b.ToolTip = "Link copied";
            var back = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
            back.Tick += (_, _) => { back.Stop(); b.Content = "\uE8C8"; b.ToolTip = "Copy link"; };
            back.Start();
        }
        App.Trace("link copied");
    }

    async void Shot_Click(object sender, RoutedEventArgs e)
    {
        if (live?.View?.CoreWebView2 is not { } core) return;
        try
        {
            using var ms = new MemoryStream();
            await core.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, ms);
            ms.Position = 0;
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Screenshots");
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, $"Search {DateTime.Now:yyyy-MM-dd HH.mm.ss}.png");
            await File.WriteAllBytesAsync(file, ms.ToArray());
            ms.Position = 0;
            var image = new System.Windows.Media.Imaging.BitmapImage();
            image.BeginInit(); image.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad; image.StreamSource = ms; image.EndInit();
            try { Clipboard.SetImage(image); } catch { }
            App.Trace("screenshot " + file);
            Ask("Screenshot saved", "It is on the clipboard and in Pictures › Screenshots.",
            [
                ("Show in folder", () => System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + file + "\"")),
                ("Done", () => { }),
            ], null, true);
        }
        catch (Exception ex) { App.Log(ex); }
    }

    // Windows' own share sheet, for the address of the page.
    [ComImport, Guid("3A3DCD6C-3EAB-43DC-BCDE-45671CE800C8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IDataTransferManagerInterop
    {
        IntPtr GetForWindow(IntPtr appWindow, ref Guid riid);
        void ShowShareUIForWindow(IntPtr appWindow);
    }

    void Share_Click(object sender, RoutedEventArgs e)
    {
        if (live is not { IsBlank: false, IsInternal: false } t) return;
        try
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            var interop = Windows.ApplicationModel.DataTransfer.DataTransferManager.As<IDataTransferManagerInterop>();
            var iid = new Guid("a5caee9b-8708-49d1-8d36-67d25a8da00c");
            var ptr = interop.GetForWindow(hwnd, ref iid);
            var manager = WinRT.MarshalInterface<Windows.ApplicationModel.DataTransfer.DataTransferManager>.FromAbi(ptr);
            var url = t.Url; var title = t.Label;
            manager.DataRequested += (_, a) =>
            {
                a.Request.Data.Properties.Title = string.IsNullOrWhiteSpace(title) ? url : title;
                a.Request.Data.SetWebLink(new Uri(url));
            };
            interop.ShowShareUIForWindow(hwnd);
        }
        catch (Exception ex)
        {
            App.Log(ex);
            try { Clipboard.SetText(t.Url); } catch { } // no share sheet here: the link is on the clipboard instead
        }
    }
}
