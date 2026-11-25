using Xunit;

namespace Search.Core.Tests;

public class FolderSyncTests
{
    // Two PCs are two data folders and one sync folder; Store.Root is switched between their passes.
    sealed class Pc : IDisposable
    {
        public readonly string Data = Path.Combine(Path.GetTempPath(), "pc-" + Guid.NewGuid().ToString("N")[..6]);
        readonly string shared;
        public Pc(string shared) { this.shared = shared; Directory.CreateDirectory(Data); }
        public T In<T>(Func<T> f) { var was = Store.Root; Store.Root = Data; try { return f(); } finally { Store.Root = was; } }
        public (FolderSync Sync, Prefs Prefs, SpaceBook Book, Bookmarks Marks) Open()
        {
            var prefs = Prefs.Load(); var book = SpaceBook.Load(); var marks = Bookmarks.Load();
            var sync = new FolderSync(prefs, book, _ => marks);
            sync.Configure(true, shared);
            return (sync, prefs, book, marks);
        }
        public void Dispose() { try { Directory.Delete(Data, true); } catch { } }
    }

    [Fact]
    public void TwoPcsConvergeOnBookmarksSpacesPinsAndSettings()
    {
        var folder = Path.Combine(Path.GetTempPath(), "sync-" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(folder);
        using var a = new Pc(folder); using var b = new Pc(folder);
        try
        {
            Guid space = default;
            a.In(() =>
            {
                var (sync, prefs, book, marks) = a.Open();
                prefs.Update(d => d.BookmarksBar = true);
                marks.Add("https://one.example/", "One");
                var s = book.AddSpace(Spaces.PersonalId, "Reading", 3);
                space = s.Id;
                Session.Save(new SessionShape { Tabs = [new SessionEntry { Url = "https://pin.example/", Title = "Pin", PinId = Guid.NewGuid(), Home = "https://pin.example/" }] }, s.Id);
                Assert.True(sync.Run(1000).Ran);
                return 0;
            });
            b.In(() =>
            {
                var (sync, prefs, book, marks) = b.Open();
                var r = sync.Run(2000);
                Assert.Equal(1, r.Devices);
                Assert.True(prefs.Data.BookmarksBar);
                Assert.Equal("https://one.example/", marks.Items.Single().Url);
                Assert.Equal("Reading", book.Spaces.Single(s => s.Id == space).Name);
                Assert.Equal(3, book.Spaces.Single(s => s.Id == space).Colour);
                Assert.Equal("https://pin.example/", Session.Load(space)!.Tabs.Single(t => t.PinId is not null).Home);
                // B renames the space, drops the bookmark, adds one.
                book.Rename(space, "Library");
                marks.Remove("https://one.example/");
                marks.Add("https://two.example/", "Two");
                Assert.True(sync.Run(3000).Ran);
                return 0;
            });
            a.In(() =>
            {
                var (sync, _, book, marks) = a.Open();
                sync.Run(4000);
                Assert.Equal("Library", book.Spaces.Single(s => s.Id == space).Name);
                Assert.Equal(["https://two.example/"], marks.Items.Select(i => i.Url));
                return 0;
            });
        }
        finally { try { Directory.Delete(folder, true); } catch { } }
    }

    [Fact]
    public void LaterStampWinsAndOldTombstonesGo()
    {
        var old = new SyncEntry { V = "a", T = 10, D = "x" };
        var newer = new SyncEntry { V = "b", T = 11, D = "y" };
        Assert.Equal("b", SyncMerge.Merge([new() { ["k"] = old }, new() { ["k"] = newer }])["k"].V);
        var dead = new SyncEntry { V = null, T = 5, D = "x" };
        Assert.Empty(SyncMerge.Prune(new() { ["k"] = dead }, 5 + SyncMerge.TombstoneLife + 1));
        Assert.Single(SyncMerge.Prune(new() { ["k"] = dead }, 6));
    }

    [Fact]
    public void JoiningNeverOverridesWhatIsAlreadyThere()
    {
        var theirs = new Dictionary<string, SyncEntry> { ["set:look"] = new() { V = "\"Dark\"", T = 50, D = "z" } };
        var merged = SyncMerge.Pass(new() { ["set:look"] = "\"Light\"", ["only:mine"] = "1" }, null, [theirs], "me", 100);
        Assert.Equal("\"Dark\"", merged["set:look"].V);
        Assert.Equal("1", merged["only:mine"].V);
    }

    [Fact]
    public void AWholeKindVanishingIsNotADeletion()
    {
        var baseMap = Enumerable.Range(0, 6).ToDictionary(i => "bm:" + i, i => new SyncEntry { V = "x", T = 1, D = "me" });
        var stamped = SyncMerge.Stamp([], baseMap, "me", 10);
        Assert.All(stamped.Values, e => Assert.NotNull(e.V));
        var fewer = SyncMerge.Stamp([], baseMap.Take(2).ToDictionary(k => k.Key, k => k.Value), "me", 10);
        Assert.All(fewer.Values, e => Assert.Null(e.V));
    }

    [Fact]
    public void ForeignAndDamagedFilesAreNotRead()
    {
        Assert.Null(SyncMerge.Decode("not json", "app"));
        Assert.Null(SyncMerge.Decode("{\"format\":2,\"device\":\"d\",\"scope\":\"app\",\"items\":{}}", "app"));
        Assert.Null(SyncMerge.Decode("{\"format\":1,\"device\":\"d\",\"scope\":\"other\",\"items\":{}}", "app"));
        Assert.NotNull(SyncMerge.Decode("{\"format\":1,\"device\":\"d\",\"scope\":\"app\",\"items\":{}}", "app"));
    }
}
