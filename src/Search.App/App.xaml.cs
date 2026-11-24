using System.Windows;
using Microsoft.Win32;

namespace Search.App;

public partial class App : Application
{
    public static Palette.Look Look { get; set; } = Palette.Look.System;

    protected override void OnStartup(StartupEventArgs e)
    {
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
}
