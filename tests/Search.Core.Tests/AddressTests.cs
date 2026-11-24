using Search.Core;
using Xunit;

namespace Search.Core.Tests;

// Ported from Tests/AddressRegression/main.swift and Tests/SearchTests/AddressTests.swift.
public class AddressTests
{
    [Theory]
    // A server on this machine or the local network: plain http.
    [InlineData("localhost:3000", "http://localhost:3000")]
    [InlineData("printer.local", "http://printer.local")]
    [InlineData("127.0.0.1:8080", "http://127.0.0.1:8080")]
    [InlineData("0.0.0.0:8080", "http://0.0.0.0:8080")]
    [InlineData("10.0.0.1", "http://10.0.0.1")]
    [InlineData("10.255.255.254:9000", "http://10.255.255.254:9000")]
    [InlineData("192.168.1.1", "http://192.168.1.1")]
    [InlineData("172.16.0.1", "http://172.16.0.1")]
    [InlineData("172.31.255.254", "http://172.31.255.254")]
    // A website, whatever its name looks like: https.
    [InlineData("example.com", "https://example.com")]
    [InlineData("172.15.0.1", "https://172.15.0.1")]
    [InlineData("172.32.0.1", "https://172.32.0.1")]
    [InlineData("10.io", "https://10.io")]
    [InlineData("10.org/about", "https://10.org/about")]
    [InlineData("10.0.0.1.example.com", "https://10.0.0.1.example.com")]
    [InlineData("192.168.1.1.example.com", "https://192.168.1.1.example.com")]
    [InlineData("localhost:3000/test", "http://localhost:3000/test")]
    [InlineData("https://example.com/a?q=b#c", "https://example.com/a?q=b#c")]
    // Not a place at all.
    [InlineData("hello world", null)]
    [InlineData("todo", null)]
    [InlineData("", null)]
    [InlineData("two words", null)]
    [InlineData("./report.pdf", null)]
    [InlineData("person@example.com", null)]
    [InlineData("javascript:alert(1)", null)]
    [InlineData("vnc://192.168.1.20", null)]
    [InlineData("version 1.2.3", null)]
    [InlineData("1.2.3", null)]
    public void SchemeForTypedAddress(string typed, string? wanted) =>
        Assert.Equal(wanted, Address.Url(typed));

    [Fact]
    public void ZeroAddressOpensAsLocalhost() =>
        Assert.Equal("http://localhost:3000/", Address.Reachable("http://0.0.0.0:3000"));

    [Theory]
    [InlineData("https://example.com/", "example.com")]
    [InlineData("https://example.com/a", "example.com/a")]
    [InlineData("http://example.com/", "http://example.com/")]
    [InlineData("http://localhost:3000/", "localhost:3000")]
    public void EditableGoesBackToTheSamePage(string full, string shown) =>
        Assert.Equal(shown, Address.Editable(full));

    [Theory]
    [InlineData("https://www.apple.com/", "apple.com")]
    [InlineData("https://github.com/driceroland/search", "github.com/driceroland/search")]
    public void Pretty(string url, string shown) => Assert.Equal(shown, Address.Pretty(url));
}

public class OmniboxTests
{
    [Fact]
    public void WordsAreSearched() =>
        Assert.Equal("https://www.google.com/search?q=hello%20world", Omnibox.Resolve("hello world", Engine.Google));

    [Fact]
    public void AddressesAreOpened() =>
        Assert.Equal("https://example.com", Omnibox.Resolve(" example.com ", Engine.Google));

    [Fact]
    public void DevServerZeroBecomesLocalhost() =>
        Assert.Equal("http://localhost:5173/", Omnibox.Resolve("0.0.0.0:5173", Engine.Google));

    [Fact]
    public void CustomEngineFromAnotherBrowser() =>
        Assert.Equal("https://example.org/find?q=cats", Engine.Custom.SearchUrl("cats", "example.org/find?q={searchTerms}"));

    [Fact]
    public void BadCustomEngineFallsBackToGoogle() =>
        Assert.StartsWith("https://www.google.com/", Engine.Custom.SearchUrl("cats", "not a template"));
}

public class FuzzyTests
{
    [Theory]
    [InlineData("github", "github.com code", 1)]
    [InlineData("gihtub", "github.com code", 0)]
    [InlineData("githbu", "github.com code", 0)]
    [InlineData("xyzzy", "github.com code", null)]
    [InlineData("gh", "github.com", null)]
    public void Match(string typed, string text, int? wanted) =>
        Assert.Equal(wanted, Fuzzy.Match(Fuzzy.Words(typed), text));

    [Fact]
    public void Abbreviation()
    {
        Assert.True(Fuzzy.Abbreviates("dwn", "downloads"));
        Assert.False(Fuzzy.Abbreviates("wdn", "downloads"));
    }
}

public class HistoryTests
{
    [Fact]
    public void FrontDoorRanksAboveInnerPage()
    {
        var h = new History(() => new DateTime(2026, 11, 24));
        h.Record("https://github.com/driceroland/search", "Search");
        h.Record("https://github.com/", "GitHub");
        var s = h.Suggestions("git");
        Assert.Equal("github.com", s[0].Key);
    }

    [Fact]
    public void OneLetterCompletesOnlyAVisitedFrontDoor()
    {
        var h = new History(() => new DateTime(2026, 11, 24));
        h.Record("https://x.com/", "X");
        var s = h.Suggestions("x");
        Assert.Equal(".com", History.Completion("x", s));
    }

    [Fact]
    public void TypoStillFinds()
    {
        var h = new History(() => new DateTime(2026, 11, 24));
        h.Record("https://news.ycombinator.com/", "Hacker News");
        Assert.NotEmpty(h.Suggestions("hakcer"));
    }

    [Fact]
    public void EmptyFieldProposesNothing() => Assert.Empty(new History().Suggestions("  "));

    [Fact]
    public void IdentityIgnoresSchemeWwwAndSlash() =>
        Assert.Equal(History.Identity("http://www.example.com/"), History.Identity("https://example.com#top"));
}

public class SuggestionTests
{
    [Fact]
    public void FirstDayKnowsPopularSites()
    {
        var h = new History(() => new DateTime(2026, 11, 24));
        var s = h.Suggestions("you");
        Assert.Equal("youtube.com", s[0].Key);
        Assert.Equal("tube.com", History.Completion("you", s));
    }

    [Fact]
    public void KnownSiteFoundByItsName() =>
        Assert.Contains(new History().Suggestions("hacker"), s => s.Key == "news.ycombinator.com");

    [Fact]
    public void VisitedOutranksKnown()
    {
        var h = new History(() => new DateTime(2026, 11, 24));
        h.Record("https://gitlab.com/", "GitLab");
        Assert.Equal("gitlab.com", h.Suggestions("git")[0].Key);
    }

    [Fact]
    public void KnownSiteDroppedOnceVisited()
    {
        var h = new History(() => new DateTime(2026, 11, 24));
        h.Record("https://github.com/driceroland/search", "Search");
        Assert.DoesNotContain(h.Suggestions("github"), s => s.Kind == SuggestionKind.Known);
    }

    [Theory]
    [InlineData("[\"cat\",[\"cats\",\"cat videos\"]]", 2)]
    [InlineData("[{\"phrase\":\"cats\"},{\"phrase\":\"cat food\"}]", 2)]
    [InlineData("not json", 0)]
    public void ParsesEngineSuggestions(string body, int count) => Assert.Equal(count, EngineSuggester.Parse(body).Count);

    [Fact]
    public void EngineWordsGoUnderTheRestWithoutRepeatingTheTypedText()
    {
        var rows = Omnibox.Offers("cat", new History(), Engine.Google);
        var all = Omnibox.WithEngine(rows, "cat", ["cat", "cats", "Cats"], Engine.Google);
        Assert.Single(all, s => s.Kind == SuggestionKind.Suggested);
        Assert.Equal(SuggestionKind.Suggested, all[^1].Kind);
    }
}

public class UpdateTests
{
    const string Feed = """
    [
      {"tag_name":"v1.0.19.14","draft":false,"html_url":"https://github.com/chamsco/Search/releases/tag/v1.0.19.14",
       "assets":[{"name":"Search-win-x64-portable.zip","browser_download_url":"https://x/zip"},
                 {"name":"Search-Setup-1.0.19.14-x64.exe","browser_download_url":"https://x/14.exe"}]},
      {"tag_name":"v1.0.19-win.3","draft":false,"html_url":"h","assets":[{"name":"Search-Setup-1.0.19-x64.exe","browser_download_url":"https://x/3.exe"}]},
      {"tag_name":"v1.0.19.20","draft":true,"html_url":"h","assets":[{"name":"Search-Setup-1.0.19.20-x64.exe","browser_download_url":"https://x/20.exe"}]}
    ]
    """;

    [Fact]
    public void PicksNewestNonDraftWithInstaller()
    {
        var r = Updates.Newer(Feed, new System.Version(1, 0, 19, 3));
        Assert.NotNull(r);
        Assert.Equal("https://x/14.exe", r!.InstallerUrl);
    }

    [Fact]
    public void NothingWhenUpToDate() => Assert.Null(Updates.Newer(Feed, new System.Version(1, 0, 19, 14)));

    [Theory]
    [InlineData("v1.0.19-win.3", "1.0.19.3")]
    [InlineData("v1.0.19.14", "1.0.19.14")]
    public void ReadsBothTagShapes(string tag, string version) => Assert.Equal(System.Version.Parse(version), Updates.VersionOf(tag));
}

[Collection("Store")]
public class BookmarkTests : IDisposable
{
    readonly string old = Store.Root;
    public BookmarkTests() => Store.Root = Path.Combine(Path.GetTempPath(), "search-test-" + Guid.NewGuid().ToString("N"));
    public void Dispose() { try { Directory.Delete(Store.Root, true); } catch { } Store.Root = old; }

    [Fact]
    public void AddOnceByIdentity()
    {
        var b = Bookmarks.Load();
        var one = b.Add("https://www.example.com/", "Example");
        var two = b.Add("http://example.com", "Again");
        Assert.Equal(one.Id, two.Id);
        Assert.Single(b.Items);
        Assert.True(b.Contains("https://example.com/#x"));
    }

    [Fact]
    public void KeptBetweenRuns()
    {
        var b = Bookmarks.Load();
        b.Add("https://a.com", "A");
        Assert.Single(Bookmarks.Load().Items);
    }

    [Fact]
    public void RenameMoveReorder()
    {
        var b = Bookmarks.Load();
        var a = b.Add("https://a.com", "A");
        var c = b.Add("https://c.com", "C");
        var f = b.AddFolder("Work");
        b.Move(c.Id, f.Id);
        b.Rename(a.Id, "  Alpha ");
        b.Reorder(c.Id, 0);
        Assert.Equal("Alpha", b.Items.First(x => x.Id == a.Id).Title);
        Assert.Equal(c.Id, b.Items[0].Id);
        Assert.Single(b.In(f.Id));
        b.RemoveFolder(f.Id);
        Assert.Null(b.Items.First(x => x.Id == c.Id).Folder);
    }

    [Fact]
    public void SuggestByNameOrAddress()
    {
        var b = Bookmarks.Load();
        b.Add("https://news.ycombinator.com/", "Hacker News");
        Assert.Single(b.Suggest("hacker"));
        Assert.Single(b.Suggest("ycomb"));
        Assert.Empty(b.Suggest("zzz"));
    }

    [Fact]
    public void RemoveByUrl()
    {
        var b = Bookmarks.Load();
        b.Add("https://a.com", "A");
        b.Remove("https://a.com/");
        Assert.Empty(b.Items);
    }
}

[Collection("Store")]
public class PrefsTests : IDisposable
{
    readonly string old = Store.Root;
    public PrefsTests() => Store.Root = Path.Combine(Path.GetTempPath(), "search-test-" + Guid.NewGuid().ToString("N"));
    public void Dispose() { try { Directory.Delete(Store.Root, true); } catch { } Store.Root = old; }

    [Fact]
    public void DefaultsAndPersistence()
    {
        var p = Prefs.Load();
        Assert.Equal(Engine.Google, p.Data.Engine);
        Assert.True(p.Data.EngineSuggests);
        p.Update(d => { d.Engine = Engine.DuckDuckGo; d.Look = LookChoice.Dark; });
        var again = Prefs.Load();
        Assert.Equal(Engine.DuckDuckGo, again.Data.Engine);
        Assert.Equal(LookChoice.Dark, again.Data.Look);
    }

    [Fact]
    public void MissingDownloadFolderFallsBack()
    {
        var p = Prefs.Load();
        p.Update(d => d.DownloadFolder = @"Z:\nope\nothing");
        Assert.EndsWith("Downloads", p.EffectiveDownloadFolder);
    }
}

[CollectionDefinition("Store", DisableParallelization = true)]
public class StoreCollection;

public class InternalPageTests
{
    [Theory]
    [InlineData("search://settings", "settings", null)]
    [InlineData("search://settings/privacy", "settings", "privacy")]
    [InlineData("SEARCH://History/", "history", null)]
    public void Parses(string url, string kind, string? detail) => Assert.Equal((kind, detail), InternalPages.Parse(url));

    [Theory]
    [InlineData("search://nothing")]
    [InlineData("https://example.com")]
    public void NotOurs(string url) => Assert.Null(InternalPages.Parse(url));

    [Fact]
    public void AddressFieldTakesOurPages() => Assert.Equal("search://history", Address.Url("search://history"));

    [Theory]
    [InlineData("sett", "search://settings")]
    [InlineData("dwn", "search://downloads")]
    [InlineData("hist", "search://history")]
    public void CommandsInTheField(string typed, string url) =>
        Assert.Contains(Omnibox.Offers(typed, new History(), Engine.Google), s => s.Kind == SuggestionKind.Command && s.Url == url);

    [Fact]
    public void HistoryQueryAndClear()
    {
        var now = new DateTime(2026, 11, 24, 12, 0, 0);
        var at = now;
        var h = new History(() => at);
        at = now.AddHours(-30); h.Record("https://old.com/", "Old");
        at = now.AddMinutes(-10); h.Record("https://new.com/", "New one");
        Assert.Equal("New one", h.Query("").First().Title);
        Assert.Single(h.Query("old"));
        Assert.Equal(1, h.ClearSince(now.AddHours(-1)));
        Assert.Single(h.All);
        Assert.True(h.Remove("https://old.com"));
        Assert.Empty(h.All);
    }
}

public class LocalTests
{
    [Theory]
    [InlineData("localhost", true)] [InlineData("printer.local", true)] [InlineData("192.168.1.2", true)] [InlineData("8.8.8.8", true)]
    [InlineData("example.com", false)] [InlineData("10.io", false)]
    public void IsLocal(string host, bool local) => Assert.Equal(local, Address.IsLocal(host));
}
