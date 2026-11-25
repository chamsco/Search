using Search.Core;
using Xunit;

namespace Search.Core.Tests;

public class PrivacyTests : IDisposable
{
    readonly string dir = Path.Combine(Path.GetTempPath(), "search-w6-" + Guid.NewGuid().ToString("N")[..8]);
    readonly string old = Store.Root;
    public PrivacyTests() { Store.Root = dir; }
    public void Dispose() { Store.Root = old; try { Directory.Delete(dir, true); } catch { } }

    [Fact]
    public void OriginKeepsSchemeHostAndPort() => Assert.Equal("https://example.com:8443", SitePermissions.OriginOf("https://Example.com:8443/a/b?c#d"));

    [Fact]
    public void OriginOfNonSiteIsEmpty() { Assert.Equal("", SitePermissions.OriginOf("search://settings")); Assert.Equal("", SitePermissions.OriginOf("nonsense")); }

    [Fact]
    public void AChoiceIsRememberedAndSurvivesReload()
    {
        var p = new SitePermissions();
        p.Set("https://a.com", PermissionKind.Camera, PermissionChoice.Allow);
        p.Set("https://a.com", PermissionKind.Location, PermissionChoice.Block);
        var again = SitePermissions.Load();
        Assert.Equal(PermissionChoice.Allow, again.Get("https://a.com", PermissionKind.Camera));
        Assert.Equal(PermissionChoice.Block, again.Get("https://a.com", PermissionKind.Location));
        Assert.Null(again.Get("https://a.com", PermissionKind.Microphone));
        Assert.Null(again.Get("https://b.com", PermissionKind.Camera));
    }

    [Fact]
    public void ResetForgetsOneThenTheSite()
    {
        var p = new SitePermissions();
        p.Set("https://a.com", PermissionKind.Camera, PermissionChoice.Allow);
        p.Set("https://a.com", PermissionKind.Location, PermissionChoice.Allow);
        p.Reset("https://a.com", PermissionKind.Camera);
        Assert.Single(p.All());
        p.Reset("https://a.com", PermissionKind.Location);
        Assert.Empty(p.Sites);
    }

    [Fact]
    public void ResetAllEmpties() { var p = new SitePermissions(); p.Set("https://a.com", PermissionKind.Camera, PermissionChoice.Block); p.ResetAll(); Assert.Empty(SitePermissions.Load().Sites); }

    [Theory]
    [InlineData("https://x.com/a?utm_source=n&id=3&fbclid=abc", "https://x.com/a?id=3")]
    [InlineData("https://x.com/a?utm_medium=e", "https://x.com/a")]
    [InlineData("https://x.com/a?q=hi%20there&gclid=1#frag", "https://x.com/a?q=hi%20there#frag")]
    [InlineData("https://www.youtube.com/watch?v=abc&si=zzz", "https://www.youtube.com/watch?v=abc")]
    [InlineData("https://example.com/p?si=keep", "https://example.com/p?si=keep")]
    [InlineData("search://settings", "search://settings")]
    public void CleanLinksLoseTheirTracking(string url, string expected) => Assert.Equal(expected, CleanLink.Strip(url));

    [Theory]
    [InlineData("https://doubleclick.net/x.js", true)]
    [InlineData("https://stats.g.doubleclick.net/collect", true)]
    [InlineData("https://www.google-analytics.com/analytics.js", true)]
    [InlineData("https://example.com/doubleclick.net", false)]
    [InlineData("https://notdoubleclick.net/", false)]
    [InlineData("http://localhost:3000/", false)]
    [InlineData("search://settings", false)]
    public void TrackerHostsAreMatchedByDomain(string url, bool expected) => Assert.Equal(expected, Trackers.IsTracker(url));

    [Theory]
    [InlineData("youtube", "YouTube")]
    [InlineData("yout", "YouTube")]
    [InlineData("github.com", "GitHub")]
    [InlineData("stackoverflow", "Stack Overflow")]
    [InlineData("wiki", "Wikipedia")]
    public void ASiteNameMatches(string typed, string name) => Assert.Equal(name, SiteSearch.Match(typed)?.Name);

    [Theory]
    [InlineData("")] [InlineData("y")] [InlineData("weather paris")] [InlineData("zzzzzz")]
    public void WordsAreNotASite(string typed) => Assert.Null(SiteSearch.Match(typed));

    [Fact]
    public void SiteSearchEscapesWords() => Assert.Equal("https://www.youtube.com/results?search_query=cats%20%26%20dogs", SiteSearch.UrlFor(SiteSearch.Match("youtube")!, "cats & dogs"));

    [Theory]
    [InlineData("127.0.0.1:8080", "127.0.0.1:8080")]
    [InlineData(" http://proxy.local:3128/ ", "http://proxy.local:3128")]
    [InlineData("socks5://h:1080", "socks5://h:1080")]
    [InlineData("ftp://h:1", "")] [InlineData("h", "")] [InlineData("h:99999", "")] [InlineData("a b:1", "")] [InlineData("", "")]
    public void ProxyServersAreChecked(string typed, string expected) => Assert.Equal(expected, Proxy.Normalise(typed));

    [Fact]
    public void ProxyArgumentFollowsTheMode()
    {
        Assert.Equal("", Proxy.Argument(ProxyMode.System, "h:1"));
        Assert.Equal("--no-proxy-server", Proxy.Argument(ProxyMode.Off, ""));
        Assert.Equal("--proxy-server=h:1", Proxy.Argument(ProxyMode.Custom, "h:1"));
        Assert.Equal("", Proxy.Argument(ProxyMode.Custom, "nonsense"));
    }

    [Fact]
    public void StylesApplyToSubdomainsAndStack()
    {
        var s = new SiteStyles();
        s.Set("example.com", "body{color:red}");
        s.Set("news.example.com", "h1{x:y}");
        Assert.Equal("body{color:red}\nh1{x:y}", s.For("https://news.example.com/a"));
        Assert.Equal("body{color:red}", s.For("https://www.example.com/"));
        Assert.Equal("", s.For("https://other.org/"));
        s.Set("example.com", "");
        Assert.Equal("h1{x:y}", SiteStyles.Load().For("https://news.example.com/a"));
    }
}
