using Search.Core;
using Xunit;

namespace Search.Core.Tests;

public class VaultTests
{
    sealed class Reverse : ISecretProtector
    {
        public byte[] Protect(byte[] p) { var c = p.Reverse().ToArray(); return c; }
        public byte[] Unprotect(byte[] p) => p.Reverse().ToArray();
    }

    static PasswordVault Make(out string path)
    {
        path = Path.Combine(Path.GetTempPath(), "vault-" + Guid.NewGuid().ToString("N") + ".json");
        return new PasswordVault(path, new Reverse());
    }

    [Fact]
    public void SavesAndReadsBackWithoutTheFileHoldingThePassword()
    {
        var v = Make(out var path);
        Assert.True(v.Save("example.com", "ana", "hunter2hunter2"));
        Assert.Equal("hunter2hunter2", v.SecretOf("example.com", "ana"));
        Assert.DoesNotContain("hunter2", File.ReadAllText(path));
        Assert.Equal("hunter2hunter2", new PasswordVault(path, new Reverse()).SecretOf("example.com", "ana"));
    }

    [Fact]
    public void SavingAgainReplacesNotDuplicates()
    {
        var v = Make(out _);
        v.Save("example.com", "ana", "one-one-one"); v.Save("example.com", "ana", "two-two-two");
        Assert.Single(v.All());
        Assert.Equal("two-two-two", v.SecretOf("example.com", "ana"));
    }

    [Fact]
    public void ASiteIsMatchedAsASiteHostFirstThenNewestFirst()
    {
        var v = Make(out _);
        var t = DateTimeOffset.UtcNow;
        v.Save("example.com", "old", "p1p1p1p1", t.AddDays(-5));
        v.Save("accounts.example.com", "exact", "p2p2p2p2", t.AddDays(-9));
        v.Save("shop.example.com", "new", "p3p3p3p3", t);
        v.Save("other.org", "x", "p4p4p4p4");
        Assert.Equal(["exact", "new", "old"], v.KeptMatching("accounts.example.com").Select(k => k.User));
        Assert.Equal(["exact"], v.KeptMatching("accounts.example.com", wide: false).Select(k => k.User));
    }

    [Theory]
    [InlineData("accounts.example.com", "example.com")]
    [InlineData("www.example.co.uk", "example.co.uk")]
    [InlineData("someone.github.io", "someone.github.io")]
    [InlineData("localhost", "localhost")]
    [InlineData("192.168.0.1", "192.168.0.1")]
    [InlineData("example.co.uk", "example.co.uk")]
    public void RegistrableDomains(string host, string domain) => Assert.Equal(domain, Registrable.Domain(host));

    [Fact]
    public void ForgetTouchAndNever()
    {
        var v = Make(out _);
        v.Save("a.com", "u", "pppppppp");
        var k = v.All()[0];
        Assert.Null(k.Used);
        v.Touch(k);
        Assert.NotNull(v.All()[0].Used);
        Assert.True(v.Forget("a.com", "u")); Assert.False(v.Forget("a.com", "u"));
        v.Never("example.com");
        Assert.True(v.IsNever("accounts.example.com")); Assert.False(v.IsNever("other.org"));
        v.Save("a.com", "", "pppppppp");
        Assert.Equal(1, v.ForgetAll());
    }

    [Fact]
    public void ListsCarryNoSecrets()
    {
        var v = Make(out _);
        v.Save("a.com", "u", "pppppppp");
        Assert.DoesNotContain("pppppppp", string.Join(' ', v.All()));
    }

    [Fact]
    public void GeneratedPasswordsAreStrong()
    {
        for (var i = 0; i < 50; i++)
        {
            var p = PasswordGenerator.Make();
            Assert.Equal(20, p.Length);
            Assert.Contains(p, char.IsUpper); Assert.Contains(p, char.IsLower); Assert.Contains(p, char.IsDigit); Assert.Contains(p, c => !char.IsLetterOrDigit(c));
        }
        Assert.NotEqual(PasswordGenerator.Make(), PasswordGenerator.Make());
    }
}
