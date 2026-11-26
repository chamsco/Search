using System.IO.Compression;
using Xunit;

namespace Search.Core.Tests;

public class ExtensionTests : IDisposable
{
    readonly string root = Path.Combine(Path.GetTempPath(), "search-ext-" + Guid.NewGuid().ToString("N")[..8]);
    public ExtensionTests() { Directory.CreateDirectory(root); Store.Root = root; }
    public void Dispose() { try { Directory.Delete(root, true); } catch { } }

    static byte[] Zip(Dictionary<string, string> files)
    {
        var ms = new MemoryStream();
        using (var z = new ZipArchive(ms, ZipArchiveMode.Create, true))
            foreach (var (name, text) in files) { using var w = new StreamWriter(z.CreateEntry(name).Open()); w.Write(text); }
        return ms.ToArray();
    }

    static byte[] Crx3(byte[] zip)
    {
        var header = new byte[] { 1, 2, 3, 4, 5 };
        var all = new List<byte>("Cr24"u8.ToArray());
        all.AddRange(BitConverter.GetBytes(3u)); all.AddRange(BitConverter.GetBytes((uint)header.Length)); all.AddRange(header); all.AddRange(zip);
        return [.. all];
    }

    [Theory]
    [InlineData("https://chromewebstore.google.com/detail/ublock-origin/cjpalhdlnbpafiamejdnhcphjbkeiagm?hl=en", "cjpalhdlnbpafiamejdnhcphjbkeiagm")]
    [InlineData("https://chrome.google.com/webstore/detail/cjpalhdlnbpafiamejdnhcphjbkeiagm", "cjpalhdlnbpafiamejdnhcphjbkeiagm")]
    [InlineData("cjpalhdlnbpafiamejdnhcphjbkeiagm", "cjpalhdlnbpafiamejdnhcphjbkeiagm")]
    public void StoreIdIsFoundInALinkOrBare(string text, string id) => Assert.Equal(id, Extensions.StoreIdOf(text));

    [Fact] public void NotAnId() => Assert.Null(Extensions.StoreIdOf("https://example.com/page"));

    [Fact]
    public void Crx3HeaderIsStrippedAndPlainZipIsLeftAlone()
    {
        var zip = Zip(new() { ["manifest.json"] = "{}" });
        Assert.Equal(zip, Extensions.ZipOf(Crx3(zip)));
        Assert.Equal(zip, Extensions.ZipOf(zip));
    }

    [Fact]
    public void InstallReadsTheManifestAndKeepsTheFolder()
    {
        var zip = Zip(new()
        {
            ["manifest.json"] = "{\"manifest_version\":3,\"name\":\"__MSG_appName__\",\"version\":\"1.2\",\"default_locale\":\"en\",\"description\":\"Does things\",\"icons\":{\"16\":\"i16.png\",\"48\":\"/img/i48.png\"},\"action\":{\"default_popup\":\"popup.html\"},\"options_page\":\"opts.html\",\"permissions\":[\"storage\"],\"content_scripts\":[{\"matches\":[\"https://*.example.com/*\"],\"js\":[\"c.js\"]}]}",
            ["_locales/en/messages.json"] = "{\"appName\":{\"message\":\"Neat Tool\"}}",
            ["popup.html"] = "<p>hi</p>",
        });
        var ext = Extensions.Load();
        var e = ext.InstallPackage(Crx3(zip), "abcdefghijklmnopabcdefghijklmnop");
        Assert.Equal("Neat Tool", e.Name);
        Assert.Equal("1.2", e.Version);
        Assert.Equal("img/i48.png", e.Icon);
        Assert.Equal("popup.html", e.Popup);
        Assert.Equal("opts.html", e.Options);
        Assert.Contains("storage", e.Permissions);
        Assert.Contains("https://*.example.com/*", e.Sites);
        Assert.True(File.Exists(Path.Combine(ext.PathOf(e), "popup.html")));
        Assert.Single(Extensions.Load().Items);
    }

    [Fact]
    public void InstallingAgainReplacesAndKeepsItsSwitch()
    {
        var ext = Extensions.Load();
        byte[] Pack(string v) => Zip(new() { ["manifest.json"] = "{\"name\":\"Same\",\"version\":\"" + v + "\"}" });
        var first = ext.InstallPackage(Pack("1"), "abcdefghijklmnopabcdefghijklmnop");
        ext.SetEnabled(first.Key, false);
        var second = ext.InstallPackage(Pack("2"), "abcdefghijklmnopabcdefghijklmnop");
        Assert.Single(ext.Items);
        Assert.Equal("2", second.Version);
        Assert.False(second.Enabled);
    }

    [Fact]
    public void ZipSlipIsIgnoredAndNoManifestIsRefused()
    {
        var ext = Extensions.Load();
        var evil = Zip(new() { ["manifest.json"] = "{\"name\":\"X\",\"version\":\"1\"}", ["../outside.txt"] = "no" });
        ext.InstallPackage(evil);
        Assert.False(File.Exists(Path.Combine(root, "outside.txt")));
        Assert.Throws<InvalidDataException>(() => ext.InstallPackage(Zip(new() { ["readme.txt"] = "x" })));
    }

    [Fact]
    public void RemoveDeletesTheFolder()
    {
        var ext = Extensions.Load();
        var e = ext.InstallPackage(Zip(new() { ["manifest.json"] = "{\"name\":\"Gone\",\"version\":\"1\"}" }));
        var path = ext.PathOf(e);
        ext.Remove(e.Key);
        Assert.False(Directory.Exists(path));
        Assert.Empty(Extensions.Load().Items);
    }

    [Fact]
    public void UpdateAnswerGivesThePackageOnlyWhenNewer()
    {
        const string yes = "<gupdate><app appid=\"x\"><updatecheck codebase=\"https://example.com/a.crx?x=1&amp;y=2\" version=\"1.10.0\" status=\"ok\"/></app></gupdate>";
        Assert.Equal("https://example.com/a.crx?x=1&y=2", Extensions.NewerPackage(yes, "1.9.0"));
        Assert.Null(Extensions.NewerPackage(yes, "1.10.0"));
        Assert.Null(Extensions.NewerPackage("<gupdate><app><updatecheck status=\"noupdate\"/></app></gupdate>", "1"));
        Assert.Null(Extensions.NewerPackage("<gupdate/>", "1"));
    }

    [Fact]
    public void UpdateBumpsTheRevisionSoProfilesSwap()
    {
        var ext = Extensions.Load();
        byte[] Pack(string v) => Zip(new() { ["manifest.json"] = "{\"name\":\"Rev\",\"version\":\"" + v + "\"}" });
        var a = ext.InstallPackage(Pack("1"), "abcdefghijklmnopabcdefghijklmnop");
        ext.MarkApplied(a.Key, "Default", a.Revision);
        var b = ext.InstallPackage(Pack("2"), "abcdefghijklmnopabcdefghijklmnop");
        Assert.Equal(a.Revision + 1, b.Revision);
        Assert.Equal(a.Revision, b.Applied["Default"]);
    }

    [Fact]
    public void ZipWithOneFolderLevelInstallsAndLeavesNoTempBehind()
    {
        var ext = Extensions.Load();
        var e = ext.InstallPackage(Zip(new() { ["inner/manifest.json"] = "{\"name\":\"Nested\",\"version\":\"1\"}", ["inner/a.js"] = "1" }));
        Assert.True(File.Exists(Path.Combine(ext.PathOf(e), "a.js")));
        Assert.Equal([ext.PathOf(e)], Directory.GetDirectories(Extensions.Folder));
    }
}
