using System.IO;
using System.Windows;
using Search.Core;

namespace Search.App;

/// <summary>
/// Windows (Windows.swift): every window's tabs are kept, and a restart brings them all back where they were.
/// The first window keeps the files there have always been; the others each have files of their own. A window you close
/// while others stay open is one you meant to close and is forgotten; closing the last one, or quitting, keeps them all.
/// </summary>
public partial class MainWindow
{
    static readonly List<WindowEntry> book = WindowList.Load();

    /// <summary>Windows saved from last time, other than the first, to be opened again.</summary>
    public static IReadOnlyList<WindowEntry> Remembered => book.Where(e => e.Id != Guid.Empty).ToList();

    void RegisterWindow()
    {
        if (windowId == Guid.Empty || book.Any(e => e.Id == windowId)) return;
        book.Add(new WindowEntry { Id = windowId, Profile = profileId });
        WindowList.Save(book);
    }

    void RestorePlacement()
    {
        if (book.FirstOrDefault(e => e.Id == windowId)?.Placement is not { Width: > 200, Height: > 150 } p) return;
        var screen = SystemParameters.VirtualScreenLeft;
        if (p.Left + p.Width < SystemParameters.VirtualScreenLeft + 80 || p.Left > SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 80) return;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = p.Left; Top = p.Top; Width = p.Width; Height = p.Height;
        if (p.Maximized) WindowState = WindowState.Maximized;
    }

    void Windows_Closing()
    {
        if (windowId == Guid.Empty) return;
        var entry = book.FirstOrDefault(e => e.Id == windowId);
        var others = Application.Current.Windows.OfType<MainWindow>().Count(w => w != this);
        if (App.Quitting || others == 0)
        {
            if (entry is not null)
            {
                var b = WindowState == WindowState.Maximized ? RestoreBounds : new Rect(Left, Top, Width, Height);
                entry.Placement = new WindowPlacement { Left = b.Left, Top = b.Top, Width = b.Width, Height = b.Height, Maximized = WindowState == WindowState.Maximized };
                WindowList.Save(book);
            }
            return;
        }
        // Closed on purpose, with others still open: forget it and the files of its spaces.
        book.RemoveAll(e => e.Id == windowId);
        WindowList.Save(book);
        foreach (var s in Shared.Book.Spaces)
            try { File.Delete(Store.File(Session.FileFor(s.Id, windowId))); } catch { }
    }
}
