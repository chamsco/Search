using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Search.Core;

namespace Search.App;

public enum DownloadState { Progress, Done, Cancelled, Failed }

/// <summary>One file. Port of the model behind Sources/Search/DownloadsFloat.swift: pause and resume, a progress line, open and show in folder.</summary>
public sealed class DownloadItem : INotifyPropertyChanged
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = "";
    public string Path { get; init; } = "";
    public string Url { get; init; } = "";
    public DateTime Started { get; init; } = DateTime.Now;
    public CoreWebView2DownloadOperation? Op { get; set; }

    long received, total;
    DownloadState state;
    bool paused;
    public long Received { get => received; set => Set(ref received, value); }
    public long Total { get => total; set { if (Set(ref total, value)) Raise(nameof(Fraction)); } }
    public DownloadState State { get => state; set { if (Set(ref state, value)) { Raise(nameof(Line)); Raise(nameof(InProgress)); Raise(nameof(CanOpen)); } } }
    public bool Paused { get => paused; set { if (Set(ref paused, value)) Raise(nameof(Line)); } }

    public bool InProgress => state == DownloadState.Progress;
    public bool Exists => File.Exists(Path);
    public bool CanOpen => state == DownloadState.Done && Exists;
    public double Fraction => total > 0 ? Math.Min(1, (double)received / total) : 0;

    public static string Size(long n) =>
        n > 1_000_000_000 ? $"{n / 1e9:0.0} GB" : n > 1_000_000 ? $"{n / 1e6:0.0} MB" : n > 1000 ? $"{n / 1000} KB" : $"{n} B";

    public string Line => state switch
    {
        DownloadState.Progress => $"{Size(received)}{(total > 0 ? " of " + Size(total) : "")}{(paused ? " · paused" : "")}",
        DownloadState.Done => Exists ? Size(total > 0 ? total : received) : "File was moved or deleted",
        DownloadState.Cancelled => "Cancelled",
        _ => "Failed",
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    void Raise(string p) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    bool Set<T>(ref T field, T value, [CallerMemberName] string? p = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value; Raise(p!); if (p is nameof(Received)) { Raise(nameof(Line)); Raise(nameof(Fraction)); } return true;
    }
}

public sealed class Downloads
{
    sealed record Saved(Guid Id, string Name, string Path, string Url, long Total, DownloadState State, DateTime Started);

    public ObservableCollection<DownloadItem> Items { get; } = [];
    public event Action? Changed;
    readonly DispatcherTimer settle = new() { Interval = TimeSpan.FromMilliseconds(250) };

    public Downloads()
    {
        foreach (var s in Store.Read<List<Saved>>("downloads.json") ?? [])
            Items.Add(new DownloadItem { Id = s.Id, Name = s.Name, Path = s.Path, Url = s.Url, Started = s.Started, Total = s.Total, Received = s.Total, State = s.State == DownloadState.Progress ? DownloadState.Cancelled : s.State });
        settle.Tick += (_, _) => { settle.Stop(); Changed?.Invoke(); };
    }

    void Notify() { if (!settle.IsEnabled) settle.Start(); }

    void Save() => Store.Write("downloads.json",
        Items.Where(i => !i.InProgress).Select(i => new Saved(i.Id, i.Name, i.Path, i.Url, i.Total, i.State, i.Started)).ToList());

    /// <summary>The name for a file that doesn't replace one already there: report (1).pdf, report (2).pdf…</summary>
    public static string UniquePath(string folder, string fileName)
    {
        var name = string.Concat(fileName.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        if (name.Length == 0) name = "download";
        var path = Path.Combine(folder, name);
        for (var n = 1; File.Exists(path) || File.Exists(path + ".crdownload"); n++)
            path = Path.Combine(folder, $"{Path.GetFileNameWithoutExtension(name)} ({n}){Path.GetExtension(name)}");
        return path;
    }

    public DownloadItem Begin(CoreWebView2DownloadOperation op, string path)
    {
        var item = new DownloadItem { Name = Path.GetFileName(path), Path = path, Url = op.Uri, Op = op, Total = (long)(op.TotalBytesToReceive ?? 0) };
        Items.Insert(0, item);
        op.BytesReceivedChanged += (_, _) => { item.Received = op.BytesReceived; item.Total = (long)(op.TotalBytesToReceive ?? 0); Notify(); };
        op.StateChanged += (_, _) =>
        {
            item.Paused = op.State == CoreWebView2DownloadState.Interrupted && op.CanResume;
            item.State = op.State switch
            {
                CoreWebView2DownloadState.Completed => DownloadState.Done,
                CoreWebView2DownloadState.Interrupted when op.InterruptReason == CoreWebView2DownloadInterruptReason.UserCanceled => DownloadState.Cancelled,
                CoreWebView2DownloadState.Interrupted when op.CanResume => DownloadState.Progress,
                CoreWebView2DownloadState.Interrupted => DownloadState.Failed,
                _ => DownloadState.Progress,
            };
            if (item.State == DownloadState.Done) { item.Received = item.Total = new FileInfo(path).Exists ? new FileInfo(path).Length : item.Received; }
            if (!item.InProgress) Save();
            Notify();
        };
        Notify();
        return item;
    }

    public DownloadItem? Find(Guid id) => Items.FirstOrDefault(i => i.Id == id);
    public void Pause(DownloadItem i) { try { i.Op?.Pause(); i.Paused = true; } catch { } Notify(); }
    public void Resume(DownloadItem i) { try { i.Op?.Resume(); i.Paused = false; } catch { } Notify(); }
    public void Cancel(DownloadItem i) { try { i.Op?.Cancel(); } catch { } i.State = DownloadState.Cancelled; Save(); Notify(); }
    public void Remove(DownloadItem i) { Items.Remove(i); Save(); Notify(); }
    public void ClearFinished() { foreach (var i in Items.Where(i => !i.InProgress).ToList()) Items.Remove(i); Save(); Notify(); }
    public void ClearSince(DateTime since) { foreach (var i in Items.Where(i => !i.InProgress && i.Started >= since).ToList()) Items.Remove(i); Save(); Notify(); }
}
