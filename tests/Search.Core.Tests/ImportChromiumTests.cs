using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Search.Core;
using Search.Core.Import;
using Xunit;

namespace Search.Core.Tests;

public class ImportChromiumTests
{
    static string Temp(string name) => Path.Combine(Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "imp-" + Guid.NewGuid().ToString("N")[..8])).FullName, name);

    static byte[] Seal(string text, byte[] key)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plain = System.Text.Encoding.UTF8.GetBytes(text);
        var cipher = new byte[plain.Length]; var tag = new byte[16];
        using var aes = new AesGcm(key, 16); aes.Encrypt(nonce, plain, cipher, tag);
        return [.. "v10"u8.ToArray(), .. nonce, .. cipher, .. tag];
    }

    static void Exec(string db, params string[] sql)
    {
        using var c = new SqliteConnection($"Data Source={db};Pooling=false"); c.Open();
        foreach (var s in sql) { using var cmd = c.CreateCommand(); cmd.CommandText = s; cmd.ExecuteNonQuery(); }
    }

    [Fact]
    public void BookmarksKeepTheirFolderAndOnlyWebAddresses()
    {
        var json = """
        {"roots":{"bookmark_bar":{"type":"folder","name":"Bookmarks bar","children":[
          {"type":"url","name":"Top","url":"https://top.example/"},
          {"type":"folder","name":"Reading","children":[{"type":"url","name":"Deep","url":"https://deep.example/"},{"type":"url","name":"Local","url":"chrome://settings"}]}]},
          "other":{"type":"folder","name":"Other bookmarks","children":[{"type":"url","name":"Loose","url":"http://loose.example"}]}}}
        """;
        var b = ChromiumImport.Bookmarks(json);
        Assert.Equal(3, b.Count);
        Assert.Contains(b, x => x.Title == "Top" && x.Folder is null);
        Assert.Contains(b, x => x.Title == "Deep" && x.Folder == "Reading");
        Assert.Contains(b, x => x.Title == "Loose" && x.Folder is null);
    }

    [Fact]
    public void HistoryReadsChromeTime()
    {
        var db = Temp("History");
        var t = (long)((new DateTime(2025, 3, 1, 12, 0, 0, DateTimeKind.Utc) - DateTime.UnixEpoch).TotalSeconds + 11_644_473_600L) * 1_000_000;
        Exec(db, "create table urls (id integer primary key, url text, title text, visit_count integer, last_visit_time integer)",
            $"insert into urls (url, title, visit_count, last_visit_time) values ('https://a.example/x', 'A', 3, {t}), ('chrome://version', 'V', 9, {t}), ('https://b.example', 'B', 0, {t})");
        var h = ChromiumImport.History(db);
        var one = Assert.Single(h);
        Assert.Equal("A", one.Title); Assert.Equal(3, one.Count); Assert.Equal(new DateTime(2025, 3, 1, 12, 0, 0, DateTimeKind.Utc), one.Last);
    }

    [Fact]
    public void LoginsAreOpenedWithTheKey()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var db = Temp("Login Data");
        Exec(db, "create table logins (origin_url text, username_value text, password_value blob, blacklisted_by_user integer)");
        using (var c = new SqliteConnection($"Data Source={db};Pooling=false"))
        {
            c.Open();
            void Add(string url, string user, byte[] blob, int black = 0)
            {
                using var cmd = c.CreateCommand();
                cmd.CommandText = "insert into logins values ($u, $n, $p, $b)";
                cmd.Parameters.AddWithValue("$u", url); cmd.Parameters.AddWithValue("$n", user); cmd.Parameters.AddWithValue("$p", blob); cmd.Parameters.AddWithValue("$b", black);
                cmd.ExecuteNonQuery();
            }
            Add("https://site.example/login", "ana", Seal("s3cret!", key));
            Add("https://other.example/", "bo", Seal("nope", RandomNumberGenerator.GetBytes(32)));
            Add("https://never.example/", "", [], 1);
            Add("https://never2.example/", "x", Seal("zz", key), 1);
        }
        var (logins, bad) = ChromiumImport.Logins(db, key);
        var one = Assert.Single(logins);
        Assert.Equal(("https://site.example/login", "ana", "s3cret!"), (one.Url, one.User, one.Password));
        Assert.Equal(1, bad);
    }

    [Fact]
    public void WrappedKeyComesFromLocalState()
    {
        var raw = System.Text.Encoding.ASCII.GetBytes("DPAPI").Concat(new byte[] { 1, 2, 3 }).ToArray();
        var json = "{\"os_crypt\":{\"encrypted_key\":\"" + Convert.ToBase64String(raw) + "\"}}";
        Assert.Equal(new byte[] { 1, 2, 3 }, ChromiumImport.WrappedKey(json));
        Assert.Null(ChromiumImport.WrappedKey("{}"));
    }

    [Fact]
    public void HistoryMergeKeepsTheLaterTimeAndAddsCounts()
    {
        var h = new History();
        var t = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        Assert.True(h.Merge("https://a.example/", "A", 2, t));
        Assert.False(h.Merge("https://www.a.example", "", 3, t.AddDays(1)));
        var v = Assert.Single(h.All);
        Assert.Equal(5, v.Count); Assert.Equal(t.AddDays(1), v.Last);
    }
}
