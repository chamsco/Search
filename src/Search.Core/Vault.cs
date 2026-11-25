using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Search.Core;

/// <summary>A kept password, with its secret. Only the paths that hand a password over take this: filling a sign-in, telling whether one is already kept.</summary>
public sealed record Login(string Host, string User, string Password, DateTimeOffset? Used = null, bool Clear = false)
{
    public string Id => Host + "\u0001" + User;
}

/// <summary>One item as a list needs it: the site, the account, and when it was last used, without the secret (Vault.swift's Kept). A row cannot be handed on as though it held a password.</summary>
public sealed record Kept(string Host, string User, DateTimeOffset? Used = null, bool Clear = false)
{
    public string Id => Host + "\u0001" + User;
}

/// <summary>How the vault's secrets are kept on disk: on Windows, protected for the signed-in account (DPAPI). Tests bring their own.</summary>
public interface ISecretProtector
{
    byte[] Protect(byte[] plain);
    byte[] Unprotect(byte[] protectedBytes);
}

/// <summary>Where passwords live (Vault.swift): one file, vault.json, each secret protected on its own, so a list reads names and dates and never a password. Nothing is logged.</summary>
public sealed class PasswordVault
{
    sealed class Item
    {
        public string Host { get; set; } = "";
        public string User { get; set; } = "";
        public string Secret { get; set; } = "";
        public DateTimeOffset? Used { get; set; }
        public bool Clear { get; set; }
    }
    sealed class FileShape
    {
        public List<Item> Items { get; set; } = [];
        public List<string> Never { get; set; } = [];
    }

    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    readonly string path;
    readonly ISecretProtector protector;
    readonly object gate = new();
    FileShape data = new();

    public event Action? Changed;

    public PasswordVault(string path, ISecretProtector protector)
    {
        this.path = path; this.protector = protector;
        try { if (File.Exists(path)) data = JsonSerializer.Deserialize<FileShape>(File.ReadAllText(path), Json) ?? new(); } catch { data = new(); }
    }

    void Write()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(data, Json));
            File.Move(tmp, path, true);
        }
        catch { }
        Changed?.Invoke();
    }

    static Kept AsKept(Item i) => new(i.Host, i.User, i.Used, i.Clear);

    /// <summary>What is kept for a host exactly, without secrets.</summary>
    public List<Kept> KeptFor(string host)
    {
        lock (gate) return data.Items.Where(i => i.Host == host).Select(AsKept).ToList();
    }

    /// <summary>A sign-in rarely lives on the page it was saved from: accounts.example.com asks and the password was kept for example.com. So the site is matched as a site: the host first, then anything sharing its registrable domain. <paramref name="wide"/> false: the host alone.</summary>
    public List<Kept> KeptMatching(string host, bool wide = true)
    {
        var domain = Registrable.Domain(host);
        lock (gate)
        {
            static int Newest(Kept a, Kept b) => (b.Used ?? DateTimeOffset.MinValue).CompareTo(a.Used ?? DateTimeOffset.MinValue);
            var exact = data.Items.Where(i => i.Host == host).Select(AsKept).ToList(); exact.Sort(Newest);
            var wider = wide ? data.Items.Where(i => i.Host != host && Registrable.Domain(i.Host) == domain).Select(AsKept).ToList() : [];
            wider.Sort(Newest);
            return [.. exact, .. wider];
        }
    }

    public List<Kept> All()
    {
        lock (gate) return data.Items.Select(AsKept).OrderBy(k => k.Host, StringComparer.Ordinal).ThenBy(k => k.User, StringComparer.Ordinal).ToList();
    }

    public int Count { get { lock (gate) return data.Items.Count; } }

    /// <summary>One item's secret, read when a password is shown, copied or filled, and never as part of a list.</summary>
    public string? SecretOf(Kept kept) => SecretOf(kept.Host, kept.User);

    public string? SecretOf(string host, string user)
    {
        lock (gate)
        {
            var item = data.Items.FirstOrDefault(i => i.Host == host && i.User == user);
            if (item is null) return null;
            try { return Encoding.UTF8.GetString(protector.Unprotect(Convert.FromBase64String(item.Secret))); } catch { return null; }
        }
    }

    public Login? Login(Kept kept) => SecretOf(kept) is { } p ? new Login(kept.Host, kept.User, p, kept.Used, kept.Clear) : null;

    /// <summary>Keep a password, replacing the one under the same site and account. A sign-in with a password box alone is kept under no name.</summary>
    public bool Save(string host, string user, string password, DateTimeOffset? used = null, bool clear = false)
    {
        if (host.Length == 0 || password.Length == 0) return false;
        string secret;
        try { secret = Convert.ToBase64String(protector.Protect(Encoding.UTF8.GetBytes(password))); } catch { return false; }
        lock (gate)
        {
            var item = data.Items.FirstOrDefault(i => i.Host == host && i.User == user);
            if (item is null) data.Items.Add(item = new Item { Host = host, User = user });
            item.Secret = secret; item.Clear = clear;
            item.Used = used ?? item.Used;
            Write();
        }
        return true;
    }

    /// <summary>It was just used to sign in: lists put it first from now on.</summary>
    public void Touch(Kept kept, DateTimeOffset? now = null)
    {
        lock (gate)
        {
            var item = data.Items.FirstOrDefault(i => i.Host == kept.Host && i.User == kept.User);
            if (item is null) return;
            item.Used = now ?? DateTimeOffset.UtcNow;
            Write();
        }
    }

    public bool Forget(string host, string user)
    {
        lock (gate)
        {
            var n = data.Items.RemoveAll(i => i.Host == host && i.User == user);
            if (n > 0) Write();
            return n > 0;
        }
    }

    public int ForgetAll()
    {
        lock (gate) { var n = data.Items.Count; data.Items.Clear(); if (n > 0) Write(); return n; }
    }

    // MARK: - sites never to ask about

    public void Never(string host)
    {
        lock (gate) { if (!data.Never.Contains(host)) { data.Never.Add(host); Write(); } }
    }
    public bool IsNever(string host)
    {
        lock (gate) return data.Never.Contains(host) || data.Never.Contains(Registrable.Domain(host));
    }
    public List<string> NeverList() { lock (gate) return [.. data.Never.Order()]; }
    public void ForgetNever(string host) { lock (gate) { if (data.Never.Remove(host)) Write(); } }
}

/// <summary>A strong password for a new account (what a sign-up's "suggest" offers): random, with each kind of character, none that read alike.</summary>
public static class PasswordGenerator
{
    const string Lower = "abcdefghijkmnopqrstuvwxyz", Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ", Digits = "23456789", Symbols = "-_.!?#$%&*+=";

    public static string Make(int length = 20)
    {
        length = Math.Clamp(length, 8, 64);
        var all = Lower + Upper + Digits + Symbols;
        var chars = new List<char> { Pick(Lower), Pick(Upper), Pick(Digits), Pick(Symbols) };
        while (chars.Count < length) chars.Add(Pick(all));
        for (var i = chars.Count - 1; i > 0; i--) { var j = RandomNumberGenerator.GetInt32(i + 1); (chars[i], chars[j]) = (chars[j], chars[i]); }
        return new string([.. chars]);
    }

    static char Pick(string s) => s[RandomNumberGenerator.GetInt32(s.Length)];
}
