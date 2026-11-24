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
