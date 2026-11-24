using System.IO;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;
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
        SessionEnding += (_, _) => Quitting = true;

        // One Search per data folder: a second launch (a link from another app, the Start menu) hands its address to the first.
        var id = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Store.File("x").ToLowerInvariant())))[..16];
        var first0 = e.Args.FirstOrDefault(a => !a.StartsWith('-'));
        instance = new Mutex(true, "Search.Instance." + id, out var owner);
        if (!owner)
        {
            try
            {
                using var client = new NamedPipeClientStream(".", "Search.Pipe." + id, PipeDirection.Out);
                client.Connect(3000);
                using var w = new StreamWriter(client) { AutoFlush = true };
                w.WriteLine(first0 ?? "");
            }
            catch (Exception ex) { Log(ex); }
            Shutdown();
            return;
        }
        ShutdownMode = ShutdownMode.OnLastWindowClose;
        _ = Listen("Search.Pipe." + id);
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
        // The other windows that were open last time.
        foreach (var saved in Search.App.MainWindow.Remembered)
            new MainWindow(null, true, saved.Profile, saved.Id).Show();
    }

    static Mutex? instance;
    /// <summary>Set when the whole app is going away (an update, the system closing): windows keep their tabs for next time.</summary>
    public static bool Quitting { get; set; }

    async Task Listen(string name)
    {
        while (true)
        {
            try
            {
                await using var server = new NamedPipeServerStream(name, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                await server.WaitForConnectionAsync();
                using var reader = new StreamReader(server);
                var line = (await reader.ReadLineAsync())?.Trim() ?? "";
                await Dispatcher.InvokeAsync(() => Reached(line));
            }
            catch (Exception ex) { Log(ex); await Task.Delay(500); }
        }
    }

    /// <summary>Another launch said hello: show the window it would have made, with its address if it had one.</summary>
    void Reached(string url)
    {
        var window = Windows.OfType<MainWindow>().FirstOrDefault(w => w.IsActive) ?? Windows.OfType<MainWindow>().FirstOrDefault();
        if (window is null) return;
        if (url.Length > 0) window.OpenExternal(url);
        else { if (window.WindowState == WindowState.Minimized) window.WindowState = WindowState.Normal; window.Activate(); }
    }

    /// <summary>A line in trace.log, for finding out what happened on a machine nobody can watch.</summary>
    public static void Trace(string line)
    {
        try { File.AppendAllText(Store.File("trace.log"), $"[{DateTime.Now:HH:mm:ss.fff}] {line}\n"); } catch { }
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
