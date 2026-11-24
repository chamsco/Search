using Search.Core;
using Xunit;

[Collection("Store")]
public class SpaceTests : IDisposable
{
    readonly string old = Store.Root;
    public SpaceTests() => Store.Root = Path.Combine(Path.GetTempPath(), "search-test-" + Guid.NewGuid().ToString("N"));
    public void Dispose() { try { Directory.Delete(Store.Root, true); } catch { } Store.Root = old; }

    [Fact]
    public void StartsWithOneProfileAndOneSpace()
    {
        var b = SpaceBook.Load();
        Assert.Single(b.Profiles);
        Assert.Single(b.Spaces);
        Assert.True(b.Spaces[0].IsFirst);
        Assert.Null(SpaceBook.StoreName(b.Spaces[0])); // the store there has always been: nobody is signed out
    }

    [Fact]
    public void SpacesPersistAndKeepOrder()
    {
        var b = SpaceBook.Load();
        var work = b.AddSpace(Spaces.PersonalId, "Work", colour: 1);
        var play = b.AddSpace(Spaces.PersonalId, "Play", colour: 3, sharesSignIns: false);
        var again = SpaceBook.Load();
        Assert.Equal(["Personal", "Work", "Play"], again.In(Spaces.PersonalId).Select(s => s.Name));
        Assert.Equal(1, again.Spaces[1].Colour);
        Assert.False(again.Spaces[2].SharesSignIns);
        Assert.Null(SpaceBook.StoreName(work));                // shares: the profile's
        Assert.Equal("S" + play.Id.ToString("N"), SpaceBook.StoreName(play)); // its own
    }

    [Fact]
    public void TheFirstSpaceCannotBeDeletedButOthersCan()
    {
        var b = SpaceBook.Load();
        var s = b.AddSpace(Spaces.PersonalId, "Temp");
        Assert.False(b.DeleteSpace(Spaces.FirstId));
        Assert.True(b.DeleteSpace(s.Id));
        Assert.Single(b.Spaces);
    }

    [Fact]
    public void MovingASpaceChangesItsPlaceInItsProfileOnly()
    {
        var b = SpaceBook.Load();
        var a = b.AddSpace(Spaces.PersonalId, "A");
        var c = b.AddSpace(Spaces.PersonalId, "C");
        b.Move(c.Id, -1);
        Assert.Equal(["Personal", "C", "A"], b.In(Spaces.PersonalId).Select(s => s.Name));
        b.Move(c.Id, -5);
        Assert.Equal("C", b.In(Spaces.PersonalId).First().Name);
    }

    [Fact]
    public void AProfileHasItsOwnSpacesStoreAndFiles()
    {
        var b = SpaceBook.Load();
        var p = b.AddProfile("Work");
        Assert.Equal(2, b.Profiles.Count);
        var home = b.HomeOf(p.Id);
        Assert.Equal("P" + p.Id.ToString("N"), SpaceBook.StoreName(home));
        Assert.NotEqual(SpaceBook.HistoryFile(Spaces.PersonalId), SpaceBook.HistoryFile(p.Id));
        Assert.Equal("history.json", SpaceBook.HistoryFile(Spaces.PersonalId));
        Assert.Equal("session.json", Session.FileFor(Spaces.FirstId));
        Assert.StartsWith("session-", Session.FileFor(home.Id));
    }

    [Fact]
    public void DeletingAProfileTakesItsSpacesAndNeverPersonal()
    {
        var b = SpaceBook.Load();
        var p = b.AddProfile("Work");
        b.AddSpace(p.Id, "Extra");
        Assert.False(b.DeleteProfile(Spaces.PersonalId));
        Assert.True(b.DeleteProfile(p.Id));
        Assert.Single(b.Spaces);
        Assert.Single(SpaceBook.Load().Profiles);
    }

    [Fact]
    public void HistoryAndBookmarksOfAProfileAreSeparateFiles()
    {
        var p = Guid.NewGuid();
        var h = History.Load(SpaceBook.HistoryFile(p));
        h.Record("https://example.com", "Example");
        h.Save();
        Assert.Single(History.Load(SpaceBook.HistoryFile(p)).All);
        Assert.Empty(History.Load().All);
        var m = Bookmarks.Load(SpaceBook.BookmarksFile(p));
        m.Add("https://example.org", "Org");
        Assert.Single(Bookmarks.Load(SpaceBook.BookmarksFile(p)).Items);
        Assert.Empty(Bookmarks.Load().Items);
    }

    [Fact]
    public void StrengthWashesMoreAndTheDarkGroundTakesMore()
    {
        Assert.True(Spaces.Wash(SpaceStrength.Strong, false) > Spaces.Wash(SpaceStrength.Subtle, false));
        Assert.True(Spaces.Wash(SpaceStrength.Subtle, true) > Spaces.Wash(SpaceStrength.Subtle, false));
        Assert.Null(Spaces.Rgb(0));
        Assert.NotNull(Spaces.Rgb(2));
        Assert.Null(Spaces.Rgb(99));
    }
}
