using Search.Core;
using Xunit;

public class SplitTests
{
    static Guid[] Ids(int n) => Enumerable.Range(0, n).Select(_ => Guid.NewGuid()).ToArray();

    [Fact]
    public void HoldsTwoToFourPagesAndNoMore()
    {
        var ids = Ids(5);
        var s = new TabSplit(ids.Take(2));
        Assert.True(s.Add(ids[2]));
        Assert.True(s.Add(ids[3]));
        Assert.False(s.Add(ids[4]));
        Assert.Equal(4, s.Tabs.Count);
        Assert.Equal(4, new TabSplit(ids).Tabs.Count);
    }

    [Fact]
    public void SizesAddUpToOneAndShareEvenly()
    {
        var s = new TabSplit(Ids(3));
        Assert.Equal(1.0, s.Sizes.Sum(), 6);
        Assert.All(s.Sizes, x => Assert.Equal(1.0 / 3, x, 6));
    }

    [Fact]
    public void ResizingMovesTheLineButNeverLeavesAPageTooThin()
    {
        var s = new TabSplit(Ids(2));
        s.Resize(0, 0.2);
        Assert.Equal(0.7, s.Sizes[0], 6);
        s.Resize(0, 5);
        Assert.Equal(1.0, s.Sizes.Sum(), 6);
        Assert.True(s.Sizes[1] >= TabSplit.LeastShare - 1e-9);
    }

    [Fact]
    public void LeavingTheSplitSharesTheRoomAndEndsItBelowTwo()
    {
        var ids = Ids(3);
        var s = new TabSplit(ids);
        Assert.True(s.Remove(ids[1]));
        Assert.Equal(2, s.Tabs.Count);
        Assert.False(s.Remove(ids[0]));
    }

    [Fact]
    public void RectsFillTheStageWithGapsBetween()
    {
        var s = new TabSplit(Ids(2));
        var r = s.Rects(1000, 600, 8);
        Assert.Equal(0, r[0].X);
        Assert.Equal(496, r[0].W, 6);
        Assert.Equal(504, r[1].X, 6);
        Assert.Equal(600, r[1].H);
        s.Axis = SplitAxis.Vertical;
        var v = s.Rects(1000, 600, 8);
        Assert.Equal(1000, v[0].W);
        Assert.Equal(296, v[0].H, 6);
        Assert.Equal(304, v[1].Y, 6);
    }

    [Fact]
    public void SavesAsPlacesInTheRow()
    {
        var ids = Ids(3);
        var s = new TabSplit([ids[2], ids[0]], SplitAxis.Vertical) { Focused = ids[0] };
        var e = s.ToEntry(id => Array.IndexOf(ids, id));
        Assert.Equal([2, 0], e.Tabs);
        Assert.Equal(0, e.Focused); // a place in the session row, the same numbering as Tabs
        Assert.Equal(SplitAxis.Vertical, e.Axis);
    }
}

[Collection("Store")]
public class ArchiveTests : IDisposable
{
    readonly string old = Store.Root;
    public ArchiveTests() => Store.Root = Path.Combine(Path.GetTempPath(), "search-test-" + Guid.NewGuid().ToString("N"));
    public void Dispose() { try { Directory.Delete(Store.Root, true); } catch { } Store.Root = old; }

    [Fact]
    public void KeepsNewestFirstAndOnlyRealPages()
    {
        var a = Archive.Load();
        a.Add("https://example.com/a", "A");
        a.Add("search://settings", "Settings");
        a.Add("", "");
        a.Add("https://example.org/b", "B");
        Assert.Equal(["B", "A"], a.Items.Select(i => i.Title));
        Assert.Equal(2, Archive.Load().Items.Count);
    }

    [Fact]
    public void ArchivingAPageAgainMovesItUp()
    {
        var a = Archive.Load();
        a.Add("https://example.com/a", "A");
        a.Add("https://example.org/b", "B");
        a.Add("https://www.example.com/a/", "A again");
        Assert.Equal(["A again", "B"], a.Items.Select(i => i.Title));
    }

    [Fact]
    public void ForgetsAfterThirtyDaysAndPastFiveHundred()
    {
        var t = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var a = Archive.Load(clock: () => t);
        a.Add("https://old.example.com", "Old");
        t = t.AddDays(31);
        a.Add("https://new.example.com", "New");
        Assert.Equal(["New"], a.Items.Select(i => i.Title));
        for (var i = 0; i < 520; i++) a.Add($"https://site{i}.example.com", "S" + i);
        Assert.Equal(Archive.Cap, a.Items.Count);
        Assert.Equal("S519", a.Items[0].Title);
    }

    [Fact]
    public void SearchFindsByTitleOrAddressAndProfilesKeepTheirOwn()
    {
        var a = Archive.Load();
        a.Add("https://example.com/recipes", "Pasta recipes");
        a.Add("https://news.example.org", "Headlines");
        Assert.Equal(["Pasta recipes"], a.Search("pasta").Select(i => i.Title));
        Assert.Equal(["Headlines"], a.Search("news").Select(i => i.Title));
        Assert.Equal(2, a.Search("").Count());
        Assert.Equal("archive.json", Archive.FileFor(Spaces.PersonalId));
        Assert.NotEqual("archive.json", Archive.FileFor(Guid.NewGuid()));
        a.Clear();
        Assert.Empty(Archive.Load().Items);
    }

    [Fact]
    public void IdleTabsAreThoseLeftAloneForTheirTimeAndNeverWhenOff()
    {
        var now = new DateTime(2026, 5, 10);
        var a = Guid.NewGuid(); var b = Guid.NewGuid(); var c = Guid.NewGuid();
        var tabs = new[] { (a, now.AddDays(-8), true), (b, now.AddDays(-2), true), (c, now.AddDays(-30), false) };
        Assert.Equal([a], IdleTabs.Due(tabs, 7, now));
        Assert.Equal([a, b], IdleTabs.Due(tabs, 1, now));
        Assert.Empty(IdleTabs.Due(tabs, 0, now));
    }

    [Fact]
    public void SessionFilesNameTheWindowToo()
    {
        var w = Guid.NewGuid();
        Assert.Equal("session.json", Session.FileFor(Spaces.FirstId, Guid.Empty));
        Assert.Equal($"session-w{w:N}.json", Session.FileFor(Spaces.FirstId, w));
        var s = Guid.NewGuid();
        Assert.Equal($"session-{s:N}-w{w:N}.json", Session.FileFor(s, w));
        WindowList.Save([new WindowEntry { Id = w }]);
        Assert.Equal(w, Assert.Single(WindowList.Load()).Id);
    }
}
