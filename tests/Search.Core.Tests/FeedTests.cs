using Search.Core;
using Xunit;

namespace Search.Core.Tests;

public class FeedTests
{
    static readonly Uri Url = new("https://blog.example/feed.xml");

    [Fact]
    public void ReadsRss()
    {
        var xml = @"<?xml version=""1.0""?><rss version=""2.0""><channel><title>The Blog</title>
          <item><title>First &amp;amp; best</title><link>https://blog.example/a</link></item>
          <item><title>Second</title><link>/b</link></item>
          <item><title>No link</title></item>
          <item><title>Guid only</title><guid>https://blog.example/c</guid></item></channel></rss>";
        var r = Feed.Parse(xml, Url)!;
        Assert.Equal("The Blog", r.Title);
        Assert.Equal(["First & best", "Second", "Guid only"], r.Items.Select(i => i.Title));
        Assert.Equal("https://blog.example/b", r.Items[1].Link);
    }

    [Fact]
    public void ReadsAtomAlternateLink()
    {
        var xml = @"<feed xmlns=""http://www.w3.org/2005/Atom""><title>Releases</title>
          <entry><title>v2</title><link rel=""self"" href=""https://x/self""/><link rel=""alternate"" href=""https://github.com/o/r/releases/tag/v2""/></entry></feed>";
        var r = Feed.Parse(xml, new Uri("https://github.com/o/r/releases.atom"))!;
        Assert.Equal("Releases", r.Title);
        Assert.Equal("https://github.com/o/r/releases/tag/v2", Assert.Single(r.Items).Link);
    }

    [Fact]
    public void KeepsOnlyTheLatestTwelveWithoutRepeats()
    {
        var items = string.Concat(Enumerable.Range(0, 30).Select(i => $"<item><title>P{i % 20}</title><link>https://b/{i % 20}</link></item>"));
        var r = Feed.Parse($"<rss><channel><title>T</title>{items}</channel></rss>", Url)!;
        Assert.Equal(Feed.Most, r.Items.Count);
        Assert.Equal(r.Items.Count, r.Items.Select(i => i.Link).Distinct().Count());
    }

    [Fact]
    public void APageIsNotAFeedButPointsToOne()
    {
        var html = @"<html><head><link rel=""stylesheet"" href=""x.css""><link rel=""alternate"" type=""application/rss+xml"" href=""/atom?a=1&amp;b=2""></head></html>";
        Assert.Null(Feed.Parse(html, Url));
        Assert.Equal("https://blog.example/atom?a=1&b=2", Feed.Discover(html, Url)!.AbsoluteUri);
        Assert.Null(Feed.Discover("<html></html>", Url));
    }

    [Fact]
    public void DoctypesAreRefused() => Assert.Null(Feed.Parse("<!DOCTYPE rss [<!ENTITY x SYSTEM \"file:///etc/passwd\">]><rss><channel><title>&x;</title></channel></rss>", Url));

    [Fact]
    public void AddressesAreWebOnly()
    {
        Assert.Equal("https://example.com/feed", Feed.Address("example.com/feed")!.AbsoluteUri);
        Assert.Null(Feed.Address("two words"));
        Assert.Null(Feed.Address("ftp://example.com/x"));
        Assert.Null(Feed.Address(""));
    }

    [Fact]
    public void RefetchKeepsIdsByLink()
    {
        var old = new List<FeedItem> { new() { Title = "a", Link = "https://b/a" } };
        var fresh = new List<FeedItem> { new() { Title = "a", Link = "https://b/a" }, new() { Title = "n", Link = "https://b/n" } };
        var kept = Feed.KeepingIds(fresh, old);
        Assert.Equal(old[0].Id, kept[0].Id);
        Assert.NotEqual(old[0].Id, kept[1].Id);
    }
}
