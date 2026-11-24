using System.Windows;
using System.Windows.Threading;
using Search.Core;

namespace Search.App;

/// <summary>Close tabs left alone (Closing.swift, Archive.swift): a tab nobody has looked at for its time closes and waits in the archive.</summary>
public partial class MainWindow
{
    DispatcherTimer? idle;

    void StartIdleSweep()
    {
        Shared.ArchiveOf(profileId).Changed += OnArchiveChanged;
        Closed += (_, _) => Shared.ArchiveOf(profileId).Changed -= OnArchiveChanged;
        idle = new DispatcherTimer { Interval = TimeSpan.FromMinutes(5) };
        idle.Tick += (_, _) => SweepIdle();
        idle.Start();
    }

    void OnArchiveChanged() => PostToPages(ArchiveMessage(), "archive");

    /// <summary>Close, into the archive, every tab (of any space of this window) left alone for the chosen time.</summary>
    public int SweepIdle(DateTime? now = null)
    {
        var days = Shared.Prefs.Data.CloseIdleDays;
        if (days <= 0) return 0;
        var closedNow = 0;
        foreach (var st in states.Values.ToList())
        {
            var candidates = st.Tabs.Select(t => (t.Id, t.LastSeen, Closable: t != live && !st.Splits.Any(x => x.Holds(t.Id)) && !t.IsPin && !t.IsBlank && !t.IsInternal && Archive.Keeps(t.Url) && !t.Loading)).ToList();
            foreach (var id in IdleTabs.Due(candidates, days, now ?? DateTime.UtcNow))
            {
                var t = st.Tabs.First(x => x.Id == id);
                Shared.ArchiveOf(profileId).Add(t.Url, t.Title);
                if (t.View is { } v) { Stage.Children.Remove(v); v.Dispose(); t.View = null; }
                st.Tabs.Remove(t);
                closedNow++;
            }
        }
        if (closedNow > 0) SaveSoon();
        return closedNow;
    }
}
