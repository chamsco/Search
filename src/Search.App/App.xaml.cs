using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using Search.Core;

namespace Search.App;

public partial class App : Application
{
    public static Palette.Look Look { get; set; } = Palette.Look.System;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Nothing a page does should take the whole browser down. What goes
        // wrong is written to %LOCALAPPDATA%\Search\crash.log and the window stays.
        DispatcherUnhandledException += (_, a) => { Log(a.Exception); a.Handled = true; };
        TaskScheduler.UnobservedTaskException += (_, a) => { Log(a.Exception); a.SetObserved(); };
        AppDomain.CurrentDomain.UnhandledException += (_, a) => Log(a.ExceptionObject as Exception);

        base.OnStartup(e);
        Palette.Apply(Resources, Look);
        SystemEvents.UserPreferenceChanged += (_, a) =>
        {
            if (a.Category == UserPreferenceCategory.General)
                Dispatcher.Invoke(() => Palette.Apply(Resources, Look));
        };
        // A link handed to Search on the command line (default browser, a file opened with it).
        var first = e.Args.FirstOrDefault(a => !a.StartsWith('-'));
        var window = new MainWindow(first);
        MainWindow = window;
        window.Show();
    }

    public static void Log(Exception? e)
    {
        if (e is null) return;
        try
        {
            var path = Store.File("crash.log");
            if (File.Exists(path) && new FileInfo(path).Length > 1_000_000) File.Delete(path);
            File.AppendAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {Version.Current}\n{e}\n\n");
        }
        catch { /* the log is best effort */ }
    }
}
